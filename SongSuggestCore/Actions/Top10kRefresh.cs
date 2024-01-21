using Data;
using SongSuggestNS;
using FileHandling;
using LinkedData;
using ScoreSabersJson;
using SongLibraryNS;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using WebDownloading;
using System.Collections.Generic;
using System.Xml.Linq;
using System.ComponentModel.Design;

namespace Actions
{

    public class Top10kRefresh
    {
        public SongSuggest songSuggest { get; set; }
        private Top10kPlayers top10kPlayers;

        //Updates the Meta file based on if update is a major version or not.
        public void UpdateFilesMeta(bool majorVersionSwitch)
        {
            FilesMeta filesMeta = songSuggest.fileHandler.LoadFilesMeta();

            if (majorVersionSwitch)
            {
                filesMeta.UpdateMajor(FilesMetaType.Top10kVersion);
                filesMeta.UpdateMajor(FilesMetaType.SongLibraryVersion);
            }
            else
            {
                filesMeta.UpdateMinor(FilesMetaType.Top10kVersion);
                filesMeta.UpdateMinor(FilesMetaType.SongLibraryVersion);
            }

            filesMeta.top10kUpdated = DateTime.UtcNow;

            songSuggest.fileHandler.SaveFilesMeta(filesMeta);
        }

        //Grabs top 30 plays for each players
        //Filters out players with too large a distance between 1st and 30th play
        //Filter out the 10 plays that are the lowest acc (likely too hard)
        public void AlternativeTop10kPlayerDataPuller(ref Top10kPlayers fullInfoPlayers, bool largeUpdate)
        {
            double maxSpread = 0.7; //closer to 1 the lower the spread

            //FileHandler fileHandler = songSuggest.fileHandler;
            WebDownloader webDownloader = songSuggest.webDownloader;
            SongLibraryInstance songLibrary = songSuggest.songLibrary;
            //Throttler throttler = webDownloader.ssThrottler;
            
            top10kPlayers = new Top10kPlayers() { songSuggest = songSuggest };

            //counters of progress
            int playerCount = 0;
            int skippedPlayers = 0;
            int lowPlayCount = 0;
            List<string> lowPlayCountID = new List<string>();
            int lowEfficiencyPlayers = 0;
            List<string> lowEfficiencyPlayersID = new List<string>();
            int inactivePlayers = 0;
            List<string> inactivePlayersID = new List<string>();
            int candidatePage = 1;

            List<Player> candidates = webDownloader.GetPlayers(candidatePage++).players.ToList();
            songSuggest.log?.WriteLine("Starting to Download Users");

            //Continue until 10k valid players are found (or too many players are skipped)
            while (playerCount < 10000 && playerCount+skippedPlayers < 15000)
            {
                //Update on progress
                if ((playerCount + skippedPlayers) % 100 == 0) songSuggest.log?.WriteLine("Found Users: {0} Skipped Users: {1} ({2} low efficiency / {3} low play / {4} inactive) Total: {5}", playerCount, skippedPlayers, lowEfficiencyPlayers, lowPlayCount, inactivePlayers, playerCount + skippedPlayers);

                //Grab a new batch of players if out of players
                if (candidates.Count() == 0)
                {
                    candidates = webDownloader.GetPlayers(candidatePage++).players.ToList();
                }

                //Get next Candidate.
                var candidate = candidates.First();
                candidates.Remove(candidate);

                //Lets Generate a player based on the candidate and validate it
                var currentPlayer = new Top10kPlayer()
                {
                    id = "" + candidate.id,
                    name = candidate.name,
                    rank = playerCount+1 //For compatibility we need to keep ranges from 1 to 10k players, so consider rank as rank of the 10k approved players
                };

                //Lets grab the top 30 scores of that player and get them added
                PlayerScoreCollection playerScoreCollection = webDownloader.GetScores(currentPlayer.id, "top", 30, 1);

                //***These failed a few times, lets try this again, and if it fails once more, log this failure***
                if (playerScoreCollection.playerScores == null)
                {
                    songSuggest.log?.WriteLine($"Player ID: {currentPlayer.id} failed to download. Page: {candidatePage}");
                    playerScoreCollection = webDownloader.GetScores(currentPlayer.id, "top", 30, 1);
                    if (playerScoreCollection.playerScores == null) songSuggest.log?.WriteLine("Failed Again!");
                    continue;
                }

                //Let us create a Top10k score for each of these, and make a list sorted by their acc (else that data is lost)
                List<(float acc, Top10kScore score)> accList = new List<(float, Top10kScore)>();

                DateTime newestScore = DateTime.MinValue;

                foreach (PlayerScore score in playerScoreCollection.playerScores)
                {
                    if (score.leaderboard.ranked)
                    {
                        songLibrary.UpsertSong(score.leaderboard);
                        Top10kScore tmpScore = new Top10kScore();
                        tmpScore.songID = score.leaderboard.id + "";
                        tmpScore.pp = score.score.pp;


                        score.acc = 1.0f * score.score.modifiedScore / score.leaderboard.maxScore;
                        float rankPercentile = score.score.rank/score.leaderboard.plays;

                        (float, Top10kScore) item = (score.acc, tmpScore);
                        accList.Add(item);

                        if (score.score.timeSet > newestScore) newestScore = score.score.timeSet;
                    }
                }

                //If a return of all players item has been made, store player in it before filtering.
                if (fullInfoPlayers != null)
                {
                    Top10kPlayer fullInfoPlayer = new Top10kPlayer()
                    {
                        id = "" + candidate.id,
                        name = candidate.name,
                        rank = playerCount + skippedPlayers + 1
                    };
                    foreach (var score in accList.Select(c => c.score))
                    {
                        fullInfoPlayer.top10kScore.Add(score);
                    }
                    fullInfoPlayers.top10kPlayers.Add(fullInfoPlayer);
                }

                //If the player does not have 30 scores skip the player
                if (accList.Count() < 30)
                {
                    skippedPlayers++;
                    lowPlayCount++;
                    lowPlayCountID.Add(""+candidate.id);
                    continue;
                }

                //If the player has not set a new score in 90 days skip the player
                if (DateTime.UtcNow.Subtract(newestScore).TotalDays > 365)
                {
                    skippedPlayers++;
                    inactivePlayers++;
                    inactivePlayersID.Add("" + candidate.id);
                    continue;
                }

                //Let us get the 20 best scores by acc
                var scores = accList.OrderByDescending(c => c.acc).Take(20).ToList();

                scores = scores.OrderByDescending(c => c.score.pp).ToList();

                //If spread on players pp is too large skip
                double playerSpread = scores.Last().score.pp/scores.First().score.pp;
                if (playerSpread < maxSpread)
                {
                    skippedPlayers++;
                    lowEfficiencyPlayers++;
                    lowEfficiencyPlayersID.Add("" + candidate.id);
                    continue;
                }

                //Set the scores rank by PP and add them to the player
                int scoreRank = 1;
                foreach (var score in scores.Select(c=> c.score).OrderByDescending(c => c.pp))
                {
                    score.rank = scoreRank;
                    currentPlayer.top10kScore.Add(score);
                    scoreRank++;
                }

                //Add the player to the list of players
                top10kPlayers.top10kPlayers.Add(currentPlayer);
                playerCount++;
            }

            //Update on progress
            songSuggest.log?.WriteLine("Found Users: {0} Skipped Users: {1} ({2} low efficiency / {3} low play / {4} inactive) Total: {5}", playerCount, skippedPlayers, lowEfficiencyPlayers, lowPlayCount, inactivePlayers, playerCount+skippedPlayers);

            if (songLibrary.Updated) songLibrary.Save();
            UpdateFilesMeta(largeUpdate);
            top10kPlayers.Save();
            top10kPlayers.GenerateTop10kSongMeta();
            songSuggest.scoreSaberScoreBoard = top10kPlayers;
        }
    }
}