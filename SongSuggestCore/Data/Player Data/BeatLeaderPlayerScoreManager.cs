using BeatLeaderJson;
using SongSuggestNS;
using System;
using System.Collections.Generic;
using System.Linq;
using SongLibraryNS;
using ActivePlayerData;
using Actions;
using Curve;
using System.IO;
using System.Data.Common;

namespace PlayerScores
{
    //Stores and Handles local recorded scores when active.
    //Saves best score on each song since activation
    public class BeatLeaderPlayerScoreManager : IPlayerScores
    {

        public ActivePlayer ActivePlayer { get; set; }
        public SongSuggest songSuggest => ActivePlayer.songSuggest;
        private ScoreCollection scoreCollection = new ScoreCollection();
        private List<PlayerScore> playerScores => scoreCollection.PlayerScores;

        public void Load()
        {
            scoreCollection = songSuggest.fileHandler.LoadScoreCollection($"BL{ActivePlayer.PlayerID}");
            songSuggest.log?.WriteLine();

            songSuggest.log?.WriteLine($"BL Cache Scores Loaded: ScoresMeta.FormatVersion({scoreCollection.ScoresMeta.FormatVersion})  ScoresMeta.DataVersion({scoreCollection.ScoresMeta.DataVersion})");
        }

        public void Save()
        {
            songSuggest.log?.WriteLine($"Saving Beat Leader Scores");
            songSuggest.fileHandler.SaveScoreCollection(scoreCollection, $"BL{ActivePlayer.PlayerID}");
        }

        public void Refresh()
        {
            //Reset cached scores if there has been an update to ranked songs
            ClearIfOutdated();

            //If there are no stored scores, we set timestamp to 0, meaning we will get all, else we grab newest recorded score and grab scores after.
            long lastUpdateAsUnixTimestamp = (playerScores.Count() > 0) ? ((DateTimeOffset)playerScores.OrderByDescending(c => c.TimeSet).First().TimeSet).ToUnixTimeSeconds():0;
            int scoresPerPage = 100;
            int loadedPages = 0;
            int records = 1; //Needs to be larger than 0, to ensure we make first loop

            //Loop all players records after the given timestamp.
            while (loadedPages * scoresPerPage < records)
            {
                //Pages are 1 indexed, so we need to add 1 to get next unprocessed page
                ScoresCompact scores = songSuggest.webDownloader.GetBeatLeaderScoresCompact(ActivePlayer.PlayerID, scoresPerPage, loadedPages + 1, lastUpdateAsUnixTimestamp);

                //If something in the request fails (web access etc), we get an empty scores object, we received previous updates chronological, so any received
                //in previous batch has been older, so we can on next Refresh() continue where we crashed, but we only save to disk after a fully completed refresh
                if (scores.metadata == null) return;

                //Update status
                loadedPages = scores.metadata.page;
                records = scores.metadata.total;

                //Process each found record.
                foreach (var record in scores.data)
                {
                    ////Check for boosted modifiers and skip to next record if found
                    //string modifiers = record.score.modifiers;
                    //bool boostedModifiers = modifiers.Contains("SF") ||
                    //                        modifiers.Contains("FS") || 
                    //                        modifiers.Contains("GN") ||
                    //                        modifiers.Contains("GN") ||
                    //                        modifiers.Contains("NA") ||
                    //                        modifiers.Contains("NB") ||
                    //                        modifiers.Contains("NF") ||
                    //                        modifiers.Contains("SS") ||
                    //                        modifiers.Contains("NO");
                    //if (boostedModifiers) continue;

                    Song song = ((BeatLeaderID)record.leaderboard.id).GetSong();
                    
                    //If song is unknown by SongLibrary it is a non ranked score and we skip it.
                    //**Optional Upsert the song in the future via DataItem record**
                    if (song == null) continue;
                    //We store scores via Internal ID type
                    var playerScore = playerScores.Find(c => c.SongID == song.internalID);
                    //Check if score is present and update its values
                    if (playerScore != null)
                    {
                        playerScore.TimeSet = DateTimeOffset.FromUnixTimeSeconds(record.score.epochTime).UtcDateTime;
                        playerScore.Accuracy = record.score.accuracy;
                        playerScore.RatedScore = record.score.pp;
                    }
                    //Else create a new score and add it
                    else
                    {
                        var newScore = new PlayerScore()
                        {
                            SongName = song.name,
                            RatedScore = record.score.pp,
                            Accuracy = record.score.accuracy,
                            TimeSet = DateTimeOffset.FromUnixTimeSeconds(record.score.epochTime).UtcDateTime,
                            SongID = song.internalID
                        };
                        playerScores.Add(newScore);
                    }
                }

                songSuggest.log?.WriteLine($"Completed Pages: {loadedPages}/{(records - 1) / scoresPerPage + 1}");
            }
            Save();
            songSuggest.log?.WriteLine($"BL Scores: {playerScores.Count()}");
        }

        public void Clear()
        {
            scoreCollection = new ScoreCollection();
            scoreCollection.ScoresMeta.DataVersion = $"{songSuggest.filesMeta.beatLeaderLeaderboardUpdated}";
            Save();
        }

        public void ClearIfOutdated()
        {
            songSuggest.log?.WriteLine("Clear Outdated Check: Beat Leader");
            if (!scoreCollection.Validate($"{songSuggest.filesMeta.beatLeaderLeaderboardUpdated}"))
            {
                Clear();
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
                    return ScoreSaberCurve.PP(score.Accuracy, song.starScoreSaber);
                case LeaderboardType.AccSaber:
                    return AccSaberCurve.AP(score.Accuracy, song.complexityAccSaber);
                case LeaderboardType.BeatLeader:
                    return score.RatedScore;
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
            
            log?.WriteLine($"BeatLeader Score Count: {playerScores.Count()}");
        }
    }
}