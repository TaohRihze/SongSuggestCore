using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Actions;
using Newtonsoft.Json;
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
            scores[ScoreLocation.ScoreSaber] = new ScoreSaberPlayerScoreManager() {ActivePlayer = this};
            scores[ScoreLocation.BeatLeader] = new BeatLeaderPlayerScoreManager() {ActivePlayer = this};
            scores[ScoreLocation.LocalScores] = new LocalPlayerScoreManager() {ActivePlayer = this};
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
            songSuggest.log?.WriteLine($"Starting Fresh of {ActiveScoreLocations.Count}");
            foreach (var location in ActiveScoreLocations)
            {
                songSuggest.log?.WriteLine($"Refreshing: {location}");
                scores[location].Refresh();
                songSuggest.log?.WriteLine($"Done refreshing: {location}");
            }
        }

        //All Song Categories (except the BrokenDownloads)
        private static SongCategory allCategories = (SongCategory)Enum.GetValues(typeof(SongCategory))
            .Cast<int>()
            .Except(new [] { (int)SongCategory.BrokenDownloads })
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
            if (score == null) return int.MaxValue;
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

    //Source Manager connections.
    public interface IPlayerScores
    {
        ActivePlayer ActivePlayer { get; set; }

        //Loads the current score cache for the player on the related Scoreboard
        void Load();
        //Save the current score cache for the player on the related ScoreBoard
        void Save();
        //Clear all saved scores
        void Clear();
        //Verify if the loaded version is outdated compared to source data
        void ClearIfOutdated();
        //Refresh Data
        void Refresh();
        //Returns true if there is set a Score on the songID
        bool Contains(SongID songID);

        //Returns either DateTime of the score with the given ID, or min DateTime value
        DateTime GetTimeSet(SongID songID);

        //Returns the songs Accuracy in a 0-1 range (90% = 0.9)
        double GetAccuracy(SongID songID);

        //Returns the songs score on the Leaderboard (0 if unknown).
        double GetRatedScore(SongID songID, LeaderboardType leaderboardType);

        //Returns the Score Object of the given SongID
        PlayerScore GetScore(SongID songID);

        //Returns the SongIDs of songs in the given Leaderboard Categories
        List<SongID> GetRankedScoreIDs(SongCategory songCategory);
        //Returns the SongIDs of songs in the given Leaderboard Categories
        List<SongID> GetScoreIDs();
        void ShowCache(TextWriter log);
    }
}
