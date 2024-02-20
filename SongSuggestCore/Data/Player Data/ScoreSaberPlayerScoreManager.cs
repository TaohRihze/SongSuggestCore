//using Actions;
//using PlayerScores;
//using ScoreSabersJson;
//using ScoreCollection = PlayerScores.ScoreCollection;
//using PlayerScore = PlayerScores.ScoreCollection;
//using ScoreCollectionJson = ScoreSabersJson.ScoreCollection;
using SongSuggestNS;
using System;
using System.Collections.Generic;
using System.Linq;
using SongLibraryNS;
using ActivePlayerData;
using Actions;
using Curve;
using System.IO;

namespace PlayerScores
{
    internal class ScoreSaberPlayerScoreManager : IPlayerScores
    {
        public ActivePlayer ActivePlayer { get; set; }

        public bool Updated { get; set; }
        public SongSuggest songSuggest => ActivePlayer.songSuggest;
        private ScoreCollection scoreCollection = new ScoreCollection();
        private List<PlayerScore> playerScores => scoreCollection.PlayerScores;

        public void Load()
        {
            scoreCollection = songSuggest.fileHandler.LoadScoreCollection($"SS{ActivePlayer.PlayerID}");
        }

        public void Save()
        {
            songSuggest.log?.WriteLine($"Saving Score Saber Scores");
            songSuggest.fileHandler.SaveScoreCollection(scoreCollection, $"SS{ActivePlayer.PlayerID}");
        }

        public void Refresh()
        {
            //Reset cached scores if there has been an update to ranked songs
            ClearIfOutdated();

            ////If there are no stored scores, we set timestamp to 0, meaning we will get all, else we grab newest recorded score and grab scores after.
            //DateTime lastScoreTime = (playerScores.Count() > 0) ? playerScores.OrderByDescending(c => c.TimeSet).First().TimeSet : DateTime.MinValue;
            int scoresPerPage = 100;
            int loadedPages = 0;
            int records = 1; //Needs to be larger than 0, to ensure we make first loop
            bool noDuplicateFound = true;

            //Loop all players records until we hit a possible known score (timestamp matches and we falsify the noDuplicateFound).
            while (loadedPages * scoresPerPage < records && noDuplicateFound)
            {
                //Pages are 1 indexed, so we need to add 1 to get next unprocessed page
                var scores = songSuggest.webDownloader.GetScoreSaberPlayerScores(ActivePlayer.PlayerID, "recent", scoresPerPage, loadedPages+1);

                //If something in the request fails (web access etc), we get an empty scores object, we received previous updates reverse chronological (newest first), so any received
                //scores on previous pages (loaded pages > 0) may leave a gap of scores (any new scores on this page), and we have to reset the refresh time before returning.
                if (scores.metadata == null)
                {
                    //No previous pages loaded, so no gap, we just return immidiatly
                    if (loadedPages == 0) return;
                    //As data may be unsynced, we restore last set of refreshed data
                    Load();
                    return;
                }

                //Process each found record.
                foreach (var record in scores.playerScores)
                {
                    Song song = ((ScoreSaberID)$"{record.leaderboard.id}").GetSong();

                    //If song is unknown by SongLibrary it is a non ranked score and we skip it.
                    //**Optional Upsert the song in the future via DataItem record**
                    if (song == null) continue;

                    //We store scores via Internal ID type
                    var playerScore = playerScores.Find(c => c.SongID == song.internalID);
                   
                    //If a score is not recorded yet, make a new score and add it
                    if (playerScore == null)
                    {
                        playerScore = new PlayerScore
                        {
                            SongName = song.name,
                            SongID = song.internalID
                        };
                        playerScores.Add(playerScore);
                    }
                    
                    //Only update if it is a new score, else exit the loop
                    if (playerScore.TimeSet == record.score.timeSet)
                    {
                        noDuplicateFound = false;
                        break;
                    }

                    //Update records data
                    playerScore.TimeSet = record.score.timeSet;
                    playerScore.RatedScore = record.score.pp;
                    playerScore.Accuracy = (double)record.score.baseScore / record.leaderboard.maxScore;
                    playerScore.SourcePlays = record.leaderboard.plays;
                    playerScore.SourceRank = record.score.rank;
                }

                //Update status
                loadedPages = scores.metadata.page;
                records = scores.metadata.total;
                songSuggest.log?.WriteLine($"Completed Pages: {loadedPages}/{(records - 1) / scoresPerPage + 1}");
            }

            //Update completed without errors, so we save the updated local cache.
            Save();
        }

        public void Clear()
        {
            scoreCollection = new ScoreCollection();
            Save();
        }


        public void ClearIfOutdated()
        {
            if (!scoreCollection.Validate(songSuggest.filesMeta.top10kVersion))
            {
                Clear();
                scoreCollection.ScoresMeta.DataVersion = songSuggest.filesMeta.top10kVersion;
            }
        }

        public List<SongID> GetRankedScoreIDs(SongCategory songCategory)
        {
            var songIDs = playerScores
                .Select(c => (SongID)(InternalID)c.SongID)
                .Where(c => SongLibrary.HasAnySongCategory(c, songCategory)) //Select scores from Leaderboard with at least 1 match
                .ToList();

            return songIDs;
        }

        public List<SongID> GetScoreIDs()
        {
            var songIDs = playerScores
                .Select(c => (SongID)(InternalID)c.SongID)
                .ToList();

            return songIDs;
        }

        public DateTime GetTimeSet(SongID songID)
        {
            var score = playerScores.Where(c => c.SongID == songID.GetSong().internalID)
                .OrderByDescending(c => c.TimeSet)
                .Select(c => c.TimeSet);

            if (score.Count() == 0) return DateTime.MinValue;
            return score.First();
        }

        public double GetAccuracy(SongID songID)
        {
            var score = playerScores.Where(c => c.SongID == songID.GetSong().internalID)
                .OrderByDescending(c => c.Accuracy)
                .Select(c => c.Accuracy);

            if (score.Count() == 0) return 0;
            return score.First();
        }

        public double GetRatedScore(SongID songID, LeaderboardType leaderboardType)
        {
            var song = songID.GetSong();

            PlayerScore score = playerScores.Where(c => c.SongID == song.internalID)
                .OrderByDescending(c => c.Accuracy)
                .FirstOrDefault();

            if (score == null) return 0;

            switch (leaderboardType)
            {
                case LeaderboardType.ScoreSaber:
                    return score.RatedScore;
                case LeaderboardType.AccSaber:
                    return AccSaberCurve.AP(score.Accuracy, song.complexityAccSaber);
                case LeaderboardType.BeatLeader:
                    return 0; //no Curve Calculation yet
                default:
                    return 0;
            }
        }

        public PlayerScore GetScore(SongID songID)
        {
            var song = songID.GetSong();

            PlayerScore score = playerScores.Where(c => c.SongID == song.internalID)
                .OrderByDescending(c => c.Accuracy)
                .FirstOrDefault();

            return score;
        }

        public bool Contains(SongID songID)
        {
            string internalID = songID.GetSong().internalID;
            return playerScores.Select(c => c.SongID).Contains(internalID);
        }

        public void ShowCache(TextWriter log)
        {

            log?.WriteLine($"ScoreSaber Score Count: {playerScores.Count()}");
        }

        //Dictionary<SongID, int> _cachedRank = new Dictionary<SongID, int>();

        //public int GetRank(SongID songID)
        //{
        //    //Create new cache if not already created
        //    if (_cachedRank.Count() == 0)
        //    {
        //        var songIDs = GetScoreIDs().OrderByDescending(c => GetAccuracy(c)).ToList();
        //        int rank = 1;
        //        foreach (var cachedSongID in songIDs)
        //        {
        //            _cachedRank.Add(cachedSongID, rank);
        //            rank++;
        //        }
        //    }

        //    return _cachedRank.TryGetValue(songID, out var value) ? value : -1;
        //}
    }
}