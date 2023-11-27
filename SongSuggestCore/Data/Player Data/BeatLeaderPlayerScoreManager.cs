using BeatLeaderJson;
using SongSuggestNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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

        public List<string> GetScores(SongCategory songCategory)
        {
            var songs = playerScores
                .Select(c => c.SongID)
                .Where(c => songSuggest.songLibrary.HasAnySongCategory(c, songCategory))    //Select scores from Leaderboard with at least 1 match
                .ToList();                                                                  //Create the List needed
             return songs;
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