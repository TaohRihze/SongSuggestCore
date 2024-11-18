using System;
using System.Collections.Generic;
using System.IO;
using Actions;
using PlayerScores;
using SongLibraryNS;
using SongSuggestNS;

namespace ActivePlayerData
{
    //Source Manager connections.
    public interface IPlayerScores
    {
        ActivePlayer ActivePlayer { get; set; }

        //Loads the current score cache for the player on the related Scoreboard
        void Load();
        //Save the current score cache for the player on the related ScoreBoard
        void Save();
        //Clear all saved scores
        void Clear();
        //Verify if the loaded version is outdated compared to source data
        void ClearIfOutdated();
        //Refresh Data
        void Refresh();
        //Returns true if there is set a Score on the songID
        bool Contains(SongID songID);

        //Returns either DateTime of the score with the given ID, or min DateTime value
        DateTime GetTimeSet(SongID songID);

        //Returns the songs Accuracy in a 0-1 range (90% = 0.9)
        double GetAccuracy(SongID songID);

        //Returns the songs score on the Leaderboard (0 if unknown).
        double GetRatedScore(SongID songID, LeaderboardType leaderboardType);

        //Returns the Score Object of the given SongID
        PlayerScore GetScore(SongID songID);

        //Returns the SongIDs of songs in the given Leaderboard Categories
        List<SongID> GetRankedScoreIDs(SongCategory songCategory);
        //Returns the SongIDs of songs in the given Leaderboard Categories
        List<SongID> GetScoreIDs();
        void ShowCache(TextWriter log);
    }
}