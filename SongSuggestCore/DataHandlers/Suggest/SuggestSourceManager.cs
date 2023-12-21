using System;
using System.Collections.Generic;
using System.Linq;
using LinkedData;
using SongSuggestNS;
using Curve;
using SongLibraryNS;

namespace Actions
{
    //--- Handling of data sources ---
    public class SuggestSourceManager
    {
        public SongSuggest songSuggest { get; set; }
        public ScoreLocation scoreLocation { get; set; } = ScoreLocation.ScoreSaber;
        public LeaderboardType leaderboardType { get; set; } = LeaderboardType.ScoreSaber;

        //Return all the players SongIDs
        public List<SongID> PlayerScoresIDs()
        {
            switch (scoreLocation)
            {
                case ScoreLocation.ScoreSaber:
                    var songID = songSuggest.activePlayer.scores.Values
                        .Select(c => SongLibrary.StringIDToSongID(c.songID,SongIDType.ScoreSaber))
                        .Where(c => SongLibrary.HasAnySongCategory(c,LeaderboardSongCategory()))
                        .ToList();
                    return songID;
                case ScoreLocation.LocalScores:
                    return songSuggest.localScores.GetScores(LeaderboardSongCategory());
                case ScoreLocation.BeatLeader:
                    return songSuggest.beatLeaderScores.GetScores(LeaderboardSongCategory());
            }

            //Unknown handling detected
            throw new InvalidOperationException($"Unknown PlayerScoreIDs Source found: {scoreLocation}");
        }

        //Returns the value of a songID .. if song is unknown 0 is returned.
        public double PlayerScoreValue(SongID songID)
        {
            Song song = SongLibrary.SongIDToSong(songID);

            //Return recorded scores if leaderboard is cached, else calculate
            if (!(scoreLocation == ScoreLocation.LocalScores))
            {
                //Return results from the cached leaderboard data if it supports it.
                //e.g. Acc Saber can only be calculated.
                switch (leaderboardType)
                {
                    case LeaderboardType.ScoreSaber:
                        if (!songSuggest.activePlayer.scores.ContainsKey(song.scoreSaberID)) return 0;
                        return songSuggest.activePlayer.scores[song.scoreSaberID].pp;
                    case LeaderboardType.BeatLeader:
                        var playerScore = songSuggest.beatLeaderScores.playerScores.Find(c => c.SongID == song.beatLeaderID);
                        return (playerScore != null) ? playerScore.PP:0;
                }
            }

            double accuracy = PlayerAccuracyValue(songID);
            double score = CalculatedScore(songID, accuracy);
            return score;
        }

        //Returns the acc value of a song, if song .. if song is unknown 0 is returned.
        //Acc is 0 to 1.
        public double PlayerAccuracyValue(SongID songID)
        {
            Song song = SongLibrary.SongIDToSong(songID);

            switch (scoreLocation)
            {
                case ScoreLocation.ScoreSaber:
                    if (!songSuggest.activePlayer.scores.ContainsKey(song.scoreSaberID)) return 0;
                    return songSuggest.activePlayer.scores[song.scoreSaberID].accuracy / 100;
                case ScoreLocation.LocalScores:
                    return songSuggest.localScores.GetAccuracy(song.scoreSaberID); //**Needs updating when local scores moves to internalID, but for now its scoreSaberIDs
                case ScoreLocation.BeatLeader:
                    var playerScore = songSuggest.beatLeaderScores.playerScores.Find(c => c.SongID == song.beatLeaderID);
                    return (playerScore != null) ? playerScore.Accuracy : 0;
            }

            //Unknown handling detected
            throw new InvalidOperationException($"Unknown PlayerAccuracyValue Source found: {scoreLocation}");
        }

        //Returns the Time of when a score was set.
        internal DateTime PlayerScoreDate(SongID songID)
        {
            Song song = SongLibrary.SongIDToSong(songID);

            switch (scoreLocation)
            {
                case ScoreLocation.ScoreSaber:
                    if (!songSuggest.activePlayer.scores.ContainsKey(song.scoreSaberID)) return DateTime.MinValue;
                    return songSuggest.activePlayer.scores[song.scoreSaberID].timeSet;
                case ScoreLocation.LocalScores:
                    return songSuggest.localScores.GetTimeSet(song.scoreSaberID); //**Update to send SongID
                case ScoreLocation.BeatLeader:
                    var playerScore = songSuggest.beatLeaderScores.playerScores.Find(c => c.SongID == song.beatLeaderID);
                    return (playerScore != null) ? playerScore.TimeSet : DateTime.MinValue;

            }

            //Unknown handling detected
            throw new InvalidOperationException($"Unknown PlayerScoreDate Source found: {scoreLocation}");
        }

        public List<SongID> LikedSongs()
        {
            var allLikedSongs = songSuggest.songLiking.GetLikedIDs();
            var allSourceSongs = songSuggest.songLibrary.GetAllRankedSongIDs(LeaderboardSongCategory()).Select(c => c.Value).ToList();

            return SongLibrary.StringIDToSongID(allLikedSongs.Intersect(allSourceSongs).ToList(),SongIDType.ScoreSaber); //**Needs rewrite to store SongLibrary in Internal ID format and return those direct.
        }

        //Returns the calculated value of a songID .. unknown songs got 0 accuracy so 0 is returned.
        public double CalculatedScore(SongID songID, double accuracy)
        {
            Song song = SongLibrary.SongIDToSong(songID);

            switch (leaderboardType)
            {
                case LeaderboardType.ScoreSaber:
                    double starRating = song.starScoreSaber;
                    return ScoreSaberCurve.PP(accuracy, starRating);
                case LeaderboardType.AccSaber:
                    double complexityRating = song.complexityAccSaber;
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