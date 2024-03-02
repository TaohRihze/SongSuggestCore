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
        //public ScoreLocation scoreLocation { get; set; } = ScoreLocation.ScoreSaber;
        public LeaderboardType leaderboardType { get; set; } = LeaderboardType.ScoreSaber;

        //Return all the players SongIDs regardless of leaderboard. A recorded score is a recorded score.
        public List<SongID> PlayerScoresIDs()
        {
            return songSuggest.activePlayer.GetRankedScoreIDs()
                .Where(c => SongLibrary.HasAnySongCategory(c, LeaderboardSongCategory()))
                .ToList();
        }

        public double PlayerScoreValue(string stringID)
        {
            return PlayerScoreValue(SongLibrary.StringIDToSongID(stringID,LeaderboardSongIDType()));
        }

        //Returns the value of a songID .. if song is unknown 0 is returned.
        public double PlayerScoreValue(SongID songID)
        {
            return songSuggest.activePlayer.GetRatedScore(songID, leaderboardType);
        }

        //Returns the acc value of a song, if song .. if song is unknown 0 is returned.
        //Acc is 0 to 1.
        public double PlayerAccuracyValue(SongID songID)
        {
            return songSuggest.activePlayer.GetAccuracy(songID);
        }

        //Returns the Time of when a score was set.
        internal DateTime PlayerScoreDate(SongID songID)
        {
            return songSuggest.activePlayer.GetTimeSet(songID);
        }

        public List<SongID> LikedSongs()
        {
            var allLikedSongIDs = songSuggest.songLiking.GetLikedIDs();
            //var allLikedSongIDs = SongLibrary.StringIDToSongID(allLikedSongs, SongIDType.ScoreSaber);
            //reduce the liked songs to the ones relevant to active leaderboard.
            allLikedSongIDs = allLikedSongIDs.Where(c => SongLibrary.HasAnySongCategory(c, LeaderboardSongCategory())).ToList();

            //var allSourceSongs = songSuggest.songLibrary.GetAllRankedSongIDs(LeaderboardSongCategory()).Select(c => c.Value).ToList();

            return allLikedSongIDs;
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

        internal int GetRank(SongID songID)
        {
            return songSuggest.activePlayer.GetLeaderboardRank(songID, leaderboardType);
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

        //Returns the enum for the Leaderboards SongIDType.
        public SongIDType LeaderboardSongIDType()
        {
            switch (leaderboardType)
            {
                case LeaderboardType.ScoreSaber:
                    return SongIDType.ScoreSaber;
                case LeaderboardType.AccSaber:
                    return SongIDType.ScoreSaber;
                case LeaderboardType.BeatLeader:
                    return SongIDType.BeatLeader;
            }
            throw new InvalidOperationException($"Unknown LeaderboardSongIDType Source found: {leaderboardType}");
        }

    }
}