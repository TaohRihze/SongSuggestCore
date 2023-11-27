using SongSuggestNS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerScores
{
    //Stores and Handles local recorded scores when active.
    //Saves best score on each song since activation
    public class LocalPlayerScoreManager
    {
        public bool updated { get; set; }
        public SongSuggest songSuggest { get; set; }
        public List<LocalPlayerScore> playerScores;

        //Accuracy is a value of 0 to 1
        public void AddScore(string songID, double accuracy)
        {
            //Check if there is a local score and it needs updating
            var matchingScores = playerScores.Where(c => c.SongID == songID).ToList();

            //Check if matching scores are obsolette and either remove any obsolette scores, or if current are better mark return.
            //Later this might need handling of multiple scores on same song, and remove oldest/worst depending on settings.
            if (matchingScores.Count > 0)
            {
                bool returnWhenDone = false;
                foreach (var score in matchingScores)
                {
                    //For now remove better scores, might keep multiple scores later
                    if (score.Accuracy < accuracy)
                    {
                        playerScores.Remove(score);
                        updated = true;
                    }
                    else returnWhenDone = true;
                }
                if (returnWhenDone) return;
            }

            //Record the new score.
            var tmpScore = new LocalPlayerScore()
            {
                SongID = songID,
                SongName = songSuggest.songLibrary.songs[songID].name,
                Accuracy = accuracy,
                TimeSet = DateTime.UtcNow
            };
            playerScores.Add(tmpScore);
            updated = true;
        }

        public void Load()
        {
            playerScores = songSuggest.fileHandler.LoadLocalScores();
        }

        public void Save()
        {
            songSuggest.log?.WriteLine($"Saving Score");
            songSuggest.fileHandler.SaveLocalScores(playerScores);
            updated = false;
        }

        //public void AddScore(string songHash, string difficulty, double accuracy)
        //{
        //    string songID = songSuggest.songLibrary.GetID(songHash, difficulty);
        //    AddScore(songID, accuracy);
        //}

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