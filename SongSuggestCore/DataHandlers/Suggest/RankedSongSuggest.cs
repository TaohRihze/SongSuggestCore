using System;
using System.Collections.Generic;
using System.Linq;
using PlaylistNS;
using System.Diagnostics;
using LinkedData;
using Settings;
using SongSuggestNS;
using BanLike;
using SongLibraryNS;
using ScoreSabersJson;

namespace Actions
{
    public partial class RankedSongSuggest
    {
        public static RankedSongSuggest ActiveRankedSongSuggest;

        //Read only object links to data in this class for the calculations
        private DTO dto;

        //Access to the general toolbox
        public SongSuggest songSuggest { get; set; }

        //Source Manager abstraction for handling leaderboard and player score data logic
        public SuggestSourceManager suggestSM;

        [Obsolete("External calls should not access Context data. This should be moved into Ranked Song Suggest when possible. This is a workaround to allow temporary access.")]
        public Top10kPlayers Leaderboard() => suggestSM.Leaderboard();

        //0-1 Value showing how far the calculations have progressed for the UIs to display.
        public double songSuggestCompletion { get; set; }

        //All found songs ordered by current filter settings.
        public List<SongID> sortedSuggestions { get; set; }

        //Found songs with ignored songs removed
        public List<SongID> filteredSuggestions { get; set; }
        //ID's of songs used in actual playlist
        public List<SongID> songSuggestIDs { get; set; }

        //Settings file for current active session of RankedSongsSuggest, can be replaced/updated for refiltering.
        public SongSuggestSettings settings { get; set; }

        //Debug Timer, used to see if calculations end up being too slow.
        Stopwatch timer = new Stopwatch();

        //List for songs selected as Origin.
        public List<SongID> originSongIDs;
        List<SongID> ignoreSongs;

        //The two collections of endpoints of the Origin and Target songs from 10k player data based on Active Players data.
        //Active Player Songs = Top originSongsCount ranked and or liked songs
        //Origin = (10k player) Songs that match Active Players Songs
        //Target = Suggested Songs linked from Origin (within a single 10k player, 1 origin -> 19 target links of other top 20 songs.)
        //Only needs recalculation if Originsongs change, or the player improves a top originSongsCount score (less filtered betterAccCap suggestions).
        public SongEndPointCollection originSongs;
        public SongEndPointCollection targetSongs;

        //Amount of songlinks for good results. Less and we can try and remove the better/worse limits and evaluate the results.
        //Removing bad results, and just add default results to fill the list to make sure there is results for the playlist.
        //int minSongLinks = 6000;
        internal int linkedPlayers = 0;
        int linkedSongs = 0;

        //Add Filler songs if player has a low amount of source songs
        int targetFillers = 50;
        int minPlays = 20;

        //Value for how many spots must be expected to be improved before being shown in suggestions (unplayed songs are always shown)
        int improveSpots = 5;
        int improveSpotsAccSaber = 2;

        //Percent of song links to keep (70% seems to work well in general, but acc saber players can use PP Potential slider to adjust this (
        double LinkKeepPercent = 0.70;

        //Links for variables used in this class from Settings file. Classes are not done here
        public bool ignorePlayedAll => settings.IgnorePlayedAll;
        public int ignorePlayedDays => settings.IgnorePlayedDays;
        public bool ignoreNonImproveable => settings.IgnoreNonImproveable;
        public int requiredMatches => settings.RequiredMatches;
        public bool useLikedSongs => settings.UseLikedSongs;
        public bool fillLikedSongs => settings.FillLikedSongs;
        public bool useLocalScores => settings.UseLocalScores;
        public int extraSongs => settings.ExtraSongs;
        public int playlistLength => settings.PlaylistLength;
        public SongCategory accSaberPlaylistCategories => settings.AccSaberPlaylistCategories;
        int originSongsCount => settings.OriginSongCount;
        int extraSongsCount => settings.ExtraSongs;
        LeaderboardType leaderboard => settings.Leaderboard;

        //Filter Results
        //public List<SongID> distanceFilterOrdered;
        public List<SongID> styleFilterOrdered;
        public List<SongID> overWeightFilterOrdered;

        //Default constructor, creates the DTO link object that can pull object links from here.
        public RankedSongSuggest()
        {
            dto = new DTO(this);
        }

        string TimeTaken = "";
        long segment = 0;
        long previousTotal = 0;
        long total = 0;

        private void UpdateTimeTaken(string sectionName)
        {
            previousTotal = total;
            total = timer.ElapsedMilliseconds;
            segment = total - previousTotal;
            TimeTaken = ($"{TimeTaken}{Environment.NewLine}{sectionName,-40} Total: {total,5}ms  Segment: {segment,5}ms  Requests: {songSuggest.songLibrary.GetSongIDRequests()}");
        }

        //Creates a playlist with playlist count suggested songs based on the link system.
        public void SuggestedSongs()
        {
            ActiveRankedSongSuggest = this;
            //save request
            songSuggest.fileHandler.SaveSongSuggestRequest(settings);

            //Workaround to default accsaber to 50 songs if unmodified. (suggested default).
            if (settings.Leaderboard == LeaderboardType.AccSaber && originSongsCount == 25) settings.OriginSongCount = 50;

            songSuggest.log?.WriteLine($"LinkKeepPercent set at: {LinkKeepPercent}");

            suggestSM = new SuggestSourceManager()
            {
                songSuggest = this.songSuggest,
                leaderboardType = leaderboard
            };

            //Update player scores to only use local scores if enabled.
            var currentActiveScoreLocations = songSuggest.activePlayer.ActiveScoreLocations;
            if (useLocalScores)
            {
                Console.WriteLine("Using Local Scores");
                songSuggest.activePlayer.ActiveScoreLocations.Clear();
                songSuggest.activePlayer.ActiveScoreLocations.Add(ScoreLocation.SessionScores);
            }

            songSuggest.log?.WriteLine("Starting Song Suggest");

            songSuggest.log?.WriteLine("Starts the timer");
            timer.Start();

            TimeTaken = "";
            UpdateTimeTaken("Start:");

            //Setup Base Linking (song links).
            CreateLinks();
            UpdateTimeTaken("After: CreateLinks()");

            //Generate the different filters rankings. (Calculate Scores, and Rank them)
            CreateFilterRanks();
            UpdateTimeTaken("After: CreateFilterRanks()");

            //Takes the orderes lists runs through them and assign points based on order.
            EvaluateFilters();
            UpdateTimeTaken("After: EvaluateFilters()");

            //Removes filtered songs (Played/Played within X days/Banned/Not expected improveable atm) depending on settings
            RemoveIgnoredSongs();
            UpdateTimeTaken("After: RemoveIgnoredSongs()");

            //Performs special selection filtering for Leaderboards
            LeaderboardSpecialFiltering();
            UpdateTimeTaken("After: LeaderboardSpecialFiltering()");

            //Creates the playist of remaining songs
            CreatePlaylist();
            UpdateTimeTaken("After: CreatePlaylist()");

            //----- Console Writeline for Debug -----
            songSuggest.log?.WriteLine($"Players Linked: {linkedPlayers}");
            songSuggest.log?.WriteLine($"Songs Linked: {linkedSongs}");
            songSuggest.log?.WriteLine($"Unique Songs Found: {filteredSuggestions.Count}");
            songSuggest.log?.WriteLine($"Playlist Generation Done: {timer.ElapsedMilliseconds}ms");

            timer.Stop();
            timer.Reset();
            songSuggest.log?.WriteLine($"Time Spent: {timer.ElapsedMilliseconds}ms");
            songSuggest.log?.WriteLine(TimeTaken);

            //Restore ActiveScoreLocations if changed to local
            songSuggest.activePlayer.ActiveScoreLocations = currentActiveScoreLocations;
            ActiveRankedSongSuggest = null;

        }

        //Creates the needed linked data for song evaluation for the Active Player.
        //Until Active Players top originSongsCount scores change *1 (replaced or better scores) no need to recalculate
        //*1 (Liked songs if active changes also counts as an update)
        public void CreateLinks()
        {
            //Get Link Data
            songSuggest.status = "Finding Link Data";
            songSuggest.log?.WriteLine($"Done loading and generating the top10k player data: {timer.ElapsedMilliseconds}ms");

            //Find the Origin Song ID's based on Active Players data.
            songSuggest.status = "Finding Songs to Match";
            GenerateOriginSongIDs.Execute(dto, out originSongIDs);
            songSuggest.log?.WriteLine("Completion: " + (songSuggestCompletion * 100) + "%");

            //Link the origin songs with the songs on the LeaderBoard as a basis for suggestions.
            ignoreSongs = songSuggest.songBanning.GetPermaBannedIDs();
            GenerateLinks.Execute(dto, out originSongs, out targetSongs, out linkedSongs);
            var linkedPlayerIDs = targetSongs.endPoints.Values
                .SelectMany(c => c.songLinks)
                .Select(d => d.playerID)
                .Distinct()
                .ToList();
            linkedPlayers = linkedPlayerIDs.Count();

            songSuggest.log?.WriteLine("Completion: " + (songSuggestCompletion * 100) + "%");
            songSuggest.log?.WriteLine($"Suggest Linking Done: {timer.ElapsedMilliseconds}ms");
        }

        //If the player has no available played songs (or low count) we need a list of potential fillers.
        //At the same time we remove limits on all songs
        public List<SongID> GetFillerSongs()
        {
            //Find all songs in the leaderboard with at least a minimum of top scores, so we base seed songs on stuff that are linked to other stuff
            //We then order by lowest max scores, so we pick the likely easier songs (best metric on current leaderboards)
            //We keep a certain %'age of the remaining scores to get rid of the "highest ranked" ones
            //And then we sort by the best average rank in the list, so we get strong songs as candidates. (This is a mix of best of the worst)
            
            //Taste Testing 30% seemed to get spread results on the leaderboards
            double percentToLookIn = 0.30;

            var fillerSongCandidates = suggestSM.Leaderboard().top10kSongMeta
                .Where(c => c.Value.count >= minPlays)                                  //Linked Up songs only
                .OrderBy(c => c.Value.averageScore)                                     //Picks lowest value to get easier songs (Need better handling)
                .ToList();

            int targetCount = (int)(percentToLookIn * fillerSongCandidates.Count())+1;  //Int rounds down, lets keep at least 1 song. Take cannot overflow.

            var fillerSongIDs = fillerSongCandidates     
                .Take(targetCount)                                                      //Testing found this %'age to give a mix of old and new, and lower amount of horrible stuff
                .OrderBy(c => c.Value.totalRank / c.Value.count)                        //Selecting best average rank, makes strong candidates appear
                .Select(c => c.Value.songID)
                .Select(c => SongLibrary.StringIDToSongID(c, suggestSM.LeaderboardSongIDType()))
                .ToList();

            songSuggest.log?.WriteLine($"Selected Filler Songs");
            foreach (var song in fillerSongIDs.Take(50))
            {
                songSuggest.log?.WriteLine(SongLibrary.GetDisplayName(song));
            }

            return fillerSongIDs;
        }

        //Order the songs via the different active filters
        public void CreateFilterRanks()
        {
            //Calculate the scores on the songs for suggestions
            songSuggest.status = "Evaluating Found Songs";
            EvaluateSongs();
            songSuggest.log?.WriteLine("Completion: " + (songSuggestCompletion * 100) + "%");
            songSuggest.log?.WriteLine($"Score Relevance Calculations Done: {timer.ElapsedMilliseconds}ms");

            //Find most relevant songs for playlist selection
            songSuggest.status = "Selecting Best Matching Songs";


            //Filter on how much over/under linked a song is in the active players data vs the global player population
            styleFilterOrdered = targetSongs.endPoints.Values.OrderBy(s => (0.0 + suggestSM.Leaderboard().top10kSongMeta[s.songID].count) / (0.0 + s.proportionalStyle)).Select(p => p.songID).ToList();

            //Filter on how the selected songs rank are better than average
            overWeightFilterOrdered = targetSongs.endPoints.Values.OrderBy(s => s.averageRank).Select(p => p.songID).ToList();
        }

        //Goal here is to get a good sample of a players songs that are not banned. The goal is try and find originSongsCount candidates to represent a player.
        //We filter out round up 25% worst scores (keeping at least 1) to allow progression on actual scores on lower song counts by filtering bad fits earlier
        //Then we filter out the requested portion of low accuracy songs.
        public List<SongID> SelectPlayedOriginSongs()
        {
            double maxKeepPercentage = 0.75;                                                //Percent of plays to keep on players with low playcount (Rounding is handled locally)

            //Find available songs
            var filteredSongs = suggestSM.PlayerScoresIDs()                                 //Grab songID's for songs matching the given Suggest Context from Source Manager
                .Where(c => !songSuggest.songBanning.IsPermaBanned(c, BanType.Global))      //Remove Perma Banned Songs
                .OrderByDescending(value => suggestSM.PlayerWeightedScoreValue(value))      //Order Songs by Leaderboards Effective value
                .ToList();

            //To ensure worst songs are always removed (progression while getting enough songs) we only keep a certain percent of songs (75% default)
            int valueSongCount = filteredSongs.Count();
            valueSongCount = (int)(maxKeepPercentage * valueSongCount);     //Reduce the list to 75% best
            if (valueSongCount == 0) valueSongCount = 1;                    //If 1 is available, 1 should always be selected, but outside this goal is to reduce to 75% rounded down

            //Find the target song count after removing accuracy adjustments
            double percentToKeep = (double)originSongsCount / (originSongsCount + extraSongsCount);
            int comparativeBestCount = (int)Math.Ceiling(percentToKeep * valueSongCount);

            //Get the songs by 
            filteredSongs = filteredSongs
                .Take(valueSongCount)                                           //Grab 75% best of scores
                .Take(originSongsCount + extraSongsCount)                       //Grab up to the portion that is default cap before acc sorting
                .OrderByDescending(c => suggestSM.PlayerRelativeScoreValue(c))  //Sort by best Relative Scores (to top score on song) Should find songs you have done comparative best on.
                .Take(comparativeBestCount)                                     //Grab the goal of comparative best songs (relevant if less than originSongsCount should be kept to keep a matching % removed instead)
                .Take(originSongsCount)                                         //Reduce the with acc selection to originSongsCount comparative best songs (default reduction so comparative worst is removed)   
                .OrderByDescending(c => suggestSM.PlayerWeightedScoreValue(c))  //Reorder back to Score Value (relevant only if player got other prioritised songs like Liked songs before reducing suggest list at higher levels)
                .ToList();


            //Debug code for showing actual selected songs.
            songSuggest.log?.WriteLine("Selected Origin Songs");
            var potentialSongs = suggestSM.PlayerScoresIDs()                                //Grab songID's for songs matching the given Suggest Context from Source Manager
                .Where(c => !songSuggest.songBanning.IsPermaBanned(c, BanType.Global))      //Remove Perma Banned Songs
                .OrderByDescending(value => suggestSM.PlayerWeightedScoreValue(value))      //Order Songs by value
                .Take(originSongsCount + extraSongsCount)
                .ToList();

            foreach (var songID in potentialSongs)
            {
                var inFiltered = filteredSongs.Contains(songID) ? "X": " ";
                var song = SongLibrary.SongIDToSong(songID);
                var songCategory = song.songCategory & suggestSM.LeaderboardSongCategory();
                var songName = songSuggest.songLibrary.GetDisplayName(songID);
                var percent = suggestSM.PlayerRelativeScoreValue(songID)*100;
                songSuggest.log?.WriteLine($"{inFiltered}   SongCategory: {songCategory,-16}   Score: {suggestSM.PlayerScoreValue(songID),8:N2}({suggestSM.PlayerWeightedScoreValue(songID),8:N2})   Advantage%: {percent,6:0.00}%    {songName}");
            }

            //Returns the found songs.
            return filteredSongs;
        }

        //Generate the weighting for the different Filters and stores them in the Endpoint Data.
        public void EvaluateSongs()
        {
            //Calculate strength for filter rankings in the SongLink data with needed data sent along.
            targetSongs.SetRelevance(this, originSongs.endPoints.Count(), requiredMatches, suggestSM.LeaderboardSongIDType());
            targetSongs.SetStyle(originSongs, suggestSM.LeaderboardSongIDType());
        }

        //Takes the orderes suggestions and apply the filter values to their ranks, and create the nameplate orderings
        //**Consider rewriting to handle any amount of filters in the future (loop each filter for its position and record it before multiplying all).**
        public void EvaluateFilters()
        {
            Dictionary<SongID, double> totalScore = new Dictionary<SongID, double>();

            //Get Base Weights reset them from % value to [0-1], and must not all be 0)
            double modifierStyle = settings.FilterSettings.modifierStyle / 100;
            double modifierOverweight = settings.FilterSettings.modifierOverweight / 100;

            //reset if all = 0, reset to 100%.
            if (modifierStyle == 0 && modifierOverweight == 0) modifierStyle = modifierOverweight = 1.0;

            songSuggest.log?.WriteLine($"Style: {modifierStyle} Overweight: {modifierOverweight}");

            //Get count of candidates, and remove 1, as index start as 0, so max value is songs-1
            double totalCandidates = overWeightFilterOrdered.Count() - 1;

            //We loop either of the 2 filters and record its ordering in a temporary dictionary for quick lookup.
            Dictionary<SongID, int> overweightValues = new Dictionary<SongID, int>();
            int rankCount = 0;
            foreach (var songID in overWeightFilterOrdered)
            {
                overweightValues[songID] = rankCount;
                rankCount++;
            }

            //Reset count and do the same for the 2nd, but we might as well do the calculations at the same time.
            rankCount = 0;
            foreach (SongID songID in styleFilterOrdered)
            {
                //Get the location of the candidate in the list as a [0-1] value
                double styleValue = rankCount / totalCandidates;
                double overWeightedValue = overweightValues[songID] / totalCandidates;

                //Switch the range from [0-1] to [0.5-1.5] and reduce the gap based on modifier weight.
                //**Spacing between values may be more correct to consider a log spacing (e.g. due to 1.5*.0.5 != 1)
                //**But as values are kept around 1, and it is not important to keep total average at 1, the difference in
                //**Actual ratings in the 0.5 to 1.5 range is minimal at the "best suggestions range" even with quite a few filters.
                //**So a "correct range" of 0.5 to 2 would give a higher penalty on bad matches on a single filter, so current
                //**setup means a song must do worse on more filters to actual lose rank, which actually may be prefered.
                //double distanceTotal = distanceValue * modifierDistance + (1.0 - 0.5 * modifierDistance);
                double styleTotal = styleValue * modifierStyle + (1.0 - 0.5 * modifierStyle);
                double overWeightedTotal = overWeightedValue * modifierOverweight + (1.0 - 0.5 * modifierOverweight);

                //Get the songs multiplied average 
                double score = styleTotal * overWeightedTotal;

                //Add song ID and its score to a list for sorting and reducing size for the playlist generation
                totalScore.Add(songID, score);
                
                //Increase rank count for next song.
                rankCount++;
            }
            //Sort list, and get song ID's only
            sortedSuggestions = totalScore.OrderBy(s => s.Value).Select(s => s.Key).ToList();

        }

        //Filters out any songs that should not be in the generated playlist
        //Ignore All Played
        //Ignore X Days
        //Banned Songs
        //Songs that is not expected improveable
        public void RemoveIgnoredSongs()
        {
            //Filter out ignoreSongs before making the playlist.
            //Get the ignore lists ready (permaban, ban, and improved within X days, not improveable by X ranks)
            songSuggest.status = "Preparing Ignore List";
            List<SongID> ignoreSongs = CreateIgnoreLists(ignorePlayedAll ? -1 : ignorePlayedDays);
            filteredSuggestions = sortedSuggestions
                .Except(ignoreSongs)
                .ToList();
        }

        //Create a List of songID's to filter out. Consider splitting it so Permaban does not get links, while
        //standard temporary banned, and recently played gets removed after.
        //Send -1 if all played should be ignored, else amount of days to ignore.
        public List<SongID> CreateIgnoreLists(int ignoreDays)
        {
            List<SongID> ignoreSongs = new List<SongID>();

            //Ignore recently/all played songs
            //Add either all played songs
            var playedSongs = suggestSM.PlayerScoresIDs();

            if (ignoreDays == -1)
            {
                ignoreSongs.AddRange(playedSongs);
            }
            //Or the songs only played within a given time periode
            else
            {
                var filteredSongs = playedSongs
                    .Where(song => (DateTime.UtcNow - suggestSM.PlayerScoreDate(song)).TotalDays < ignoreDays)
                    .ToList();

                ignoreSongs.AddRange(filteredSongs);
            }

            //Add the banned songs to the ignoresong list if not already on it.
            ignoreSongs = ignoreSongs
                .Union(songSuggest.songBanning.GetBannedIDs())
                .ToList();

            //Add songs that is not expected to be improveable by X ranks
            if (ignoreNonImproveable)
            {
                ignoreSongs.AddRange(LeaderboardNonImproveableFiltering());
            }

            //**Remove all acc broken songs for now
            ignoreSongs.AddRange(SongLibrary.GetAllRankedSongIDs(SongCategory.BrokenDownloads));

            return ignoreSongs;
        }

        //Filtering out songs that are deemed nonimproveable.
        private List<SongID> LeaderboardNonImproveableFiltering()
        {
            List<SongID> ignoreSongs = new List<SongID>();

            //As acc saber has more leaderboards, less improvement on the local leaderboard should be needed before shown.
            if (suggestSM.leaderboardType == LeaderboardType.AccSaber) improveSpots = improveSpotsAccSaber;

            //Create a lookup for ranks of the leaderboards. If multiple leaderboards use the sub leaderboards rank.
            var scoreToRank = sortedSuggestions
                .Select(c => new { score = c, Masked = suggestSM.LeaderboardSongCategory() & c.GetSong().songCategory}) //Reduce the SongCategory to Relevant categories only
                .GroupBy(x => x.Masked)                                                                                 //Group by Category
                .Select(g => g.Select((x, index) => new { x.score, GroupIndex = index + 1 }))                           //Assign Rank Index in each Category (1-index)
                .SelectMany(g => g)                                                                                     //Flatten the data so it is only score and index.
                .ToDictionary(x => x.score, x => x.GroupIndex);                                                         //Create a lookup for songID -> category rank

            foreach (SongID songID in sortedSuggestions)
            {
                int currentSongRank = suggestSM.PlayerScoreRank(songID);

                //Add songs ID to ignore list if current rank is not expected improveable by at least X spots, and it is not an unplayed song
                if (currentSongRank < scoreToRank[songID] + improveSpots && currentSongRank != -1)
                {
                    ignoreSongs.Add(songID);
                }
            }

            return ignoreSongs;
        }

        //Filtering that only applies to specific leaderboards
        private void LeaderboardSpecialFiltering()
        {
            if (suggestSM.leaderboardType == LeaderboardType.AccSaber)
            {
                filteredSuggestions = filteredSuggestions
                    .Where(c => SongLibrary.HasAnySongCategory(c, accSaberPlaylistCategories))
                    .ToList();
            }
        }

        //Make Playlist
        public void CreatePlaylist()
        {
            songSuggest.status = "Making Playlist";

            //Select requested count best suggestions
            songSuggestIDs = filteredSuggestions.Take(playlistLength).ToList();

            PlaylistManager playlist = new PlaylistManager(settings.PlaylistSettings) { songSuggest = songSuggest };
            playlist.AddSongs(songSuggestIDs);
            playlist.Generate();
        }

        internal void ShowCache()
        {
            songSuggest.log?.WriteLine($"Cache Counts");
            songSuggest.log?.WriteLine($"sortedSuggestions : {sortedSuggestions.Count()}");
            songSuggest.log?.WriteLine($"filteredSuggestions : {filteredSuggestions.Count()}");
            songSuggest.log?.WriteLine($"songSuggestIDs : {songSuggestIDs.Count()}");
            songSuggest.log?.WriteLine($"originSongIDs : {originSongIDs.Count()}");
            songSuggest.log?.WriteLine($"ignoreSongs : {ignoreSongs.Count()}");
            songSuggest.log?.WriteLine($"originSongs : {originSongs.endPoints.Count()}");
            double originSum = originSongs.endPoints.Sum(c => c.Value.songLinks.Count());
            songSuggest.log?.WriteLine($"originSongs entries : {originSum}");
            songSuggest.log?.WriteLine($"targetSongs : {targetSongs.endPoints.Count()}");
            double targetSum = targetSongs.endPoints.Sum(c => c.Value.songLinks.Count());
            songSuggest.log?.WriteLine($"originSongs entries : {targetSum}");
        }
    }

    //A filter contains a way to evaluate rankings and provide both an ordering of songs linked, as well as the strength (default methode can
    //be overwritten if special cases needs non standard spreads, such as Filters that adds specific bonusses or penalties in a limited scope.
    //e.g. Recently played penalty, less played songs bonus
    public abstract class Filter
    {
        //Default the filter to inactive and 0 strength until set.
        private Boolean active = false;
        private Boolean calculated = false;
        public Double filterStrength { get; set; } = 0;

        //Name of the Filter
        public abstract string Name();
        //Description of the Filter
        public abstract string Description();

        //SongID for lookup and relevant data for the song
        private SortedDictionary<String, FilterSongData> songData;
        private SongEndPointCollection originSongs;
        private SongEndPointCollection targetSongs;

        //Creates a new filter and sets the endpoint collections for calculations
        public Filter(SongEndPointCollection originSongs, SongEndPointCollection targetSongs)
        {
            this.originSongs = originSongs;
            this.targetSongs = targetSongs;
        }

        //Sets the strength of the filter as a value between 0 and 1 (0 sets the filter inactive)
        public void SetFilterStrength(double strength)
        {
            if (strength == 0)
            {
                filterStrength = 0;
                active = false;
            }
            else
            {
                filterStrength = strength;
                active = true;
            }
        }

        public Boolean IsActive()
        {
            return active;
        }

        //Make the pre calculations for the filter so order can be made.
        public abstract void Calculate();

        //A songs default multiplier as a value based on filter strength averaged around 1. Some filters may use other caluclations.
        public double SongMultiplier(String songID)
        {
            //Returns 1 if the filter is inactive, or if it has not been calculated yet
            if (!active) return 1;
            if (!calculated) return 1;

            //Since we start with 0 index, the max song is 1 less than the elements.
            double maxCount = songData.Count() - 1;
            //For the calculations we use the 0 index rank, so we get a 0 to 1 spread.
            double songRank = songData[songID].songRank - 1;
            //A value from 0 to 1
            double unfilteredStrength = songRank / maxCount;
            //Reduce the spread with the filterstrength.
            double filteredStrength = unfilteredStrength * filterStrength;
            //Move the found strength up to center around 1 (Lowest value is half a filterStrength lower, and max is half a filterStrength higher)
            double strength = filteredStrength + 1 - (filterStrength / 2);
            return strength;
        }
    }

    //Contains song specific data for a filter to store, along with a default Filter Values per song basis.
    public abstract class FilterSongData
    {
        public String songID;
        //Rank of song starting with 1 for best
        public int songRank;

    }
    public class FilterManager
    {

    }
}