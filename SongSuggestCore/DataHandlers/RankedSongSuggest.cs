using System;
using System.Collections.Generic;
using System.Linq;
using PlaylistNS;
using System.Diagnostics;
using LinkedData;
using Settings;
using SongSuggestNS;
using Curve;
using BanLike;
using SongLibraryNS;
using ScoreSabersJson;

namespace Actions
{
    public class RankedSongSuggest
    {
        //Output Active?
        bool showDetailedOutout = false;

        public SongSuggest songSuggest { get; set; }
        //Information for the GUI on how far the update progress is.

        private SuggestSourceManager suggestSM;
        [Obsolete("External calls should not access Context data. This should be moved into Ranked Song Suggest when possible. This is a workaround to allow temporary access.")]
        public Top10kPlayers Leaderboard()
        {
            return suggestSM.Leaderboard();
        }

        public double songSuggestCompletion { get; set; }

        //All found songs ordered by current filter settings.
        public List<String> sortedSuggestions { get; set; }

        //Found songs with ignored songs removed
        public List<String> filteredSuggestions { get; set; }
        //ID's of songs used in actual playlist
        public List<String> songSuggestIDs { get; set; }

        //Settings file for current active session of RankedSongsSuggest, can be replaced/updated for refiltering.
        public SongSuggestSettings settings { get; set; }

        //Debug Timer, used to see if calculations end up being too slow.
        Stopwatch timer = new Stopwatch();

        //List for songs selected as Origin.
        List<String> originSongIDs;

        //The two collections of endpoints of the Origin and Target songs from 10k player data based on Active Players data.
        //Active Player Songs = Top originSongsCount ranked and or liked songs
        //Origin = (10k player) Songs that match Active Players Songs
        //Target = Suggested Songs linked from Origin (within a single 10k player, 1 origin -> 19 target links of other top 20 songs.)
        //Only needs recalculation if Originsongs change, or the player improves a top originSongsCount score (less filtered betterAccCap suggestions).
        SongEndPointCollection originSongs;
        SongEndPointCollection targetSongs;

        //Amount of Players the user got linked to. Low count then we remove betterAccCap limits.
        //Still low count, then the suggestions may be strange (Way too hard songs), we make sure to evaluate songs
        //(compare the songs min and max range to the players max PP on any song, and remove
        //unrealistic suggestions)
        int minSongLinks = 300;
        int linkedPlayers = 0;
        int linkedSongs = 0;

        //Add Filler songs if player has a low source song set
        int targetFillers = 10;
        int minPlays = 40;

        //Value for how many spots must be expected to be improved before being shown in suggestions (unplayed songs are always shown)
        int improveSpots = 5;

        //Links for variables used in this class from Settings file. Classes are not done here
        public bool ignorePlayedAll => settings.IgnorePlayedAll;                //
        public int ignorePlayedDays => settings.IgnorePlayedDays;
        public bool ignoreNonImproveable => settings.IgnoreNonImproveable;
        public int requiredMatches => settings.RequiredMatches;
        public bool useLikedSongs => settings.UseLikedSongs;
        public bool fillLikedSongs => settings.FillLikedSongs;
        public bool useLocalScores => settings.UseLocalScores;
        public int extraSongs => settings.ExtraSongs;
        public int playlistLength => settings.PlaylistLength;
        public double betterAccCap { get => settings.BetterAccCap; set => settings.BetterAccCap = value; }
        public double worseAccCap { get => settings.WorseAccCap; set => settings.WorseAccCap = value; }
        public SongCategory accSaberPlaylistCategories => settings.AccSaberPlaylistCategories;
        int originSongsCount => settings.OriginSongCount;
        int extraSongsCount => settings.ExtraSongs;
        LeaderboardType leaderboard => settings.Leaderboard;





        //Filter Results
        public List<String> distanceFilterOrdered;
        public List<String> styleFilterOrdered;
        public List<String> overWeightFilterOrdered;
        //in test        
        //PP for new distance
        List<String> ppFilterOrdered;
        //PP local vs Global
        List<String> ppLocalVSGlobalOrdered;

        //Creates a playlist with playlist count suggested songs based on the link system.
        public void SuggestedSongs()
        {
            suggestSM = new SuggestSourceManager()
            {
                songSuggest = this.songSuggest,
                scoreLocation = useLocalScores ? ScoreLocation.LocalScores : ScoreLocation.ScoreSaber,
                leaderboardType = leaderboard
            };

            songSuggest.log?.WriteLine("Starting Song Suggest");

            //Sets the lower quality suggestions to false, different parts of the song evaluations can turn it true.
            songSuggest.lowQualitySuggestions = false;

            //Setup Base Linking (song links).
            CreateLinks();

            //Generate the different filters rankings. (Calculate Scores, and Rank them)
            CreateFilterRanks();

            //Takes the orderes lists runs through them and assign points based on order.
            EvaluateFilters();

            //Removes filtered songs (Played/Played within X days/Banned/Not expected improveable atm) depending on settings
            RemoveIgnoredSongs();

            //Performs special selection filtering for Leaderboards
            LeaderboardSpecialFiltering();

            //Creates the playist of remaining songs
            CreatePlaylist();

            //----- Console Writeline for Debug -----
            songSuggest.log?.WriteLine($"Players Linked: {linkedPlayers}");
            songSuggest.log?.WriteLine($"Songs Linked: {linkedSongs}");
            songSuggest.log?.WriteLine($"Playlist Generation Done: {timer.ElapsedMilliseconds}ms");

            timer.Stop();
            songSuggest.log?.WriteLine($"Time Spent: {timer.ElapsedMilliseconds}ms");

            //Outputs results in the Console with how the different styles rankings
            ConsoleWriteStyleBreakdown();
        }

        public void Recalculate()
        {
            timer.Reset();
            timer.Start();
            songSuggest.log?.WriteLine("Starting Recalculations");
            EvaluateFilters();
            RemoveIgnoredSongs();
            CreatePlaylist();
            songSuggest.log?.WriteLine($"Recalculations Done: {timer.ElapsedMilliseconds}ms");

            ////Outputs results in the Console with how the different styles rankings
            ConsoleWriteStyleBreakdown();
        }

        //Creates the needed linked data for song evaluation for the Active Player.
        //Until Active Players top originSongsCount scores change *1 (replaced or better scores) no need to recalculate
        //*1 (Liked songs if active changes also counts as an update)
        public void CreateLinks()
        {
            //Updating scores has external wait time of the API call, so restarting measurement for the remainder of the update.
            songSuggest.log?.WriteLine("Starts the timer");
            timer.Start();

            //Get Link Data
            songSuggest.status = "Finding Link Data";
            songSuggest.log?.WriteLine($"Done loading and generating the top10k player data: {timer.ElapsedMilliseconds}ms");

            //Find the Origin Song ID's based on Active Players data.
            songSuggest.status = "Finding Songs to Match";
            originSongIDs = OriginSongs(useLikedSongs, fillLikedSongs);

            ////Create the Origin Points collection, and have them linked in Origin Points (Permabanned songs are not to be used).
            //songSuggest.status = "Preparing Origin Songs";
            //originSongs = CreateOriginPoints(originSongIDs, songSuggest.songBanning.GetPermaBannedIDs());
            ////Check if there is no links and if, retry this time without removing lower acc links
            //if (linkedPlayers < minSongLinks)
            //{
            //    songSuggest.lowQualitySuggestions = true;
            //    songSuggest.log?.WriteLine("Not Enough Player Links Found ({0}) with Acc Limit on. Activate Limit Breaker.", linkedPlayers);
            //    betterAccCap = Double.MaxValue;
            //    originSongs = CreateOriginPoints(originSongIDs, songSuggest.songBanning.GetPermaBannedIDs());
            //}

            //songSuggest.log?.WriteLine("Completion: " + (songSuggestCompletion * 100) + "%");
            //songSuggest.log?.WriteLine($"Origin Endpoint Done: {timer.ElapsedMilliseconds}ms");

            ////Link the Target end points in the Target End Point Collection
            //targetSongs = CreateTargetPoints();
            //songSuggest.log?.WriteLine("Completion: " + (songSuggestCompletion * 100) + "%");
            //songSuggest.log?.WriteLine($"Suggest Endpoint Done: {timer.ElapsedMilliseconds}ms");

            songSuggest.log?.WriteLine("Completion: " + (songSuggestCompletion * 100) + "%");

            GenerateLinks(originSongIDs, songSuggest.songBanning.GetPermaBannedIDs());

            songSuggest.log?.WriteLine("Completion: " + (songSuggestCompletion * 100) + "%");
            songSuggest.log?.WriteLine($"Suggest Linking Done: {timer.ElapsedMilliseconds}ms");

            if (linkedPlayers < minSongLinks)
            {
                songSuggest.lowQualitySuggestions = true;
                songSuggest.log?.WriteLine("Not Enough Player Links Found ({0}) with Acc Limit on. Activate Limit Breaker.", linkedPlayers);
                betterAccCap = Double.MaxValue;
                GenerateLinks(originSongIDs, songSuggest.songBanning.GetPermaBannedIDs());
                songSuggest.log?.WriteLine("Completion: " + (songSuggestCompletion * 100) + "%");
                songSuggest.log?.WriteLine($"Suggest Linking Done: {timer.ElapsedMilliseconds}ms");
            }
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

            //Filter on PP value compared to users songs PP values
            distanceFilterOrdered = targetSongs.endPoints.Values.OrderByDescending(s => s.weightedRelevanceScore).Select(p => p.songID).ToList();

            //Filter on how much over/under linked a song is in the active players data vs the global player population
            styleFilterOrdered = targetSongs.endPoints.Values.OrderBy(s => (0.0 + suggestSM.Leaderboard().top10kSongMeta[s.songID].count) / (0.0 + s.proportionalStyle)).Select(p => p.songID).ToList();
            //Old bias for more suggestions from highly linked songs.
            //styleFilterOrdered = targetSongs.endPoints.Values.OrderBy(s => (0.0 + top10kPlayers.top10kSongMeta[s.songID].count) / (0.0 + s.songLinks.Count())).Select(p => p.songID).ToList();

            //Filter on how the selected songs rank are better than average
            overWeightFilterOrdered = targetSongs.endPoints.Values.OrderBy(s => s.averageRank).Select(p => p.songID).ToList();

            //***test
            //Filter on what the expected PP would be on a song.
            ppFilterOrdered = targetSongs.endPoints.Values.OrderByDescending(s => s.estimatedPP).Select(p => p.songID).ToList();
            //Filter on which PP is strongest in Local vs Global
            ppLocalVSGlobalOrdered = targetSongs.endPoints.Values.OrderByDescending(s => s.localVSGlobalPP).Select(p => p.songID).ToList();
        }

        //Creates a list of the origin songs (Liked and top originSongsCount)
        public List<String> OriginSongs(Boolean useLikedSongs, Boolean fillLikedSongs)
        {
            List<String> originSongsIDs = new List<String>();
            //Add Liked songs.
            songSuggest.log?.WriteLine("Use Liked Songs: " + useLikedSongs);

            if (useLikedSongs) originSongsIDs.AddRange(suggestSM.LikedSongs());
            int targetCount = originSongsIDs.Count();


            songSuggest.log?.WriteLine("Liked Songs in list: " + originSongsIDs.Count());

            //Add the standard origin songs if either normal mode of filler is activated
            if (!useLikedSongs || fillLikedSongs)
            {
                //update targetsongs to either originSongsCount, or liked songs total, whichever is larger
                targetCount = Math.Max(originSongsCount, targetCount);

                originSongsIDs.AddRange(SelectPlayedOriginSongs());

                originSongsIDs = originSongsIDs
                    .Distinct()         //Remove Duplicates
                    .ToList();

                songSuggest.log?.WriteLine("Liked + Played Songs in list: " + originSongsIDs.Count());

                //int neededFillers = targetFillers - originSongsIDs.Count();
                int neededFillers = (originSongsIDs.Count == 0) ? targetFillers : 0;

                if (neededFillers > 0)
                {
                    //Too few source songs, so autoactivating "Limitbreak" and set low match warning
                    songSuggest.log?.WriteLine($"Less than {targetFillers} played. Filler Songs Activated");
                    betterAccCap = Double.MaxValue;
                    songSuggest.lowQualitySuggestions = true;

                    //Find all songs in the leaderboard with at least a minimum of records amount, and sort them by Max Score, so easiest is first, and remove already approved songs
                    var fillerSongs = suggestSM.Leaderboard().top10kSongMeta
                        .Where(c => c.Value.count >= minPlays)
                        .OrderBy(c => c.Value.maxScore)
                        .Select(c => c.Value.songID)
                        .ToList();

                    originSongsIDs.AddRange(fillerSongs);

                    originSongsIDs = originSongsIDs
                        .Distinct()         //Remove Duplicates
                        .Take(targetFillers)
                        .ToList();

                    songSuggest.log?.WriteLine("Liked + Played + Filler Songs in list: " + originSongsIDs.Count());
                }

                originSongsIDs = originSongsIDs
                    .Take(targetCount)  //Try and get originSongsCount or all liked whichever is larger
                    .ToList();
                songSuggest.log?.WriteLine("Final Songs in list: " + originSongsIDs.Count());
            }
            return originSongsIDs;
        }

        //Goal here is to get a good sample of a players songs that are not banned. The goal is try and find originSongsCount candidates to represent a player.
        //We filter out round up 25% worst scores (keeping at least 1) to allow progression on actual scores on lower song counts by filtering bad fits earlier
        //Then we filter out the requested portion of low accuracy songs.
        public List<String> SelectPlayedOriginSongs()
        {
            double maxKeepPercentage = 0.75;                                                //Percent of plays to keep on players with low playcount (Rounding is handled locally)

            //Find available songs
            var filteredSongs = suggestSM.PlayerScoresIDs()                                 //Grab songID's for songs matching the given Suggest Context from Source Manager
                .Where(c => !songSuggest.songBanning.IsPermaBanned(c, BanType.SongSuggest)) //Remove Perma Banned Songs
                .OrderByDescending(value => suggestSM.PlayerScoreValue(value))              //Order Songs by value
                .ToList();
            //If it is AccSaber leaderboard, filter origin songs if needed
            if (suggestSM.leaderboardType == LeaderboardType.AccSaber)
                {
                //The max spread on Origin Songs (some acc players only focus on a single or two leaderboards, so getting random songs from other leaderboards with low
                //and or old AP scores makes little sense).
                double maxSpreadBetweenSongs = 0.8;


                SongCategory activeCategories = suggestSM.LeaderboardSongCategory();

                int targets = settings.OriginSongCount;                                                 //Target songs to end up with
                int maxKeep = (int)Math.Ceiling(maxKeepPercentage * filteredSongs.Count);               //Calculate max songs to keep based on plays
                int adjustedTargets = Math.Min(maxKeep, targets);                                       //Reduces targets if needed.
                //Counts how many flags are active in the activeCategories defined for Acc Saber (3). This is if there later are similar groupings it can be done without hardcoding.
                int activeCategoriesCount = Enum.GetValues(typeof(SongCategory))                        //Get all enums as assigned integers
                    .Cast<SongCategory>()                                                               //Switch them to the enum type
                    .Count(value => activeCategories.HasFlag(value));                                   //Counts how many times the activeCategories has a matching flag
                //We round up to ensure enough values to hit our goal, and add the +1 to allows a little bias towards highest scoring groups in selection
                int groupSamples = ((int)Math.Ceiling((double)targets / activeCategoriesCount)) + 1;    //Calculate samples per active group


                // Lets get the sample size from all Acc Saber categories, and then reduce the list if needed.
                filteredSongs = Enum.GetValues(typeof(SongCategory))                                                                //Get Int values for all SongCategory
                    .Cast<SongCategory>()                                                                                           //Convert the list to actual enum type
                    .Where(value => activeCategories.HasFlag(value))                                                                //Reduce list to active groups only
                    .Select(group => filteredSongs.Where(candidate => songSuggest.songLibrary.HasAllSongCategory(candidate, group)))//Sort candidates into groups based on their enum
                    .SelectMany(groupCandidates => groupCandidates.Take(groupSamples))                                              //Combine all lists to a single with a max selection
                    .OrderByDescending(candidate => suggestSM.PlayerScoreValue(candidate))                                          //Order has been changed by groups, so we need to resort
                    .Take(targets)                                                                                                  //Reduce the list to the found target
                    .ToList();                                                                                                      //And turn it back into a list for later processing

                //Find the minimum required AP target for a song
                double minTargetAP = suggestSM.PlayerScoreValue(filteredSongs.First()) * maxSpreadBetweenSongs;

                //Filter out songs with a worse AP than the target.
                filteredSongs = filteredSongs.Where(c => suggestSM.PlayerScoreValue(c) > minTargetAP).ToList();
            }
            //Default selection. (Non Acc Saber)
            else
            {
                //To ensure worst songs are always removed (progression while getting enough songs) we only keep a certain percent of songs (75% default)
                int valueSongCount = filteredSongs.Count();
                valueSongCount = (int)(maxKeepPercentage * valueSongCount);     //Reduce the list to 75% best
                if (valueSongCount == 0) valueSongCount = 1;                    //If 1 is available, 1 should always be selected, but outside this goal is to reduce to 75% rounded down

                //Find the target song count after removing accuracy adjustments
                double percentToKeep = (double)originSongsCount / (originSongsCount + extraSongsCount);
                int accSongCount = (int)Math.Ceiling(percentToKeep * valueSongCount);

                //Get the songs by 
                filteredSongs = filteredSongs
                    .Take(valueSongCount)                                       //Grab 75% best of scores
                    .Take(originSongsCount + extraSongsCount)                     //Grab up to the portion that is default cap before acc sorting
                    .OrderByDescending(c => suggestSM.PlayerAccuracyValue(c))   //Sort by acc
                    .Take(accSongCount)                                         //Grab the goal of acc related songs (relevant if less than originSongsCount should be kept to keep a matching % removed instead)
                    .Take(originSongsCount)                                     //Reduce the with acc selection to originSongsCount best acc songs (default reduction so worst acc is removed)   
                    .OrderByDescending(c => suggestSM.PlayerScoreValue(c))      //Reorder back to Score Value (relevant only if player got other prioritised songs like Liked songs before reducing suggest list at higher levels)
                    .ToList();
            }


            //Debug code for showing actual selected songs.
            songSuggest.log?.WriteLine("Selected Origin Songs");
            foreach (var songID in filteredSongs)
            {
                if (songSuggest.songLibrary.songs.ContainsKey(songID))
                {
                    var song = songSuggest.songLibrary.songs[songID];
                    var songCategory = song.songCategory & suggestSM.LeaderboardSongCategory();
                    var songName = songSuggest.songLibrary.GetDisplayName(songID);
                    songSuggest.log?.WriteLine($"SongCategory: {songCategory,-16}   Score: {suggestSM.PlayerScoreValue(songID),8:N2}    {songName}");
                }
                else songSuggest.log?.WriteLine($"{songID}");
            }

            //Returns the found songs.
            return filteredSongs;
        }


        //**TST** Trying to combine the linked player/songs object in a LINQ.
        public void GenerateLinks(List<string> originSongIDs, List<string> ignoreSongs)
        {
            //Generate the initial endpoints as a dictionary
            var endPoints = originSongIDs
                .Select(songID => new SongEndPoint { songID = songID })
                .ToDictionary(endpoint => endpoint.songID, endpoint => endpoint);

            //Add them to the sorted dictionary ... might just use a dictionary later on as ordering should not matter.
            originSongs = new SongEndPointCollection() { endPoints = new SortedDictionary<string, SongEndPoint>(endPoints) };

            //Declaration of Funcs with the logic needed for the main creation of SongLinks, as well as needed variables

            var scoreBoard = suggestSM.Leaderboard();
            int maxRank = scoreBoard.top10kPlayers.Max(c => c.rank);

            //Filters players that should not be used. (Only filters out the active player currently, previous versions checked global rank and such, but it did not improve results).
            Func<Top10kPlayer, bool> playerFilter = player =>
            {
                return player.id != songSuggest.activePlayer.id;
            };

            //Filter the active score, we check first for the score having a match with origin songs, then if the score has a Score Value (liked songs will not have this but should be kept).
            //And we make sure it is within the better/worse Acc Cap range. Finally once a score is validated we increase validated score counts.
            Func<Top10kScore, bool> scoreFilter = score =>
            {
                //Return false if song is not in the list of songs we are looking for.
                if (!originSongIDs.Contains(score.songID)) return false;

                //Returns false if the score is outside limits (and not unrated)
                double playerSongValue = suggestSM.PlayerScoreValue(score.songID);
                if (!(playerSongValue != 0 || (playerSongValue * betterAccCap > score.pp && playerSongValue * worseAccCap < score.pp))) return false;
                linkedPlayers++;
                return true;
            };

            Func<Top10kScore, Top10kScore, bool> songLinkFilter = (originSong, suggestedSong) =>
            {
                return suggestedSong.rank != originSong.rank && !ignoreSongs.Contains(suggestedSong.songID);
            };

            //Generate the Song Link, as well as set the aproximate completion, as majority of loop should be in this part
            Func<Top10kPlayer, Top10kScore, Top10kScore, SongLink> generateSongLink = (player, originSong, suggestedSong) =>
            {
                double localPercentDone = (double)player.rank / maxRank;
                double localGroupStart = 0.22;
                double localGroupsTotalValue = 0.22;
                songSuggestCompletion = localGroupStart + (localPercentDone * localGroupsTotalValue);
                return new SongLink() { playerID = player.id, originSongScore = originSong, targetSongScore = suggestedSong };
            };

            //We are preparing the actual linking between a playrs origin songs to their suggested target songs.
            //origin -> matching leaderboard player -> other leaderboard players songs
            var links = scoreBoard.top10kPlayers                                        //Get reference to the top 10k players data                        
                .Where(playerFilter)                                                    //Remove active player so player does not use their own data
                .SelectMany(scoreBoardPlayer => scoreBoardPlayer.top10kScore            //Get the players scores
                    .Where(scoreFilter)                                                 //Remove scores that does not fit filtering
                    .Select(OriginSong => new { Player = scoreBoardPlayer, OriginSong = OriginSong }) //Keep variables needed for creating SongLinks
                )
                .SelectMany(playerScore => playerScore.Player.top10kScore               //Get the players other scores to link with themselves.
                    .Where(SuggestedSong => songLinkFilter(playerScore.OriginSong, SuggestedSong))      //Remove the selflink
                    .Select(SuggestedSong => new { Player = playerScore.Player, OriginSong = playerScore.OriginSong, SuggestedSong = SuggestedSong }) //Store needed variables again
                )
                .Select(linkData => new { Link = generateSongLink(linkData.Player, linkData.OriginSong, linkData.SuggestedSong), Index = linkData.Player.rank })  //Create songlinks for further processing
                .ToList();

            //Update completion to partial completion
            songSuggestCompletion = 0.44;

            //Update found links
            linkedSongs = links.Count();

            //Reset the targetPoints endpoint
            targetSongs = new SongEndPointCollection();

            //Link the links to the endpoints
            foreach (var item in links)
            {
                //Add the songlink to the origin list
                string originSongID = item.Link.originSongScore.songID;
                originSongs.endPoints[originSongID].songLinks.Add(item.Link);

                //Create the target endpoint if needed.
                string targetSongID = item.Link.targetSongScore.songID;
                if (!targetSongs.endPoints.ContainsKey(targetSongID))
                {
                    targetSongs.endPoints.Add(targetSongID, new SongEndPoint { songID = targetSongID });
                }

                //Add the songlink to the target list
                targetSongs.endPoints[targetSongID].songLinks.Add(item.Link);

                //Update complete
                double localPercentDone = (double)item.Index / maxRank;
                double localGroupStart = 0.44;
                double localGroupsSize = 0.22;
                songSuggestCompletion = localGroupStart + (localPercentDone * localGroupsSize);
            }

            //Update completion to full completion
            songSuggestCompletion = 0.66;
        }

        //Sets the Origin Endpoint collection up, and links all the SongLinks to the Origin points
        public SongEndPointCollection CreateOriginPoints(List<String> originSongIDs, List<String> ignoreSongs)
        {
            originSongs = new SongEndPointCollection();

            songSuggest.status = "Searching for Songs from Origin Songs";
            int percentDoneCalc = 0;
            //Add an endpoint for each selected originsong
            foreach (String songID in originSongIDs)
            {
                SongEndPoint songEndPoint = new SongEndPoint { songID = songID };
                originSongs.endPoints.Add(songID, songEndPoint);
            }

            //Prepare the starting endpoints for the above selected songs and tie them to the origin collection, ignoring the player itself.

            //Reset link count for new generation.
            linkedSongs = 0;
            linkedPlayers = 0;
            var scoreBoard = suggestSM.Leaderboard();

            foreach (Top10kPlayer scoreBoardPlayer in scoreBoard.top10kPlayers.Where(c => c.id != songSuggest.activePlayer.id))
            {
                //Loop the Scoreboard Players songs that are also among preselected songs.
                foreach (Top10kScore scoreBoardSong in scoreBoardPlayer.top10kScore.Where(c => originSongs.endPoints.ContainsKey(c.songID)))
                {
                    //Only check if score is high enough if player actually played the song (songValue > 0) as player may not have played Liked or Fillers in the active scoreboard
                    double playerSongValue = suggestSM.PlayerScoreValue(scoreBoardSong.songID);
                    bool validSong = playerSongValue != 0 ? playerSongValue * betterAccCap > scoreBoardSong.pp && playerSongValue * worseAccCap < scoreBoardSong.pp : true;

                    //Skip link if the targetsongs PP is too high compared to original players score
                    if (validSong)
                    {
                        //Each player can be counted originSongsCount times, as there is originSongsCount songs to link from.
                        linkedPlayers++;
                        //Loop songs again for endpoints, skipping linking itself, as well as ignoreSongs
                        foreach (Top10kScore suggestedSong in scoreBoardPlayer.top10kScore.Where(suggestedSong => suggestedSong.rank != scoreBoardSong.rank && !ignoreSongs.Contains(suggestedSong.songID)))
                        {
                            linkedSongs++;
                            SongLink songLink = new SongLink
                            {
                                playerID = scoreBoardPlayer.id,
                                originSongScore = scoreBoardSong,
                                targetSongScore = suggestedSong
                            };
                            originSongs.endPoints[scoreBoardSong.songID].songLinks.Add(songLink);
                        }
                    }
                }
                percentDoneCalc++;
                songSuggestCompletion = (0.0 + (4.0 * percentDoneCalc / 10000)) / 6.0;
            }

            //Create the suggested songs Endpoints
            songSuggest.status = "Sorting Found Songs";
            targetSongs = new SongEndPointCollection();
            return originSongs;
        }

        //Sets the Target Endpoint collection up, and links the origin songs to their respective target endpoint
        public SongEndPointCollection CreateTargetPoints()
        {
            SongEndPointCollection targetSongs = new SongEndPointCollection();
            //Creates a new target end point collection to work with
            int percentDoneCalc = 0;
            //loop all origin songs
            foreach (SongEndPoint songEndPoint in originSongs.endPoints.Values)
            {
                //loop all links in that active origin song
                foreach (SongLink songLink in songEndPoint.songLinks)
                {
                    //If song is not present, make an endpoint for it
                    if (!targetSongs.endPoints.ContainsKey(songLink.targetSongScore.songID))
                    {
                        SongEndPoint suggestedSongEndPoint = new SongEndPoint { songID = songLink.targetSongScore.songID };
                        targetSongs.endPoints.Add(songLink.targetSongScore.songID, suggestedSongEndPoint);
                    }

                    //add endpoint to suggested song
                    targetSongs.endPoints[songLink.targetSongScore.songID].songLinks.Add(songLink);
                }
                percentDoneCalc++;
                songSuggestCompletion = (4.0 + (1.5 * percentDoneCalc / originSongs.endPoints.Values.Count())) / 6.0;
            }
            return targetSongs;
        }

        //Generate the weighting for the different Filters and stores them in the Endpoint Data.
        public void EvaluateSongs()
        {
            //TODO: Should be split into new Distance calculation, and overWeight calculculation, and update the variables needed to be sent.

            //Calculate strength for filter rankings in the SongLink data with needed data sent along.
            targetSongs.SetRelevance(this, originSongs.endPoints.Count(), requiredMatches);
            targetSongs.SetStyle(originSongs);

            //New test to try and guess PP
            targetSongs.SetPP(songSuggest);

            //New test to compare local group vs global group stuff
            targetSongs.SetLocalPP(songSuggest);

        }

        //Takes the orderes suggestions and apply the filter values to their ranks, and create the nameplate orderings
        public void EvaluateFilters()
        {
            Dictionary<String, double> totalScore = new Dictionary<String, double>();

            //Get Base Weights reset them from % value to [0-1], and must not all be 0)
            double modifierDistance = 0.0; //settings.filterSettings.modifierPP / 100;  //Deprecated so disabled in future code.
            double modifierStyle = settings.FilterSettings.modifierStyle / 100;
            double modifierOverweight = settings.FilterSettings.modifierOverweight / 100;
            //***test (hardcoded to max)
            double modifierPP = 0;
            double modifierPPLocalVSGlobal = 0;

            //reset if all = 0, reset to 100%.
            if (modifierStyle == 0 && modifierOverweight == 0) modifierStyle = modifierOverweight = 1.0;

            songSuggest.log?.WriteLine($"Style: {modifierStyle} Overweight: {modifierOverweight}");

            //Get count of candidates, and remove 1, as index start as 0, so max value is songs-1
            double totalCandidates = distanceFilterOrdered.Count() - 1;

            //As all 3 filters contain same ID's we can loop the song IDs from either of the filters, and calculate their combined score.
            foreach (String distanceCandidate in distanceFilterOrdered)
            {
                //Get the location of the candidate in the list as a [0-1] value
                double distanceValue = distanceFilterOrdered.IndexOf(distanceCandidate) / totalCandidates;
                double styleValue = styleFilterOrdered.IndexOf(distanceCandidate) / totalCandidates;
                double overWeightedValue = overWeightFilterOrdered.IndexOf(distanceCandidate) / totalCandidates;
                //***test
                //ppValue for distance replacement
                double ppValue = ppFilterOrdered.IndexOf(distanceCandidate) / totalCandidates;
                //ppLocalvsGlobal
                double ppLocalVSGlobalValue = ppLocalVSGlobalOrdered.IndexOf(distanceCandidate) / totalCandidates;

                //Switch the range from [0-1] to [0.5-1.5] and reduce the gap based on modifier weight.
                //**Spacing between values may be more correct to consider a log spacing (e.g. due to 1.5*.0.5 != 1)
                //**But as values are kept around 1, and it is not important to keep total average at 1, the difference in
                //**Actual ratings in the 0.5 to 1.5 range is minimal at the "best suggestions range" even with quite a few filters.
                //**So a "correct range" of 0.5 to 2 would give a higher penalty on bad matches on a single filter, so current
                //**setup means a song must do worse on more filters to actual lose rank, which actually may be prefered.
                double distanceTotal = distanceValue * modifierDistance + (1.0 - 0.5 * modifierDistance);
                double styleTotal = styleValue * modifierStyle + (1.0 - 0.5 * modifierStyle);
                double overWeightedTotal = overWeightedValue * modifierOverweight + (1.0 - 0.5 * modifierOverweight);
                double ppTotal = ppValue * modifierPP + (1.0 - 0.5 * modifierPP);
                double ppLocalVSGlobalTotal = ppLocalVSGlobalValue * modifierPPLocalVSGlobal + (1.0 - 0.5 * modifierPPLocalVSGlobal);

                //Get the songs multiplied average 
                double score = distanceTotal * styleTotal * overWeightedTotal * ppTotal * ppLocalVSGlobalTotal;

                //Add song ID and its score to a list for sorting and reducing size for the playlist generation
                totalScore.Add(distanceCandidate, score);
            }

            //Sort list, and get song ID's only
            sortedSuggestions = totalScore.OrderBy(s => s.Value).Select(s => s.Key).ToList();

            //The suggestions may be weak if there is a low amount of Links, so current suggestions needs evaluation to make
            //sure if link count is low that potential too hard songs are removed.
            //readd all remaining songs to the ends of the list from easiest to hardest (if they are on enough top 20's), as this makes
            //it possible to filter disliked, too hard songs etc normally, and always provide a list of requested amount of songs.
            LowLinkEvaluation();
        }

        //There is not enough links to have a high confidence in all results are doable
        //So removes any songs outside expected range in min/max PP values
        //Then takes all remaining songs with at least a few plays and readd them after actual suggestions to make sure player
        //Can ban/have recently played songs removed without dropping under requested songs in suggestions.
        public void LowLinkEvaluation()
        {
            //Skip this if enough links. (It is possible that removing the low accuracy filter ended up giving enough links that song
            //suggestions are good, even if the players acc is so low that the Better Acc filter was triggered).
            if (linkedPlayers < minSongLinks)
            {
                songSuggest.log?.WriteLine("Low Linking found");
                //Enable the warning for additonal steps to ensure enough songs.
                songSuggest.lowQualitySuggestions = true;

                //Get the players max score via the players played songs, and avoid errors with Max on an empty list.
                var playerScores = suggestSM.PlayerScoresIDs();
                double playerMaxScore = playerScores.Count() > 0 ? playerScores.Max(c => suggestSM.PlayerScoreValue(c)) : 0;

                songSuggest.log?.WriteLine("Max Score Value: " + playerMaxScore);
                songSuggest.log?.WriteLine("Filtering out songs that are expected too hard");
                songSuggest.log?.WriteLine("Songs before filtering: {0}", sortedSuggestions.Count());

                //Remove songs that have too high a min PP (expected song is outside the players skill)                
                //Remove songs that have too high a max PP (expected players Acc is lacking)
                //Remove songs without 3 plays (The songs scores could be random values, so rather remove them for now)
                sortedSuggestions = sortedSuggestions
                    .Where(c => suggestSM.Leaderboard().top10kSongMeta[c].minScore < 1.2 * playerMaxScore
                    && suggestSM.Leaderboard().top10kSongMeta[c].maxScore < 1.5 * playerMaxScore
                    && suggestSM.Leaderboard().top10kSongMeta[c].count >= 3)
                    .ToList();

                songSuggest.log?.WriteLine("Songs left after filtering: {0}", sortedSuggestions.Count());

                //Find all songs with at least 3 plays, and sort them by MaxPP scores, so easiest is first, and remove already approved songs
                List<String> remainingSongs = suggestSM.Leaderboard().top10kSongMeta
                    .Where(c => c.Value.count >= 3)
                    .OrderBy(c => c.Value.maxScore)
                    .Select(c => c.Value.songID)
                    .Except(sortedSuggestions)
                    .ToList();

                //Add the songs not already suggested to the list.
                sortedSuggestions.AddRange(remainingSongs);
            }
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
            List<String> ignoreSongs = CreateIgnoreLists(ignorePlayedAll ? -1 : ignorePlayedDays);
            filteredSuggestions = sortedSuggestions.Where(s => !ignoreSongs.Contains(s)).ToList();
        }

        //Create a List of songID's to filter out. Consider splitting it so Permaban does not get links, while
        //standard temporary banned, and recently played gets removed after.
        //Send -1 if all played should be ignored, else amount of days to ignore.
        public List<String> CreateIgnoreLists(int ignoreDays)
        {
            List<String> ignoreSongs = new List<String>();

            //Ignore recently/all played songs
            //Add either all played songs
            var playedSongs = suggestSM.PlayerScoresIDs();

            if (ignoreDays == -1)
            {
                ignoreSongs.AddRange(playedSongs);
                //ignoreSongs.AddRange(songSuggest.activePlayer.scores.Keys);
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
            ignoreSongs = ignoreSongs.Union(songSuggest.songBanning.GetBannedIDs()).ToList();

            //Add songs that is not expected to be improveable by X ranks
            if (ignoreNonImproveable)
            {
                ignoreSongs.AddRange(LeaderboardNonImproveableFiltering());
            }

            return ignoreSongs;
        }

        //Filtering out songs that are deemed nonimproveable.
        private List<string> LeaderboardNonImproveableFiltering()
        {
            List<string> ignoreSongs = new List<string>();

            if (suggestSM.leaderboardType == LeaderboardType.ScoreSaber)
            {
                List<String> activePlayersPPSortedSongs = songSuggest.activePlayer.scores.Values.OrderByDescending(p => p.pp).ToList().Select(p => p.songID).ToList();

                int suggestedSongRank = 0;
                foreach (string songID in sortedSuggestions)
                {
                    int currentSongRank = activePlayersPPSortedSongs.IndexOf(songID);
                    //Add songs ID to ignore list if current rank is not expected improveable by at least X spots, and it is not an unplayed song
                    if (currentSongRank < suggestedSongRank + improveSpots && currentSongRank != -1)
                    {
                        ignoreSongs.Add(songID);
                    }
                    suggestedSongRank++;
                }
            }
            if (suggestSM.leaderboardType == LeaderboardType.AccSaber)
            {
                foreach (SongCategory category in Enum.GetValues(typeof(SongCategory)).Cast<SongCategory>())
                {
                    //Skip categories not active.
                    if (!suggestSM.LeaderboardSongCategory().HasFlag(category)) continue;

                    //Get suggestions for the category
                    List<string> activeSongs = songSuggest.songLibrary.GetAllRankedSongIDs(category);
                    List<string> categorySortedSuggestions = sortedSuggestions
                                    .Intersect(activeSongs)
                                    .ToList();
                    //Get the players scores for the category and order them by best first
                    List<String> categorySortedPlayerScores = suggestSM.PlayerScoresIDs()
                                    .Where(c => songSuggest.songLibrary.HasAnySongCategory(c, category))
                                    .Intersect(activeSongs)
                                    .OrderByDescending(c => suggestSM.PlayerScoreValue(c))
                                    .ToList();
                    //List in both (ones that might be non improveable)
                    List<String> commonSongIDs = categorySortedSuggestions.Intersect(categorySortedPlayerScores).ToList();

                    //As we have 3 lists, improvespots should be made smaller
                    int adjustedImproveSpots = (int)Math.Ceiling((double)improveSpots / 3);

                    // Create the ignoreList by filtering commonSongIDs based on how many spots a song is set to be improveable before added.

                    List<String> categoryIgnoreSongs = commonSongIDs
                        .Where(song => categorySortedPlayerScores.IndexOf(song) - categorySortedSuggestions.IndexOf(song) < adjustedImproveSpots)
                         .ToList();

                    ignoreSongs.AddRange(categoryIgnoreSongs);

                    //Non improveable Log.
                    songSuggest.log?.WriteLine($"Song Category: {category}");
                    foreach (var song in categoryIgnoreSongs)
                    {
                        int playerRank = categorySortedPlayerScores.IndexOf(song) + 1;
                        int suggestRank = categorySortedSuggestions.IndexOf(song) + 1;
                        string songInfo = songSuggest.songLibrary.GetName(song);
                        songSuggest.log?.WriteLine($"Player Rank: {playerRank,3}   Suggest Rank: {suggestRank,3}   Song: {songInfo}");

                    }
                }
            }

            return ignoreSongs;
        }

        //Filtering that only applies to specific leaderboards
        private void LeaderboardSpecialFiltering()
        {
            if (suggestSM.leaderboardType == LeaderboardType.AccSaber)
            {
                filteredSuggestions = filteredSuggestions.Where(c => (songSuggest.songLibrary.songs[c].songCategory & accSaberPlaylistCategories) > 0).ToList();
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

        public void ConsoleWriteStyleBreakdown()
        {
            if (showDetailedOutout)
            {
                int rank = 0;
                foreach (string songID in sortedSuggestions)
                {
                    rank++;
                    List<String> playerRankedPP = songSuggest.activePlayer.scores.Values.OrderByDescending(p => p.pp).ToList().Select(p => p.songID).ToList();

                    int actualPlayerRank = playerRankedPP.IndexOf(songID) + 1;
                    String actualPlayerRankTxt = actualPlayerRank == 0 ? "-" : "" + actualPlayerRank;

                    int ppRank = distanceFilterOrdered.IndexOf(songID) + 1;
                    int styleRank = styleFilterOrdered.IndexOf(songID) + 1;
                    int owRank = overWeightFilterOrdered.IndexOf(songID) + 1;

                    String songName = songSuggest.songLibrary.GetName(songID);
                    String songDifc = songSuggest.songLibrary.GetDifficultyName(songID);

                    String songInfo = songName + " (" + songDifc + " - " + songID + ")";

                    double globalPP = suggestSM.Leaderboard().top10kSongMeta[songID].maxScore;

                    ////***test PP -> Distance
                    double playerPP = songSuggest.activePlayer.GetScore(songID);
                    double estimatedPP = targetSongs.endPoints.ContainsKey(songID) ? targetSongs.endPoints[songID].estimatedPP : 0;
                    double gainablePP = estimatedPP - playerPP;

                    //***Test PP local vs global
                    double localVSGlobalPP = targetSongs.endPoints.ContainsKey(songID) ? targetSongs.endPoints[songID].localVSGlobalPP : 0;

                    //songSuggest.log?.WriteLine("#:{0}\tPPdiff:{8}\testPP:{9}\tactPP:{10}\tAc:{1}\tPP:{2}\tSt:{3}\tOw:{4}\t: {5} ({6} - {7})", rank, actualPlayerRankTxt, ppRank, styleRank, owRank, songName, songDifc, songID, gainablePP,estimatedPP,playerPP);

                    //songSuggest.log?.WriteLine("#:{0}\t{2}\tRatio:{3}\t{1}", rank, songInfo, actualPlayerRankTxt, localVSGlobalPP);
                    songSuggest.log?.WriteLine("#:{0}\tDistance:{3}\tStyle :{4}\tOW:{5}\tActual:{2}\t{1}", rank, songInfo, actualPlayerRankTxt, ppRank, styleRank, owRank);
                }

                //    rank = 0;
                //    foreach (String songID in originSongIDs)
                //    {
                //        rank++;

                //        String songName = songSuggest.songLibrary.GetName(songID);
                //        String songDifc = songSuggest.songLibrary.GetDifficultyName(songID);

                //        String songInfo = songName + " (" + songDifc + " - " + songID + ")";

                //        songSuggest.log?.WriteLine("#:{0,3}   {1}",rank,songInfo);

                //    }

            }
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

    //--- Handling of data sources ---
    public class SuggestSourceManager
    {
        public SongSuggest songSuggest { get; set; }
        public ScoreLocation scoreLocation { get; set; } = ScoreLocation.ScoreSaber;
        public LeaderboardType leaderboardType { get; set; } = LeaderboardType.ScoreSaber;

        public List<String> PlayerScoresIDs()
        {
            switch (scoreLocation)
            {
                case ScoreLocation.ScoreSaber:
                    return songSuggest.activePlayer.scores.Values
                        .Select(c => c.songID)
                        .Intersect(songSuggest.songLibrary.GetAllRankedSongIDs(LeaderboardSongCategory()))
                        .ToList();
                case ScoreLocation.LocalScores:
                    return songSuggest.localScores.GetScores(LeaderboardSongCategory());
            }

            //Unknown handling detected
            throw new InvalidOperationException($"Unknown PlayerScoreIDs Source found: {scoreLocation}");
        }

        //Returns the value of a songID .. if song is unknown 0 is returned.
        public double PlayerScoreValue(string songID)
        {

            //Return recorded scores if leaderboard is cached, else calculate
            if (!(scoreLocation == ScoreLocation.LocalScores))
            {
                //Return results from the cached leaderboard data if it supports it.
                //e.g. Acc Saber can only be calculated.
                switch (leaderboardType)
                {
                    case LeaderboardType.ScoreSaber:
                        if (!songSuggest.activePlayer.scores.ContainsKey(songID)) return 0;
                        return songSuggest.activePlayer.scores[songID].pp;
                }
            }
            double accuracy = PlayerAccuracyValue(songID);
            double score = CalculatedScore(songID, accuracy);
            return score;
        }

        //Returns the acc value of a song, if song .. if song is unknown 0 is returned.
        public double PlayerAccuracyValue(string songID)
        {
            switch (scoreLocation)
            {
                case ScoreLocation.ScoreSaber:
                    if (!songSuggest.activePlayer.scores.ContainsKey(songID)) return 0;
                    return songSuggest.activePlayer.scores[songID].accuracy / 100;
                case ScoreLocation.LocalScores:
                    return songSuggest.localScores.GetAccuracy(songID);
            }

            //Unknown handling detected
            throw new InvalidOperationException($"Unknown PlayerAccuracyValue Source found: {scoreLocation}");
        }

        //Returns the Time of when a score was set.
        internal DateTime PlayerScoreDate(string songID)
        {
            switch (scoreLocation)
            {
                case ScoreLocation.ScoreSaber:
                    if (!songSuggest.activePlayer.scores.ContainsKey(songID)) return DateTime.MinValue;
                    return songSuggest.activePlayer.scores[songID].timeSet;
                case ScoreLocation.LocalScores:
                    return songSuggest.localScores.GetTimeSet(songID);
            }

            //Unknown handling detected
            throw new InvalidOperationException($"Unknown PlayerScoreDate Source found: {scoreLocation}");
        }

        public List<String> LikedSongs()
        {
            var allLikedSongs = songSuggest.songLiking.GetLikedIDs();
            var allSourceSongs = songSuggest.songLibrary.GetAllRankedSongIDs(LeaderboardSongCategory());

            return allLikedSongs.Intersect(allSourceSongs).ToList();
        }


        //Returns the calculated value of a songID .. unknown songs got 0 accuracy so 0 is returned.
        public double CalculatedScore(string songID, double accuracy)
        {
            switch (leaderboardType)
            {
                case LeaderboardType.ScoreSaber:
                    double starRating = songSuggest.songLibrary.songs[songID].starScoreSaber;
                    return ScoreSaberCurve.PP(accuracy, starRating);
                case LeaderboardType.AccSaber:
                    double complexityRating = songSuggest.songLibrary.songs[songID].complexityAccSaber;
                    double score = AccSaberCurve.AP(accuracy, complexityRating);
                    return score;
            }

            //Unknown handling detected
            throw new InvalidOperationException($"Unknown CalculatedScore Source found: {leaderboardType}");
        }

        public Top10kPlayers Leaderboard()
        {
            switch (leaderboardType)
            {
                case LeaderboardType.ScoreSaber:
                    return songSuggest.scoreSaberScoreBoard;
                case LeaderboardType.AccSaber:
                    return songSuggest.accSaberScoreBoard;

            }
            throw new InvalidOperationException($"Unknown ScoreBoardTopPlays Source found: {leaderboardType}");
        }


        //Returns the enums of the related ranked songs in the given Scoreboard.
        public SongCategory LeaderboardSongCategory()
        {
            switch (leaderboardType)
            {
                case LeaderboardType.ScoreSaber:
                    return SongCategory.ScoreSaber;

                case LeaderboardType.AccSaber:
                    return SongCategory.AccSaberTrue | SongCategory.AccSaberStandard | SongCategory.AccSaberTech;
            }
            throw new InvalidOperationException($"Unknown LeaderboardSongCategory Source found: {leaderboardType}");
        }

    }

    public enum ScoreLocation
    {
        ScoreSaber,
        LocalScores,
        BeatLeader
    }

    public enum LeaderboardType
    {
        ScoreSaber,
        AccSaber,
        BeatLeader
    }
}