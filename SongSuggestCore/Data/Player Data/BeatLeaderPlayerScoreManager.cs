using BeatLeaderJson;
using SongSuggestNS;
using System;
using System.Collections.Generic;
using System.Linq;
using SongLibraryNS;

namespace PlayerScores
{
    //Stores and Handles local recorded scores when active.
    //Saves best score on each song since activation
    public class BeatLeaderPlayerScoreManager
    {
        public bool updated { get; set; }
        public SongSuggest songSuggest { get; set; }
        public List<BeatLeaderPlayerScore> playerScores;

        public void Load()
        {
            playerScores = songSuggest.fileHandler.LoadBeatLeaderScores();
        }

        public void Save()
        {
            songSuggest.log?.WriteLine($"Saving Score");
            songSuggest.fileHandler.SaveBeatLeaderScores(playerScores);
            updated = false;
        }

        public void Refresh()
        {
            //Reset cached scores if there has been an update to ranked songs
            var meta = songSuggest.filesMeta;
            if (meta.beatLeaderPlayerScoresDate < meta.beatLeaderLeaderboardUpdated)
            {
                playerScores.Clear();
                Save();
                meta.beatLeaderPlayerScoresDate = meta.beatLeaderLeaderboardUpdated;
                songSuggest.fileHandler.SaveFilesMeta(meta);
            }

            //If there are no stored scores, we set timestamp to 0, meaning we will get all, else we grab newest recorded score and grab scores after.
            long lastUpdateAsUnixTimestamp = (playerScores.Count() > 0) ? ((DateTimeOffset)playerScores.OrderByDescending(c => c.TimeSet).First().TimeSet).ToUnixTimeSeconds():0;
            int scoresPerPage = 100;
            int loadedPages = 0;
            int records = 1; //Needs to be larger than 0, to ensure we make first loop

            //Loop all players records after the given timestamp.
            while (loadedPages * scoresPerPage < records)
            {
                //Pages are 1 indexed, so we need to add 1 to get next unprocessed page
                ScoresCompact scores = songSuggest.webDownloader.GetBeatLeaderScoresCompact(scoresPerPage, loadedPages + 1, lastUpdateAsUnixTimestamp);

                //If something in the request fails (web access etc), we get an empty scores object, we received previous updates chronoligcal, so any received
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

                    var playerScore = playerScores.Find(c => c.SongID == record.leaderboard.id);
                    //Check if score is present and update its values
                    if (playerScore != null)
                    {
                        playerScore.TimeSet = DateTimeOffset.FromUnixTimeSeconds(record.score.epochTime).UtcDateTime;
                        playerScore.Accuracy = record.score.accuracy;
                        playerScore.PP = record.score.pp;
                    }
                    //Else create a new score and add it
                    else
                    {
                        var score = new BeatLeaderPlayerScore()
                        {
                            PP = record.score.pp,
                            Accuracy = record.score.accuracy,
                            TimeSet = DateTimeOffset.FromUnixTimeSeconds(record.score.epochTime).UtcDateTime,
                            SongID = record.leaderboard.id
                        };
                        playerScores.Add(score);
                    }
                }

                songSuggest.log?.WriteLine($"Completed Pages: {loadedPages}/{(records - 1) / scoresPerPage + 1}");
            }
            Save();
        }

        public List<SongID> GetScores(SongCategory songCategory)
        {
            var stringIDs = playerScores
                .Select(c => c.SongID)
                .ToList();

            var songIDs = SongLibrary.StringIDToSongID(stringIDs, SongIDType.BeatLeader)
                .Where(c => SongLibrary.HasAnySongCategory(c, songCategory)) //Select scores from Leaderboard with at least 1 match
                .ToList();

             return songIDs;
        }

        public double GetAccuracy(string songID)
        {
            var score = playerScores.Where(c => c.SongID == songID)
                .OrderByDescending(c => c.Accuracy)
                .Select(c => c.Accuracy);

            if (score.Count() == 0) return 0;
            return score.First();
        }

        public DateTime GetTimeSet(string songID)
        {
            var score = playerScores.Where(c => c.SongID == songID)
                .OrderByDescending(c => c.TimeSet)
                .Select(c => c.TimeSet);

            if (score.Count() == 0) return DateTime.MinValue;
            return score.First();
        }
    }
}