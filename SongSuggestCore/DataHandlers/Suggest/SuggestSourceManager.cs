using System;
using System.Collections.Generic;
using System.Linq;
using LinkedData;
using SongSuggestNS;
using Curve;
using ScoreSabersJson;

namespace Actions
{
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
                //Temporary Hardcoded to the 20 selected scores of the BeatLeader scores
                case ScoreLocation.BeatLeader:
                    return songSuggest.beatLeaderScores.GetScores(LeaderboardSongCategory());
                    
                    //return Leaderboard()
                    //    .top10kPlayers
                    //    .Where(c => songSuggest.activePlayerID == c.id)
                    //    .SelectMany(c => c.top10kScore)
                    //    .Select(c => c.songID)
                    //    .ToList();
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
                    //Temporary Hardcoded to the 20 selected scores of the BeatLeader scores
                    case LeaderboardType.BeatLeader:
                        var playerScore = songSuggest.beatLeaderScores.playerScores.Find(c => c.SongID == songID);
                        return (playerScore != null) ? playerScore.PP:0;

                        //return (double) Leaderboard()
                        // .top10kPlayers
                        //.Where(c => songSuggest.activePlayerID == c.id)
                        //.SelectMany(c => c.top10kScore)
                        //.Where(c => c.songID == songID)
                        //.First()
                        //.pp;
                }
            }

            double accuracy = PlayerAccuracyValue(songID);
            double score = CalculatedScore(songID, accuracy);
            return score;
        }

        //Returns the acc value of a song, if song .. if song is unknown 0 is returned.
        //Acc is 0 to 1.
        public double PlayerAccuracyValue(string songID)
        {
            switch (scoreLocation)
            {
                case ScoreLocation.ScoreSaber:
                    if (!songSuggest.activePlayer.scores.ContainsKey(songID)) return 0;
                    return songSuggest.activePlayer.scores[songID].accuracy / 100;
                case ScoreLocation.LocalScores:
                    return songSuggest.localScores.GetAccuracy(songID);
                //Temporary Workaround
                case ScoreLocation.BeatLeader:
                    var playerScore = songSuggest.beatLeaderScores.playerScores.Find(c => c.SongID == songID);
                    return (playerScore != null) ? playerScore.Accuracy : 0;
                    //return 0.5;
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
                case ScoreLocation.BeatLeader:
                    var playerScore = songSuggest.beatLeaderScores.playerScores.Find(c => c.SongID == songID);
                    return (playerScore != null) ? playerScore.TimeSet : DateTime.MinValue;

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
                case LeaderboardType.BeatLeader:
                    return songSuggest.beatLeaderScoreBoard;
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
                case LeaderboardType.BeatLeader:
                    return SongCategory.BeatLeader;
            }
            throw new InvalidOperationException($"Unknown LeaderboardSongCategory Source found: {leaderboardType}");
        }
    }
}