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
        public double Accuracy => (double)Points/Song.MaxScore;
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
                songSuggest.log.WriteLine("No songs was given for refresh, use .Clear to remove all data instead, no action performed in refresh.");
                return; 
            }
            
            //Remove songs no longer in target list.
            songs = songs.Where(c => targetSongsIDs.Contains(c.SongID)).ToList();

            //Get new songs and set their initial data.
            List<String> missingSongIDs = targetSongsIDs.Except(songs.Select(c => c.SongID)).ToList();

            //Prepare loop information and autosave timer.
            int totalSongs = songs.Count();
            int currentSong = 0;
            int songsMissingDataCount = songs.Where(c => c.PlayerScores.Count() == 0 || c.Updated < obsoletePoint).Count();

            int minuteSaveInterval = 2;
            Save();
            songSuggest.log.WriteLine($"Saved");
            DateTime lastSave = DateTime.UtcNow;

            //Loop missing songs and add song entry
            foreach (var songID in missingSongIDs)
            {
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
                    int comboLoss = 7245; //Amount of points lost potentially due to missing combo at start.

                    //Insert Workaround for songs without max score
                    switch (songID)
                    {
                        //Robber and bouqet     776 : 332538
                        case "332538":
                            tmpSong.MaxScore = 776 * 115 - comboLoss;
                            break;
                        //Tell me you know      396 : 463149                        
                        case "463149":
                            tmpSong.MaxScore = 396 * 115 - comboLoss;
                            break;
                        //All my love           260 : 418921
                        case "418921":
                            tmpSong.MaxScore = 260 * 115 - comboLoss;
                            break;
                        //What you know         242 : 568102
                        case "568102":
                            tmpSong.MaxScore = 242 * 115 - comboLoss;
                            break;
                        //Waiting for love      420 : 544407
                        case "544407":
                            tmpSong.MaxScore = 420 * 115 - comboLoss;
                            break;
                        //simulation            212 : 368917
                        case "368917":
                            tmpSong.MaxScore = 212 * 115 - comboLoss;
                            break;
                        default:
                            songSuggest.log.WriteLine($"Song has no maxScore, nor known alternate: {songID}");
                            break;
                    }
                }
                songs.Add(tmpSong);
            }

            //Loop songs and update if needed.
            foreach (var song in songs)
            {
                string songID = song.SongID;
                string name = songSuggest.songLibrary.songs[songID].name;

                songSuggest.log.WriteLine($"Starting Song: {songID} - {name}.");
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
                        if (page%10 == 0) songSuggest.log.WriteLine($"Page: {page} / {totalPages} Last Acc: {lastAcc:0.000}%");
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
                            if (page%10==9) lastAcc = tmpScore.Accuracy*100;
                        }
                        page++;
                    }

                    //Song Update was completed
                    song.Updated = DateTime.UtcNow;
                    songsMissingDataCount--;
                    TimeSpan timeUntilNextSave = lastSave.AddMinutes(minuteSaveInterval) - DateTime.UtcNow;
                    string saveTimePrefix = timeUntilNextSave.CompareTo(0) < 0 ? "-" : "";
                    songSuggest.log.WriteLine($"Status: {currentSong}/{totalSongs} songs checked. {songsMissingDataCount} songs still need data. {saveTimePrefix}{timeUntilNextSave:mm\\:ss} until next save.");
                }

                if ((DateTime.UtcNow - lastSave).TotalMinutes > minuteSaveInterval)
                {
                    Save();
                    songSuggest.log.WriteLine($"***Saved {DateTime.Now}***");
                    lastSave = DateTime.UtcNow;
                }
                currentSong++;

            }
            songSuggest.log.WriteLine($"Status: {currentSong}/{totalSongs} songs checked. {songsMissingDataCount} songs still need data");
            Save();
            songSuggest.log.WriteLine($"All Songs Processed Saved");
        }

        //Clears stored Data to start a clean pull based on requested songs
        public void Clear()
        {
            songs.Clear();
        }

        public void GenerateLeaderboard()
        {
            foreach(var song in songs)
            {
                foreach(var score in song.PlayerScores)
                {
                    string playerID = score.PlayerID;
                    if(!players.ContainsKey(playerID))
                    {
                        AccPlayer tmpPlayer = new AccPlayer() {PlayerID = playerID};
                        players.Add(playerID,tmpPlayer);
                    }
                    score.Player = players[playerID];
                    score.Player.Scores.Add(score);
                }
            }

            //Remove players with less than 20 scores.
            players = new SortedDictionary<string, AccPlayer>(
                players
                    .Where(pair => pair.Value.Scores.Count >= 20)
                    .ToDictionary(pair => pair.Key, pair => pair.Value)
            );

            //Reduce the scores to the 20 best score
            foreach (var player in players.Values)
            {
                player.Scores = player.Scores
                    .OrderByDescending(c => c.AP)
                    .Take(20)
                    .ToList();
            }

            //Remove players with too large a gap in their scores.
            double spread = 0.80; //lowest value must be at least 80% of first.
            players = new SortedDictionary<string, AccPlayer>(
                players
                    .Where(pair => pair.Value.Scores.Last().AP > pair.Value.Scores.First().AP*spread)
                    .ToDictionary(pair => pair.Key, pair => pair.Value)
            );

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
            songSuggest.accSaberScoreBoard.Save();
        }

    }



//Notecount on removed songs.
//Robber and bouqet - 776
//Tell me you know - 396
//All my love - 260
//What you know - 242
//Waiting for love - 420
//simulation - 212
}
