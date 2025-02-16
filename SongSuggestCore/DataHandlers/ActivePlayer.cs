﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Actions;
using PlayerScores;
using SongLibraryNS;
using SongSuggestNS;

namespace ActivePlayerData
{
    public class ActivePlayer
    {
        public List<ScoreLocation> ActiveScoreLocations { get; set; } = new List<ScoreLocation>();
        internal SongSuggest songSuggest;
        internal string PlayerID { get; } = "-1";
        private Dictionary<ScoreLocation, IPlayerScores> scores = new Dictionary<ScoreLocation, IPlayerScores>();

        //songSuggest is used for links to which dataset to use, does not mean this is the ActivePlayer stored in that songSuggest. (e.g. 2nd players data in snipeSuggest)
        public ActivePlayer(string playerID, SongSuggest songSuggest)
        {
            PlayerID = playerID;
            this.songSuggest = songSuggest;
            scores[ScoreLocation.ScoreSaber] = new ScoreSaberPlayerScoreManager() { ActivePlayer = this };
            scores[ScoreLocation.BeatLeader] = new BeatLeaderPlayerScoreManager() { ActivePlayer = this };
            scores[ScoreLocation.LocalScores] = new LocalPlayerScoreManager() { ActivePlayer = this };
            scores[ScoreLocation.SessionScores] = new SessionScoreManager() { ActivePlayer = this };
        }

        //Loads all the cached data on the active player, and clears any that is outdated.
        public void Load()
        {
            if (PlayerID == "-1") return;

            //Load the data on the PlayerID related to this object.
            foreach (IPlayerScores playerScores in scores.Values)
            {
                playerScores.Load();
                playerScores.ClearIfOutdated();
            }

            CachedRankings.Clear();
        }

        //Saves all cached data.
        public void Save()
        {
            foreach (IPlayerScores playerScores in scores.Values)
            {
                playerScores.Save();
            }
        }

        //Refresh the active ScoreLocations data
        public void Refresh()
        {
            songSuggest.log?.WriteLine($"Starting Refresh of {ActiveScoreLocations.Count}");
            foreach (var location in ActiveScoreLocations)
            {
                songSuggest.log?.WriteLine($"Refreshing: {location}");
                scores[location].Refresh();
                songSuggest.log?.WriteLine($"Done refreshing: {location}");
            }

            CachedRankings.Clear();
        }

        //All Song Categories (except the BrokenDownloads)
        private static SongCategory allCategories = (SongCategory)Enum.GetValues(typeof(SongCategory))
            .Cast<int>()
            .Except(new[] { (int)SongCategory.BrokenDownloads })
            .Sum();

        //Return all ranked songs within the active leaderboards
        public List<SongID> GetRankedScoreIDs()
        {
            if (ActiveScoreLocations.Count == 0) return new List<SongID>();
            return ActiveScoreLocations
                .SelectMany(location => scores[location].GetRankedScoreIDs(allCategories))
                .Distinct()
                .ToList();
        }

        //Return all ranked songs within a given leaderboard
        public List<SongID> GetRankedLocationScoreIDs(ScoreLocation scoreLocation)
        {
            return scores[scoreLocation].GetRankedScoreIDs(allCategories);
        }

        //Return all songs played on any ScoreLocation
        public List<SongID> GetScoreIDs()
        {
            if (ActiveScoreLocations.Count == 0) return new List<SongID>();
            return ActiveScoreLocations
                .SelectMany(location => scores[location].GetScoreIDs())
                .Distinct()
                .ToList();
        }

        //Checks if the SongID is present in any active ScoreLocations
        public bool Contains(SongID songID)
        {
            if (ActiveScoreLocations.Count == 0) return false;
            return ActiveScoreLocations.Any(location => scores[location].Contains(songID));
        }

        //Checks if the SongID is present in specified ScoreLocations
        public bool Contains(SongID songID, ScoreLocation location)
        {
            return scores[location].Contains(songID);
        }

        //Return highest Accuracy of any location
        public double GetAccuracy(SongID songID)
        {
            if (ActiveScoreLocations.Count == 0) return 0;
            return ActiveScoreLocations.Max(location => scores[location].GetAccuracy(songID));
        }

        //Return highest Set Score (pp) of any location
        public double GetRatedScore(SongID songID, LeaderboardType leaderboardType)
        {
            if (ActiveScoreLocations.Count == 0) return 0;
            return ActiveScoreLocations.Max(location => scores[location].GetRatedScore(songID, leaderboardType));
        }

        //Return timeset on the score with the highest accuracy
        public DateTime GetTimeSet(SongID songID)
        {
            if (ActiveScoreLocations.Count == 0) return DateTime.MinValue;
            var highestAccLocation = ActiveScoreLocations
                .OrderByDescending(location => scores[location].GetAccuracy(songID))
                .FirstOrDefault();

            return scores[highestAccLocation].GetTimeSet(songID);
        }

        //Returns the world rank for a score on a specified leaderboard.
        public int GetWorldRank(SongID songID, ScoreLocation scoreLocation)
        {
            var score = scores[scoreLocation].GetScore(songID);
            if (score == null) return int.MaxValue; //No Score ... we either return a logic "worst" value or -1 signaling not found. Worst Value if not handled likely is more correct.
            return score.SourceRank;
        }

        //Returns the world percentile rank for a score on a specified leaderboard.
        public double GetWorldPercentile(SongID songID, ScoreLocation scoreLocation)
        {
            var score = scores[scoreLocation].GetScore(songID);
            if (score == null) return 1; //No score, 100% of players beat you.
            return score.SourceRankPercentile;
        }

        //Returns the world plays for a score on a specified leaderboard.
        public int GetWorldPlays(SongID songID, ScoreLocation scoreLocation)
        {
            var score = scores[scoreLocation].GetScore(songID);
            if (score == null) return -1; //Player has no scores, so we did not cache amount of plays. Let UI figure this out.
            return score.SourcePlays;
        }

        //Returns the ranking of all scores from a player for a given leaderboard.
        public int GetLeaderboardRank(SongID songID, LeaderboardType leaderboard)
        {
            switch (leaderboard)
            {
                case LeaderboardType.ScoreSaber:
                    return GetLeaderboardRank(songID, leaderboard, SongCategory.ScoreSaber);
                case LeaderboardType.AccSaber:
                    return GetLeaderboardRank(songID, leaderboard, SongCategory.AccSaberStandard | SongCategory.AccSaberTrue | SongCategory.AccSaberTech);
                case LeaderboardType.BeatLeader:
                    return GetLeaderboardRank(songID, leaderboard, SongCategory.BeatLeader);
            }
            //No known handling so rank is set to unknown (-1).
            return -1;
        }

        public Dictionary<SongCategory, Dictionary<SongID, int>> CachedRankings = new Dictionary<SongCategory, Dictionary<SongID, int>>();
        //Allows you to also specify only specific sub categories (Acc Saber and possible HitBloq if added)
        //Each leaderboard uses only its attached source songs and session cache. (e.g. no BeatLeader scores for Acc Saber).
        public int GetLeaderboardRank(SongID songID, LeaderboardType leaderboard, SongCategory categories)
        {
            //Check for cache, and if none create a new.
            if (!CachedRankings.ContainsKey(categories))
            {
                //Get known songs from relevant locations
                List<SongID> scoreIDs = new List<SongID>();
                switch (leaderboard)
                {
                    case LeaderboardType.ScoreSaber:
                    case LeaderboardType.AccSaber:
                        scoreIDs = GetRankedLocationScoreIDs(ScoreLocation.ScoreSaber);
                        break;
                    case LeaderboardType.BeatLeader:
                        scoreIDs = GetRankedLocationScoreIDs(ScoreLocation.BeatLeader);
                        break;
                }
                //reduce song IDs to matching categories, and order by value
                scoreIDs = scoreIDs
                    .Where(c => SongLibrary.HasAnySongCategory(c, categories))  //Must be of the given categories
                    .OrderByDescending(c => GetRatedScore(c, leaderboard))      //Order by rank
                .ToList();

                Dictionary<SongID, int> categoryDictionary = scoreIDs
                    .Select((c, index) => new { Key = c, Value = index + 1 })
                    .ToDictionary(item => item.Key, item => item.Value);

                CachedRankings.Add(categories, categoryDictionary);
            }

            //Return cache lookup, if ID is not within the cache a rank of -1 is given, let UI decide handling of this.
            return CachedRankings[categories].TryGetValue(songID, out var rank) ? rank : -1;
        }

        //Returns the related IPlayer of the Scorelocation.
        internal IPlayerScores GetScoreLocation(ScoreLocation scoreLocation)
        {
            return scores[scoreLocation];
        }

        //sends values of different cached data to log
        public void ShowCache()
        {
            foreach (var score in scores.Values)
            {
                score.ShowCache(songSuggest.log);
            }
        }

        //Clears external data (Local Scores does not clear).
        internal void ClearCache()
        {
            foreach (var score in scores.Values)
            {
                score.Clear();
            }
        }
    }
}