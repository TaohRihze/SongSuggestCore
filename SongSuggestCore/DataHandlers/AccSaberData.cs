using Newtonsoft.Json;
using ScoreSabersJson;
using SongSuggestNS;
using System;
using System.Collections.Generic;
using Curve;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.ComponentModel.Design;
using SongLibraryNS;
using WebDownloading;
using LinkedData;
using Settings;

namespace AccSaberData
{
    public class Score
    {
        public string PlayerID { get; set; }
        public int Points { get; set; }  //points
        public double AP => AccSaberCurve.AP(Accuracy, Song.Complexity);
        [JsonIgnore]
        public SongData Song { get; set; }
        [JsonIgnore]
        public AccPlayer Player { get; set; }
        public double Accuracy => (double)Points / Song.MaxScore;
    }

    public class AccPlayer
    {
        public string PlayerID { get; set; }
        public List<Score> Scores { get; set; } = new List<Score>();
    }

    public class SongData
    {
        public DateTime Updated { get; set; }
        public string SongID { get; set; }               //ID on song
        public double Complexity { get; set; }
        public int MaxScore { get; set; }
        public List<Score> PlayerScores { get; set; }
    }

    //Suported ways to handle leaderboards score selection
    public enum AccSaberLeaderboardSongSelection
    {
        Top20Scores,
        BalancedLeaderboardSampling
    }

    public class AccSaberSongs
    {
        public SongSuggest songSuggest { get; set; }
        public List<SongData> songs { get; set; }
        public SortedDictionary<String, AccPlayer> players = new SortedDictionary<String, AccPlayer>();
        private List<String> targetSongsIDs;

        public void SetSongs(List<string> songIDs)
        {
            targetSongsIDs = songIDs;
        }

        public void Load()
        {
            songs = songSuggest.fileHandler.LoadAccSaberSongs();

            //reset links between songs and scores.
            foreach (var song in songs)
            {
                foreach (var score in song.PlayerScores)
                {
                    score.Song = song;
                }
            }
        }

        public void Save()
        {
            songSuggest.fileHandler.SaveAccSaberSongs(songs);
        }

        //Refresh data, only missing information should be updated
        public void Refresh(SongSuggest songSuggest)
        {
            Refresh(songSuggest, DateTime.MinValue);
        }

        //Updates the stored Songs first, then refresh data if needed (0 entries, or older than obsoletePoint.
        public void Refresh(SongSuggest songSuggest, DateTime obsoletePoint)
        {
            //If no songs have been set as targets, things are likely an error, lets treat it as such. (just call .Clear if all should be removed)
            if (targetSongsIDs.Count == 0)
            {
                songSuggest.log?.WriteLine("No songs was given for refresh, use .Clear to remove all data instead, no action performed in refresh.");
                return;
            }

            //Remove songs no longer in target list.
            songs = songs.Where(c => targetSongsIDs.Contains(c.SongID)).ToList();

            songSuggest.log?.WriteLine($"Songs in Request: {songs.Count()}");

            //Get new songs and set their initial data.
            List<String> missingSongIDs = targetSongsIDs.Except(songs.Select(c => c.SongID)).ToList();

            //Prepare loop information and autosave timer.
            int currentSong = 0;

            int minuteSaveInterval = 2;
            Save();
            songSuggest.log?.WriteLine($"Saved");
            DateTime lastSave = DateTime.UtcNow;


            int missingSongIDsCount = missingSongIDs.Count();
            songSuggest.log?.WriteLine($"Songs missing SongData: {missingSongIDsCount}");
            //Loop missing songs and add song entry
            foreach (var songID in missingSongIDs)
            {
                if (currentSong % 10 == 0) songSuggest.log?.WriteLine($"Getting Song Data: ({currentSong}/{missingSongIDsCount})");
                LeaderboardInfo board = songSuggest.webDownloader.GetLeaderboardInfo(songID);

                var tmpSong = new SongData()
                {
                    SongID = songID,
                    MaxScore = board.maxScore,
                    PlayerScores = new List<Score>(),
                    Updated = DateTime.MinValue,
                };

                if (tmpSong.MaxScore == 0)
                {
                    tmpSong.MaxScore = ManualData.SongMaxScore(songID, songSuggest);
                }
                songs.Add(tmpSong);
                currentSong++;

            }

            //Set data for looping the songs that are missing song data updates.
            List<SongData> songsMissingData = songs.Where(c => c.PlayerScores.Count() == 0 || c.Updated < obsoletePoint).ToList();
            int songsMissingDataCount = songsMissingData.Count();
            currentSong = 0;

            //Loop songs and update if needed.
            foreach (var song in songsMissingData)
            {
                string songID = song.SongID;
                string name = songSuggest.songLibrary.songs[songID].name;

                songSuggest.log?.WriteLine($"Starting Song: {songID} - {name}.");
                //reset the Complexity
                song.Complexity = songSuggest.songLibrary.songs[song.SongID].complexityAccSaber;

                //Perform refresh on songs that are obsolette or has 0 scores assigned (new).
                if (song.PlayerScores.Count == 0 || song.Updated < obsoletePoint)
                {
                    //Get Song meta from web
                    LeaderboardInfo songMeta = songSuggest.webDownloader.GetLeaderboardInfo(songID);

                    //12 scores per page, we need to figure out how many pages at most there is (just to give us an idea of how far we need to look and where we are,
                    //likely we will not need to parse them all to hit the 95% acc mark.
                    int totalPages = (int)Math.Ceiling((double)songMeta.plays / 12);
                    int page = 1;
                    double targetAcc = 0.95;
                    bool lowScoreFound = false;
                    double lastAcc = 0;

                    //Loop all pages until a lowscore is found.
                    while (page <= totalPages && !lowScoreFound)
                    {
                        if (page % 10 == 0) songSuggest.log?.WriteLine($"Page: {page} / {totalPages} Last Acc: {lastAcc:0.000}%");
                        ScoreCollection scorePage = songSuggest.webDownloader.GetLeaderboardScores(songID, page);

                        //Loop the 12 scores.
                        foreach (var score in scorePage.scores)
                        {
                            //Break if acc < 95%
                            if (score.modifiedScore < targetAcc * song.MaxScore)
                            {
                                lowScoreFound = true;
                                break;
                            }

                            Score tmpScore = new Score()
                            {
                                PlayerID = score.leaderboardPlayerInfo.id,
                                Points = score.modifiedScore,
                                Song = song,
                            };

                            //Add it to the data
                            song.PlayerScores.Add(tmpScore);
                            if (page % 10 == 9) lastAcc = tmpScore.Accuracy * 100;
                        }
                        page++;
                    }

                    //Song Update was completed
                    song.Updated = DateTime.UtcNow;
                    songsMissingDataCount--;
                    TimeSpan timeUntilNextSave = lastSave.AddMinutes(minuteSaveInterval) - DateTime.UtcNow;
                    string saveTimePrefix = timeUntilNextSave < TimeSpan.Zero ? "-" : "";
                    songSuggest.log?.WriteLine($"Status: {currentSong}/{songsMissingDataCount + currentSong} songs checked. {songsMissingDataCount} songs still need data. {saveTimePrefix}{timeUntilNextSave:mm\\:ss} until next save.");
                }

                if ((DateTime.UtcNow - lastSave).TotalMinutes > minuteSaveInterval)
                {
                    Save();
                    songSuggest.log?.WriteLine($"***Saved {DateTime.Now}***");
                    lastSave = DateTime.UtcNow;
                }
                currentSong++;

            }
            songSuggest.log?.WriteLine($"Status: {currentSong}/{songsMissingDataCount} songs checked. {songsMissingDataCount} songs still need data");
            Save();
            songSuggest.log?.WriteLine($"All Songs Processed Saved");
        }

        //Clears stored Data to start a clean pull based on requested songs
        public void Clear()
        {
            songs.Clear();
        }

        //Default Leaderboard
        public void GenerateLeaderboard()
        {
            GenerateLeaderboard(AccSaberLeaderboardSongSelection.Top20Scores);
        }

        public void GenerateLeaderboard(AccSaberLeaderboardSongSelection songSelection)
        {
            foreach (var song in songs)
            {
                foreach (var score in song.PlayerScores)
                {
                    string playerID = score.PlayerID;
                    if (!players.ContainsKey(playerID))
                    {
                        AccPlayer tmpPlayer = new AccPlayer() { PlayerID = playerID };
                        players.Add(playerID, tmpPlayer);
                    }
                    score.Player = players[playerID];
                    score.Player.Scores.Add(score);
                }
            }

            songSuggest.log?.WriteLine($"Players with any scores: {players.Count}");

            //Remove players with less than 20 scores. (Initial player pruning)
            players = new SortedDictionary<string, AccPlayer>(
                players
                    .Where(pair => pair.Value.Scores.Count >= 20)
                    .ToDictionary(pair => pair.Key, pair => pair.Value)
            );
            songSuggest.log?.WriteLine($"Players with 20 scores before score selection: {players.Count}");


            //Perform the selection of top 20 scores. (might return less than 20 scores)
            switch (songSelection)
            {
                case AccSaberLeaderboardSongSelection.Top20Scores:
                    Top20Scores(players);
                    break;
                case AccSaberLeaderboardSongSelection.BalancedLeaderboardSampling:
                    //Try and get scores from as many leaderboards as possible, starting with a max of 8 samples from each.
                    BalancedLeaderboardSampling(players.Values.ToList(), 8);
                    break;
            }

            //Remove players with less than 20 scores (if the selection methode did not grab 20 scores).
            players = new SortedDictionary<string, AccPlayer>(
                players
                    .Where(pair => pair.Value.Scores.Count >= 20)
                    .ToDictionary(pair => pair.Key, pair => pair.Value)
            );

            songSuggest.log?.WriteLine($"Players with 20 scores after score selection: {players.Count}");

            //Remove players with too large a gap in their scores.
            double spread = 0.80; //lowest value must be at least 80% of first.
            players = new SortedDictionary<string, AccPlayer>(
                players
                    .Where(pair => pair.Value.Scores.First().AP * spread < pair.Value.Scores.Last().AP)
                    .ToDictionary(pair => pair.Key, pair => pair.Value)
            );

            songSuggest.log?.WriteLine($"Players with low spread: {players.Count}");

            List<Top10kPlayer> top10kPlayers = new List<Top10kPlayer>();

            int playerRank = 0;
            foreach (var player in players.Values)
            {
                playerRank++;
                Top10kPlayer newPlayer = new Top10kPlayer()
                {
                    id = player.PlayerID,
                    rank = playerRank,
                    top10kScore = new List<Top10kScore>()
                };
                top10kPlayers.Add(newPlayer);

                int scoreRank = 0;
                foreach (var score in player.Scores)
                {
                    scoreRank++;
                    Top10kScore newScore = new Top10kScore()
                    {
                        songID = score.Song.SongID,
                        pp = (float)score.AP,
                        rank = scoreRank
                    };
                    newPlayer.top10kScore.Add(newScore);
                }


            }


            //Reset current acc saber board and clear it.
            songSuggest.accSaberScoreBoard.top10kPlayers = top10kPlayers;
            songSuggest.accSaberScoreBoard.top10kSongMeta.Clear();
            songSuggest.accSaberScoreBoard.GenerateTop10kSongMeta();
            songSuggest.accSaberScoreBoard.Save("AccSaberLeaderboardData");
        }
        
        //Reduce the scores of players to the 20 best score
        private void Top20Scores(SortedDictionary<string, AccPlayer> players)
        {
            foreach (var player in players.Values)
            {
                player.Scores = player.Scores
                    .OrderByDescending(c => c.AP)
                    .Take(20)
                    .ToList();
            }
        }

        //Select at most 8 scores of each category weighted by AP (8, 8, 4 is an option)
        //Then any non updated score is given a recursive handling until 20 scores from a single leaderboard is found.)
        private void BalancedLeaderboardSampling(List<AccPlayer> players, int maxGroupSamples)
        {
            //Settings variables.
            int maxScores = 20; //Target scores to return.
            double maxSpread = 0.9;

            SongCategory activeCategories = SongCategory.AccSaberTrue | SongCategory.AccSaberStandard | SongCategory.AccSaberTech;
            //Helper function which checks if a score has a given category active via masterdata.
            Func<Score, SongCategory, bool> validScore = (candidate, group) => songSuggest.songLibrary.HasAllSongCategory(candidate.Song.SongID, group);

            List<AccPlayer> invalidPlayers = new List<AccPlayer>();

            foreach (var player in players)
            {
                var sortedScores = player.Scores.OrderByDescending(candidate => candidate.AP).ToList();

                // Lets get the sample size from all Acc Saber categories, and then reduce the list if needed.
                var scores = Enum.GetValues(typeof(SongCategory))                               //Get Int values for all SongCategory
                .Cast<SongCategory>()                                                           //Convert the list to actual enum type
                .Where(value => activeCategories.HasFlag(value))                                //Reduce list to active groups only
                .Select(group => sortedScores.Where(candidate => validScore(candidate,group)))  //Sort candidates into groups based on their enum
                .SelectMany(groupCandidates => groupCandidates.Take(maxGroupSamples))           //Combine all lists to a single with a max selection
                .OrderByDescending(candidate => candidate.AP)                                   //Order has been changed by groups, so we need to resort
                .Take(maxScores)                                                                //Reduce the list to the found target
                .ToList();                                                                      //And turn it back into a list for later processing

                //Checks if a player in invalid (less than 20 scores, or too large a spread in scores.
                //If issue is found record player and skip to next.
                if (scores.Count() < 20 || scores.First().AP * maxSpread > scores.Last().AP)
                {
                    invalidPlayers.Add(player);
                    continue;
                }

                //Player is valid, record the scores.
                player.Scores = scores;
            }

            songSuggest.log?.WriteLine($"Invalid Players at max {maxGroupSamples} samples per group: {invalidPlayers.Count()}");

            //Perform a recursive reduction in score requirements until we tried to find 20 valid scores.
            int extraScores = 2;
            if (invalidPlayers.Count() == 0 || maxGroupSamples < maxScores)
            {
                int newTarget = Math.Min(maxGroupSamples + extraScores, maxScores);
                BalancedLeaderboardSampling(invalidPlayers, newTarget);
            }

            //in last loop we remove the scores of any unbalanced players instead
            if (maxGroupSamples == maxScores)
            {
                foreach (var player in invalidPlayers)
                {
                    player.Scores.Clear();
                }
            }
        }
    }
}