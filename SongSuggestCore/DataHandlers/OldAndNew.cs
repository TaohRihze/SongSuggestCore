using PlaylistNS;
using SongSuggestNS;
using Settings;
using System.Linq;
using System.Collections.Generic;
using System;
using Curve;
using SongLibraryNS;

namespace Actions
{
    //Creates a playlist with the 100 oldest maps for a player.
    public class OldAndNew
    {
        public SongSuggest songSuggest { get; set; }
        public PlaylistManager playlist;
        private OldAndNewSettings settings; //Temporary settings storage for the sorting.

        public OldAndNew(SongSuggest songSuggest)
        {
            this.songSuggest = songSuggest;
        }

        public void GeneratePlaylist(OldAndNewSettings settings)
        {
            this.settings = settings;

            //Check if nothing is selected for the SongCategories for both played and unplayed, and if default the selection to .ScoreSaber on played
            if (settings.playedSongCategories == 0 && settings.unplayedSongCategories == 0)
            {
                settings.playedSongCategories = SongCategory.ScoreSaber;
            }

            //Create empty playlist, and reset output window.
            playlist = new PlaylistManager(settings.playlistSettings) { songSuggest = songSuggest };

            //Add up to 100 oldest song to playlist that has not been banned, and is within given parameters.
            songSuggest.status = $"Finding {settings.playlistLength} {settings.songSelection}";

            //Get a list of target songs
            var selectedSongs = SelectSongs();

            //Sort list by selection order
            selectedSongs = SortSongs(selectedSongs, settings.songSelection);

            //Reverse ordering if before selection if needed.
            if (settings.reverseSelectionOrdering) selectedSongs.Reverse();

            //Perform Weighted Sorting (0 = Standard, 1 = Random, rest is weighted search applied at various strength on list order.
            //values that is 0 is treated as neutral (e.g. PP on unplayed)).
            selectedSongs = WeightedSort(selectedSongs);

            //reduce list to the wanted amount of songs.
            selectedSongs = selectedSongs.Take(settings.playlistLength).ToList();

            //Skip if no reset is requested.
            if (settings.songOrdering != SongSortCriteria.None)
            {
                //Order the selected songs
                selectedSongs = SortSongs(selectedSongs, settings.songOrdering);

                //Reverse display ordering in playlist if needed.
                if (settings.reversePlaylistOrdering) selectedSongs.Reverse();
            }

            playlist.AddSongs(selectedSongs);

            //Generate and save a playlist with the selected songs in the playlist.
            songSuggest.status = "Generating Playlist";
            playlist.Generate();
        }

        private List<SongID> WeightedSort(List<SongID> selectedSongs)
        {
            if (settings.songWeighting == 0) return selectedSongs;              //Always Original Ordering
            if (settings.songWeighting == 1) return RandomOrder(selectedSongs); //Always Random

            //Default 0 value to an empty list
            List<SongID> zeroValuedSongs = new List<SongID>();

            //Find zeroValue songID's depending on Sort Criteria.
            switch (settings.songSelection)
            {
                // Unplayed songs -> zeroValue
                case SongSortCriteria.Accuracy:
                case SongSortCriteria.WorldPercentage:
                case SongSortCriteria.WorldRank:
                case SongSortCriteria.Age:
                case SongSortCriteria.PP:
                    zeroValuedSongs = selectedSongs
                        .Except(songSuggest.activePlayer.GetRankedScoreIDs())
                        .ToList();
                    break;

                // No star rating song -> zeroValue
                case SongSortCriteria.Star:
                    zeroValuedSongs = selectedSongs.Except(SongLibrary.GetAllRankedSongIDs(SongCategory.ScoreSaber)).ToList();
                    break;

                // No complexity rating song -> zeroValue
                case SongSortCriteria.Complexity:
                    zeroValuedSongs = selectedSongs.Except(SongLibrary.GetAllRankedSongIDs(SongCategory.AccSaberStandard | SongCategory.AccSaberTrue | SongCategory.AccSaberTech)).ToList();
                    break;
            }

            //Mark which songs has a value for weighted selection
            var valuedSongs = selectedSongs.Except(zeroValuedSongs).ToList();

            //If there is 1 or less songs with a value, then all songs have same odds, so might as well just use standard random return
            if (valuedSongs.Count <= 1) return RandomOrder(selectedSongs);

            double max = 2 - settings.songWeighting;
            double min = 0 + settings.songWeighting;

            //There is songs-1 steps, so lets find their size.
            double stepValue = (max - min) / (selectedSongs.Count - 1);
            double weight = max;

            List<WeightedPair> selectedSongPairs = new List<WeightedPair>();

            foreach (var song in valuedSongs)
            {
                selectedSongPairs.Add(new WeightedPair()
                {
                    Value = song,
                    Weight = weight
                });
                weight = weight - stepValue;
            }

            foreach (var song in zeroValuedSongs)
            {
                selectedSongPairs.Add(new WeightedPair()
                {
                    Value = song,
                    Weight = weight
                });
            }

            return new WeightedSelection(selectedSongPairs).GetAllRandom().Select(c => (SongID)c.Value).ToList();
        }

        //Returns a list of all songID's that match the setting files parameters.
        private List<SongID> SelectSongs()
        {
            //Get raw lists (unplayed is only songs for selected categories that has not been played)
            List<SongID> allPlayedSongs = songSuggest.activePlayer.GetRankedScoreIDs();
            List<SongID> allUnplayedSongs = SongLibrary.GetAllRankedSongIDs(settings.unplayedSongCategories).Except(allPlayedSongs).ToList();

            //Reduce the list to only whitelisted songs
            List<SongID> whitelistedPlayedSongs = allPlayedSongs.Intersect(WhiteListPlayed(true)).ToList();
            List<SongID> whitelistedUnplayedSongs = allUnplayedSongs.Intersect(WhiteListPlayed(false)).ToList();

            //Define active broken songs, and reduce relevant lists if needed.
            List<SongID> brokenSongs = songSuggest.songLibrary.GetAllRankedSongIDs(SongCategory.BrokenDownloads);

            //Remove broken songs if they are not selected.
            if ((settings.playedSongCategories & SongCategory.BrokenDownloads) == 0)
            {
                whitelistedPlayedSongs = whitelistedPlayedSongs.Except(brokenSongs).ToList();
            }
            if ((settings.unplayedSongCategories & SongCategory.BrokenDownloads) == 0)
            {
                whitelistedUnplayedSongs = whitelistedUnplayedSongs.Except(brokenSongs).ToList();
            }

            //Combine Unplayed and Oldest Lists, and remove banned songs
            List<SongID> selectedSongs = whitelistedPlayedSongs.
                Union(whitelistedUnplayedSongs)
                .Except(songSuggest.songBanning.GetBannedSongIDs().ToList())
                .ToList();

            return selectedSongs;
        }

        private List<SongID> WhiteListPlayed(bool played)
        {
            //Prepare Song Categories, first finding the related settings, and then preparing the leaderboards active choices.
            SongCategory activeSetting = played ? settings.playedSongCategories : settings.unplayedSongCategories;
            SongCategory scoreSaber = SongCategory.ScoreSaber & activeSetting;
            SongCategory accSaber = (SongCategory.AccSaberStandard | SongCategory.AccSaberTrue | SongCategory.AccSaberTech) & activeSetting;

            //Definitions of different needed comparisons before assigning correct based on played/unplayed/leaderboard.
            Func<SongID, bool> validAccuracy = songID =>
            {
                if (!songSuggest.activePlayer.Contains(songID)) return false;

                double acc = songSuggest.activePlayer.GetAccuracy(songID);
                return settings.ignoreAccuracyEqualBelow <= acc && acc <= settings.ignoreAccuracyEqualAbove;
            };
            //songSuggest.activePlayer.Contains(songID)//songSuggest.activePlayer.scores.ContainsKey(SongLibrary.SongIDToSong(songID).scoreSaberID)
            //? settings.ignoreAccuracyEqualBelow <= songSuggest.activePlayer.GetAccuracy(songID)&&//songSuggest.activePlayer.scores[SongLibrary.SongIDToSong(songID).scoreSaberID].accuracy &&
            //    settings.ignoreAccuracyEqualAbove >= songSuggest.activePlayer.GetAccuracy(songID)//songSuggest.activePlayer.scores[SongLibrary.SongIDToSong(songID).scoreSaberID].accuracy
            //: false;

            Func<SongID, bool> validDays = songID =>
            {
                if (!songSuggest.activePlayer.Contains(songID)) return false;

                double daysSinceLastPlayed = (DateTime.UtcNow - songSuggest.activePlayer.GetTimeSet(songID)).TotalDays;
                return settings.ignorePlayedDaysBelow < daysSinceLastPlayed && daysSinceLastPlayed < settings.ignorePlayedDaysAbove;
            };
            //songSuggest.activePlayer.Contains(songID)//songSuggest.activePlayer.scores.ContainsKey(SongLibrary.SongIDToSong(songID).scoreSaberID)
            //? settings.ignorePlayedDaysBelow < (DateTime.UtcNow - songSuggest.activePlayer.scores[SongLibrary.SongIDToSong(songID).scoreSaberID].timeSet).TotalDays &&
            //    settings.ignorePlayedDaysAbove > (DateTime.UtcNow - songSuggest.activePlayer.scores[SongLibrary.SongIDToSong(songID).scoreSaberID].timeSet).TotalDays
            //: false;

            Func<SongID, bool> scoreSaberStar = songID =>
                settings.ignoreBeatSaberStarBelow < SongLibrary.SongIDToSong(songID).starScoreSaber &&
                settings.ignoreBeatSaberStarAbove > SongLibrary.SongIDToSong(songID).starScoreSaber;

            Func<SongID, bool> accSaberComplexity = songID =>
                settings.ignoreAccSaberComplexityBelow < SongLibrary.SongIDToSong(songID).complexityAccSaber &&
                settings.ignoreAccSaberComplexityAbove > SongLibrary.SongIDToSong(songID).complexityAccSaber;

            Func<SongID, bool> alwaysValid = _ => true;

            //Assigns the lookup function depending on songs are Played or Unplayed for playerdependant scores
            var days = played ? validDays : alwaysValid;
            var accuracy = played ? validAccuracy : alwaysValid;

            //Gets the whitelists from the different leaderboards and combine them.
            List<SongID> scoreSaberWhiteList = WhiteListLeaderboard(scoreSaber, days, accuracy, scoreSaberStar);
            List<SongID> accSaberWhiteList = WhiteListLeaderboard(accSaber, days, accuracy, accSaberComplexity);

            //Combine the legal whitelist and return them.
            return scoreSaberWhiteList.Union(accSaberWhiteList).ToList();
        }

        //Returns a WhiteList of the selected songs based on the filled Lamda's. Should help separate defining and applying filters from the middle Layer.
        private List<SongID> WhiteListLeaderboard(SongCategory category, Func<SongID, bool> days, Func<SongID, bool> accuracy, Func<SongID, bool> leaderBoardRating)
        {
            var songs = songSuggest.songLibrary.GetAllRankedSongIDs(category)
                .Where(days)
                .Where(accuracy)
                .Where(leaderBoardRating)
                .ToList();
            return songs;
        }

        private List<SongID> SortSongs(List<SongID> songs, SongSortCriteria sort)
        {
            switch (sort)
            {
                case SongSortCriteria.Accuracy:
                    songs = Acc(songs);
                    break;
                case SongSortCriteria.Age:
                    songs = Age(songs);
                    break;
                case SongSortCriteria.PP:
                    songs = PP(songs);
                    break;
                case SongSortCriteria.AP:
                    songs = AP(songs);
                    break;
                case SongSortCriteria.Star:
                    songs = Star(songs);
                    break;
                case SongSortCriteria.Complexity:
                    songs = Complexity(songs);
                    break;
                case SongSortCriteria.WorldPercentage:
                    songs = WorldPercentage(songs);
                    break;
                case SongSortCriteria.WorldRank:
                    songs = WorldRank(songs);
                    break;
            }
            return songs;
        }

        //All Sortings are best to worst.

        //Sort the selection by Accuracy
        private List<SongID> Acc(List<SongID> selectedSongs)
        {
            //Lets find set scores and order them
            var setScores = selectedSongs
                .Where(songID => songSuggest.activePlayer.Contains(songID))
                .OrderByDescending(songID => songSuggest.activePlayer.GetAccuracy(songID))
                .ToList();
            //.Where(songID => songSuggest.activePlayer.scores.ContainsKey(SongLibrary.SongIDToSong(songID).scoreSaberID))
            //.Select(songID => songSuggest.activePlayer.scores[SongLibrary.SongIDToSong(songID).scoreSaberID])
            //.OrderByDescending(score => score.accuracy)
            //.Select(score => (SongID)(ScoreSaberID)score.songID)
            //.ToList();

            //Any non found scores (unplayed if added) are found here
            var unplayedSongs = selectedSongs
                .Except(setScores)
                .ToList();

            //Unknown scores are treated as 0 pp, and returned along with set scores
            return setScores.Union(unplayedSongs).ToList();
        }

        //Sort the selection by Age
        private List<SongID> Age(List<SongID> selectedSongs)
        {
            //Lets find set scores and order them
            var setScores = selectedSongs
                .Where(songID => songSuggest.activePlayer.Contains(songID))
                .OrderByDescending(songID => songSuggest.activePlayer.GetTimeSet(songID))
                .ToList();

            //.Where(songID => songSuggest.activePlayer.scores.ContainsKey(SongLibrary.SongIDToSong(songID).scoreSaberID))
            //.Select(songID => songSuggest.activePlayer.scores[SongLibrary.SongIDToSong(songID).scoreSaberID])
            //.OrderByDescending(score => score.timeSet)
            //.Select(score => (SongID)(ScoreSaberID)score.songID)
            //.ToList();

            //Any non found scores (unplayed if added) are found here
            var unplayedSongs = selectedSongs
                .Except(setScores)
                .ToList();

            //Unknown scores are treated as oldest, and returned along with set scores
            return setScores.Union(unplayedSongs).ToList();
        }

        //Sort the selection by PP
        private List<SongID> PP(List<SongID> selectedSongs)
        {
            //Lets find set scores and order them
            var setScores = selectedSongs
                .Where(songID => songSuggest.activePlayer.Contains(songID))
                .OrderByDescending(songID => songSuggest.activePlayer.GetRatedScore(songID,LeaderboardType.ScoreSaber))
                .ToList();
            //.Where(songID => songSuggest.activePlayer.scores.ContainsKey(SongLibrary.SongIDToSong(songID).scoreSaberID))
            //.Select(songID => songSuggest.activePlayer.scores[SongLibrary.SongIDToSong(songID).scoreSaberID])
            //.OrderByDescending(score => score.pp)
            //.Select(score => (SongID)(ScoreSaberID)score.songID)
            //.ToList();



            //Any non found scores (unplayed if added) are found here
            var unplayedSongs = selectedSongs
                .Except(setScores)
                .ToList();

            //Unknown scores are treated as 0 pp, and returned along with set scores
            return setScores.Union(unplayedSongs).ToList();
        }

        //Sort the selection by AP
        private List<SongID> AP(List<SongID> selectedSongs)
        {
            //Lets find set scores and order them
            var setScores = selectedSongs
                .Where(songID => songSuggest.activePlayer.Contains(songID))
                .OrderByDescending(songID => songSuggest.activePlayer.GetRatedScore(songID, LeaderboardType.AccSaber))
                .ToList();
            //.Where(songID => songSuggest.activePlayer.scores.ContainsKey(SongLibrary.SongIDToSong(songID).scoreSaberID))
            //.Select(songID => songSuggest.activePlayer.scores[SongLibrary.SongIDToSong(songID).scoreSaberID])
            //.OrderByDescending(score => AccSaberCurve.AP(score.accuracy / 100, SongLibrary.SongIDToSong((ScoreSaberID)score.songID).complexityAccSaber))
            //.Select(score => (SongID)(ScoreSaberID)score.songID)
            //.ToList();

            //Any non found scores (unplayed if added) are found here
            var unplayedSongs = selectedSongs
                .Except(setScores)
                .ToList();

            //Unknown scores are treated as 0 pp, and returned along with set scores
            return setScores.Union(unplayedSongs).ToList();
        }

        //Sort the selection by Beat Saber Star Rating
        private List<SongID> Star(List<SongID> selectedSongs)
        {
            //Order all selected songs by their star rating.
            var sortedScores = selectedSongs
                .Select(songID => SongLibrary.SongIDToSong(songID))
                .OrderByDescending(song => song.starScoreSaber)
                .Select(song => (SongID)(InternalID)song.internalID)
                .ToList();

            return sortedScores;
        }

        //Sort the selection by AccSaber Complexity
        private List<SongID> Complexity(List<SongID> selectedSongs)
        {
            //Order all selected songs by their star rating.
            var sortedScores = selectedSongs
                .Select(songID => SongLibrary.SongIDToSong(songID))
                .OrderByDescending(song => song.complexityAccSaber)
                .Select(song => (SongID)(InternalID)song.internalID)
                .ToList();

            return sortedScores;
        }

        //Sort the selection by worldPercentage
        private List<SongID> WorldPercentage(List<SongID> selectedSongs)
        {
            //Lets find set scores and order them
            var setScores = selectedSongs
                .Where(songID => songSuggest.activePlayer.Contains(songID))
                .OrderBy(songID => songSuggest.activePlayer.GetWorldRank(songID, ScoreLocation.ScoreSaber))
                .ToList();
            //.Where(songID => songSuggest.activePlayer.scores.ContainsKey(SongLibrary.SongIDToSong(songID).scoreSaberID))
            //.Select(songID => songSuggest.activePlayer.scores[SongLibrary.SongIDToSong(songID).scoreSaberID])
            //.OrderBy(score => score.rankPercentile)
            //.Select(score => (SongID)(ScoreSaberID)score.songID)
            //.ToList();

            //Any non found scores (unplayed if added) are found here
            var unplayedSongs = selectedSongs
                .Except(setScores)
                .ToList();

            //Unknown scores are treated as oldest, and returned along with set scores
            return setScores.Union(unplayedSongs).ToList();
        }

        //Sort the selection by World Rank
        private List<SongID> WorldRank(List<SongID> selectedSongs)
        {
            //Lets find set scores and order them
            var setScores = selectedSongs
                .Where(songID => songSuggest.activePlayer.Contains(songID))
                .OrderBy(songID => songSuggest.activePlayer.GetWorldPercentile(songID, ScoreLocation.ScoreSaber))
                .ToList();
            //.Where(songID => songSuggest.activePlayer.scores.ContainsKey(SongLibrary.SongIDToSong(songID).scoreSaberID))
            //.Select(songID => songSuggest.activePlayer.scores[SongLibrary.SongIDToSong(songID).scoreSaberID])
            //.OrderBy(score => score.rankScoreSaber)
            //.Select(score => (SongID)(ScoreSaberID)score.songID)
            //.ToList();

            //Any non found scores (unplayed if added) are found here
            var unplayedSongs = selectedSongs
                .Except(setScores)
                .ToList();

            //Unknown scores are treated as oldest, and returned along with set scores
            return setScores.Union(unplayedSongs).ToList();
        }

        //Sort the selection Random
        private List<SongID> RandomOrder(List<SongID> selectedSongs)
        {
            Random random = new Random();

            // Start from the end and swap elements randomly
            for (int i = selectedSongs.Count - 1; i > 0; i--)
            {
                int randomIndex = random.Next(0, i + 1);
                var temp = selectedSongs[i];
                selectedSongs[i] = selectedSongs[randomIndex];
                selectedSongs[randomIndex] = temp;
            }

            return selectedSongs;
        }
    }

    public class WeightedSelection
    {
        private Random rnd = new Random();

        private WeightedSelection leftWS;
        private WeightedSelection rightWS;
        private WeightedPair myPair;
        public double Weight { get; private set; }
        public int Count { get; private set; }
        private int leftItems;
        private int rightItems;

        public WeightedSelection(List<WeightedPair> weightedPairs)
        {
            if (weightedPairs.Count == 0) return;
            //Get first item and store in this item.
            myPair = weightedPairs[0];
            Count = 1;
            Weight = myPair.Weight;

            //Splits incoming in 3 parts, first has been stored here so it is skipped
            //Indexes for remaining are found split in two, extra goes left
            //Increase total by found total for the sides.

            int leftStart = 1;
            leftItems = (weightedPairs.Count) / 2;
            int rightStart = leftItems + 1;
            rightItems = (weightedPairs.Count - 1 - leftItems);

            if (leftItems > 0)
            {
                leftWS = new WeightedSelection(weightedPairs.GetRange(leftStart, leftItems));
                Count += leftWS.Count;
                Weight += leftWS.Weight;
            }
            if (rightItems > 0)
            {
                rightWS = new WeightedSelection(weightedPairs.GetRange(rightStart, rightItems));
                Count += rightWS.Count;
                Weight += rightWS.Weight;
            }
        }

        public WeightedPair GetRandom()
        {
            return GetAt(rnd.NextDouble() * Weight);
        }

        public WeightedPair GetAt(double selection)
        {
            WeightedPair returnPair = null;

            //No items left
            if (Count == 0) return returnPair;

            Count--;

            //Check if own Item is selected
            if (selection < myPair.Weight)
            {
                returnPair = myPair;

                //Last Item
                if (Count == 0)
                {
                    Weight = 0;
                    myPair = null;
                    return returnPair;
                }

                //Grab first item from largest side.
                if (leftItems >= rightItems)
                {
                    myPair = leftWS.GetAt(0);
                    leftItems--;
                }
                else
                {
                    myPair = rightWS.GetAt(0);
                    rightItems--;
                }

                Weight = Weight - returnPair.Weight;
                return returnPair;
            }

            //Check if selection is in left side
            selection = selection - myPair.Weight;

            if (selection < leftWS.Weight)
            {
                returnPair = leftWS.GetAt(selection);
                leftItems--;
                Weight = Weight - returnPair.Weight;

                //If left is empty, move right to left and clear left
                if (leftItems == 0)
                {
                    leftWS = rightWS;
                    leftItems = rightItems;
                    rightItems = 0;
                    rightWS = null;
                }

                return returnPair;
            }

            //Selection is in right side
            selection = selection - leftWS.Weight;

            returnPair = rightWS.GetAt(selection);
            rightItems--;
            Weight = Weight - returnPair.Weight;

            return returnPair;
        }

        public List<WeightedPair> Take(int count)
        {
            if (count > Count) count = Count;
            List<WeightedPair> returnList = new List<WeightedPair>();

            while (count > 0)
            {
                returnList.Add(GetRandom());
                count--;
            }
            return returnList;
        }

        public List<WeightedPair> GetAllRandom()
        {
            return Take(Count);
        }
    }

    public class WeightedPair
    {
        public Object Value { get; set; }
        public Double Weight { get; set; }
    }
}