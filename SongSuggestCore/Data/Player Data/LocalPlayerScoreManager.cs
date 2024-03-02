using SongSuggestNS;
using System;
using System.Collections.Generic;
using System.Linq;
using SongLibraryNS;
using ActivePlayerData;
using ScoreSabersJson;
using Actions;
using Curve;
using System.IO;

namespace PlayerScores
{
    //Stores and Handles local recorded scores when active.
    //Saves best score on each song since activation
    public class LocalPlayerScoreManager : IPlayerScores
    {
        public ActivePlayer ActivePlayer { get; set; }
        public SongSuggest songSuggest => ActivePlayer.songSuggest;

        //Standard Storage for load/save
        private ScoreCollection scoreCollection = new ScoreCollection();

        //Internal Link allowing multiples scores per songID
        private Dictionary<SongID, List<PlayerScore>> groupedScores = new Dictionary<SongID, List<PlayerScore>>();

        private SongIDType _songIDType = SongIDType.Internal;
        internal bool updated;

        //Accuracy is a value of 0 to 1
        //New scores MUST be added to both the groupedScores and the scoreCollection, so it can be saved and used
        //(avoids having to recreate the full list for scoreCollection save every time a score is set).
        public void AddScore(SongID songID, double accuracy)
        {
            Song song = songID.GetSong();
            PlayerScore score = new PlayerScore()
            {
                SongID = song.internalID,
                SongName = song.name,
                TimeSet = DateTime.UtcNow,
                Accuracy = accuracy,
            };

            //Adds the score to the grouped dataset
            AddToGroupedScores(score);

            //We add the score to scoreCollection, and mark that we have updated our records.
            scoreCollection.PlayerScores.Add(score);
            updated = true;

            //Must inform manager its data has been updated
            ActivePlayer.CachedRankings.Clear();
        }

        //Lookup sorted scores, generated from load and added scores during the session, as this is based on scoreCollection, which is master additions to these do not modify updated.
        private void AddToGroupedScores(PlayerScore score)
        {
            //Add the score to both groupedScores and scoreCollection
            var songID = (SongID)(InternalID)score.SongID;
            List<PlayerScore> scores;

            //Either assign scores a known songID link, or create a new grouping and assign it to scores and dictionary
            if (!groupedScores.TryGetValue(songID, out scores)) groupedScores.Add(songID, scores = new List<PlayerScore>());

            //Add the score to the List grouping.
            scores.Add(score);
        }

        public void Load()
        {
            //Load data.
            scoreCollection = songSuggest.fileHandler.LoadScoreCollection($"Local{ActivePlayer.PlayerID}");

            //Clear current stored data (replace with load)
            groupedScores.Clear();

            //Loop each score.
            foreach (var score in scoreCollection.PlayerScores)
            {
                AddToGroupedScores(score);
            }
        }

        public void Save()
        {
            if (!updated) return;
            songSuggest.log?.WriteLine($"Saving Local Scores");
            scoreCollection.PlayerScores = scoreCollection.PlayerScores.OrderBy(c => c.SongName).ToList();
            songSuggest.fileHandler.SaveScoreCollection(scoreCollection, $"Local{ActivePlayer.PlayerID}");
            updated = false;
        }

        //Local Scores should not be cleared.
        public void Clear()
        {
            //scoreCollection = new ScoreCollection();
            //groupedScores.Clear();
            //Save();
        }

        //Yeah we do not refresh.
        public void Refresh()
        {
        }

        //Currently nothing gets outdated, all scores are calculated for use.
        public void ClearIfOutdated()
        {
        }

        //Returns the calculated score of the best stored song
        public double GetRatedScore(SongID songID, LeaderboardType leaderboardType)
        {
            var song = songID.GetSong();

            //Find the Grouped scores for the ID, and get the score with the best accuracy among those.
            //If no group is found we instead set the score to null
            PlayerScore score = groupedScores.TryGetValue(songID, out var scores)
                ? scores.OrderByDescending(c => c.Accuracy)
                        .FirstOrDefault()
                //No groupedScores. We cannot return in a ternary statement, so we assign null and return after.
                : null;

            //No score was found we return the default value.
            if (score == null) return 0;

            switch (leaderboardType)
            {
                case LeaderboardType.ScoreSaber:
                    return ScoreSaberCurve.PP(score.Accuracy, song.starScoreSaber);
                case LeaderboardType.AccSaber:
                    return AccSaberCurve.AP(score.Accuracy, song.complexityAccSaber);
                case LeaderboardType.BeatLeader:
                    return 0; //No Curve Calculation yet.
                default:
                    return 0;
            }
        }

        //Returns a List of SongID's of the current ID type stored here
        public List<SongID> GetRankedScoreIDs(SongCategory songCategory)
        {
            var songIDs = groupedScores
                .Select(c => c.Key)                                             //Get the unique known SongID's (we do not care about how many entries, as long we got any we use them)
                .Where(c => SongLibrary.HasAnySongCategory(c, songCategory))    //Select scores from Leaderboard with at least 1 match
                .ToList();                                                      //Create the List needed
            return songIDs;
        }

        public List<SongID> GetScoreIDs()
        {
            var songIDs = groupedScores
                .Select(c => c.Key)                                             //Get the unique known SongID's (we do not care about how many entries, as long we got any we use them)
                .ToList();                                                      //Create the List needed
            return songIDs;
        }

        public double GetAccuracy(SongID songID)
        {
            return groupedScores.TryGetValue(songID, out var scores)
                ? scores.OrderByDescending(c => c.Accuracy)
                    .Select(c => c.Accuracy)
                    .FirstOrDefault()
                : 0;
        }

        public DateTime GetTimeSet(SongID songID)
        {
            return groupedScores.TryGetValue(songID, out var scores)
                ? scores.OrderByDescending(c => c.Accuracy)
                    .Select(c => c.TimeSet)
                    .FirstOrDefault()
                : DateTime.MinValue;
        }

        public PlayerScore GetScore(SongID songID)
        {
            if (!groupedScores.ContainsKey(songID)) return null;
            return groupedScores[songID].OrderByDescending(score => score.Accuracy).First();
        }

        public bool Contains(SongID songID)
        {
            return groupedScores.ContainsKey(songID);
        }


        public void ShowCache(TextWriter log)
        {

            log?.WriteLine($"Local Score Count Grouped: {groupedScores.Count()}");
            log?.WriteLine($"                           {scoreCollection.PlayerScores.Count()}");
        }

        //Convert and Add old LocalScores
        public void ImportOldLocalScores()
        {
            var scores = songSuggest.fileHandler.LoadOldLocalScores();

            foreach (var score in scores)
            {
                SongID songID = (ScoreSaberID)score.SongID;
                Song song = songID.GetSong();

                if (song == null) continue;

                score.SongID = song.internalID;
                score.SongName = song.name;

                //Adds the score to the grouped dataset
                AddToGroupedScores(score);

                //We add the score to scoreCollection, and mark that we have updated our records.
                scoreCollection.PlayerScores.Add(score);
                updated = true;
            }

            Save();

            //Delete cached file
            songSuggest.fileHandler.RemoveOldLocalScores();
        }
    }
}