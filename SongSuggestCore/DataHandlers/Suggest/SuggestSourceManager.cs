using System;
using System.Collections.Generic;
using System.Linq;
using LinkedData;
using SongSuggestNS;
using Curve;
using SongLibraryNS;
using System.Reflection;

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

        internal double PlayerWeightedScoreValue(SongID value)
        {
            return PlayerScoreValue(value) * PlayerScoreRankModifier(value);
        }

        internal double PlayerScoreRankModifier(SongID value)
        {
            int rank = PlayerScoreRank(value);
            if (rank == -1) return 0.0;

            switch (leaderboardType)
            {
                case LeaderboardType.ScoreSaber:
                case LeaderboardType.BeatLeader:
                case LeaderboardType.AutoBalancer:
                    return Math.Pow(0.965, rank-1); //rank starts at 1, here we need to start at 0
                case LeaderboardType.AccSaber:
                    return AccSaberCurve.RankMultiplier(rank);
            }

            //Unknown handling detected
            throw new InvalidOperationException($"Unknown PlayerScoreRankModifier Source found: {leaderboardType}");
        }

        //Returns the specific Rank of a sub leaderboard (e.g. acc sabers 3 leaderboards separate ranks (An Acc Saber song only has 1 actual category))
        internal int PlayerScoreRank(SongID value)
        {
            SongCategory category = LeaderboardSongCategory() & value.GetSong().songCategory;
            return songSuggest.activePlayer.GetLeaderboardRank(value, leaderboardType, category);
        }

        //Returns the acc value of a song, if song .. if song is unknown 0 is returned.
        //Acc is 0 to 1.
        public double PlayerAccuracyValue(SongID songID)
        {
            return songSuggest.activePlayer.GetAccuracy(songID);
        }

        //Compares the players score on the song to the highest known score on the song, returns 0 if song is unknown
        public double PlayerRelativeScoreValue(SongID songID)
        {
            double playerScore = songSuggest.activePlayer.GetRatedScore(songID, leaderboardType);
            string songString = GetStringID(songID);
            //Try to lookup the value of the songs max if known, else 0 is used
            //(should filter out the song as a candidate, which is good, we want candidates that can link to plays).
            double leaderboardMax = Leaderboard().top10kSongMeta.TryGetValue(songString, out var songMeta)
                ? songMeta.maxScore
                : 0;
            double relativeScore = leaderboardMax != 0 ? playerScore / leaderboardMax : 0;
            return relativeScore;
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

        internal string GetStringID (SongID songID)
        {
            switch (leaderboardType)
            {
                case LeaderboardType.ScoreSaber:
                    return songID.GetSong().scoreSaberID;
                case LeaderboardType.AccSaber:
                    return songID.GetSong().scoreSaberID;
                case LeaderboardType.BeatLeader:
                    return songID.GetSong().beatLeaderID;
                case LeaderboardType.AutoBalancer:
                    return songID.GetSong().internalID;
            }
            throw new InvalidOperationException($"Unknown ScoreBoardTopPlays Source found: {leaderboardType}");
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
                case LeaderboardType.AutoBalancer:
                    return songSuggest.autoBalancerScoreBoard;
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
                case LeaderboardType.AutoBalancer:
                    return SongCategory.AutoBalancer;
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
                case LeaderboardType.AutoBalancer:
                    return SongIDType.Internal;
            }
            throw new InvalidOperationException($"Unknown LeaderboardSongIDType Source found: {leaderboardType}");
        }


    }
}