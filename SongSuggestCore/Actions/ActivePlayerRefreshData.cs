﻿using System;
using System.Linq;
using SongLibraryNS;
using ScoreSabersJson;
using ActivePlayerData;
using WebDownloading;
using SongSuggestNS;

namespace Actions
{
    //Loads cached player data if available, or make a full data pull for a new user.
    //Parse activePlayers data via "recent" from web with Get Scores from Page 1 until a duplicate score is found
    //Checks if updated/added scores is same or higher than playcount total, if higher or equal stop (more scores could have been uploaded hency also higher)

    //Should be considered merged with Active Player ... need to decide on what is data, what is the action etc.
    public class ActivePlayerRefreshData
    {
        public SongSuggest songSuggest { get; set; }
        public void RefreshActivePlayer()
        {
            ActivePlayer activePlayer = songSuggest.activePlayer;
            WebDownloader webDownloader = songSuggest.webDownloader;
            SongLibraryInstance songLibrary = songSuggest.songLibrary;

            //Have the active player verify the active version and the wanted version is the same, else load any cached version or present an empty user
            activePlayer.Load(songSuggest);

            //Reset Data if required
            if (songSuggest.fileHandler.CheckPlayerRefresh())
            {
                activePlayer.ResetScores();
                songSuggest.fileHandler.TogglePlayerRefresh();
            }

            //Figure out which searchmode to use. If 0 count songs, go through all ranked, else update via recent
            String searchmode = (songSuggest.activePlayer.rankedPlayCount == 0) ? "top" : "recent";

            //Prepare for updating from web until a duplicate score is found (then remaining scores are correct)
            int page = 0;
            string maxPage = "?";
            Boolean continueLoad = true;
            while (continueLoad)
            {
                page++;
                songSuggest.status = "Downloading Player History Page: " + page + "/" + maxPage;
                songSuggest.log?.WriteLine("Page Start: " + page + " Search Mode: " + searchmode);
                PlayerScoreCollection playerScoreCollection = webDownloader.GetScores(songSuggest.activePlayerID, searchmode, 100, page);
                if (playerScoreCollection.metadata == null)
                { 
                    playerScoreCollection.metadata = new Metadata();
                    playerScoreCollection.playerScores = new PlayerScore[0];
                }
                maxPage = ""+Math.Ceiling((double)playerScoreCollection.metadata.total / 100);
                //PlayerScoreCollection playerScoreCollection = JsonConvert.DeserializeObject<PlayerScoreCollection>(scoresJSON, serializerSettings);
                songSuggest.status = "Parsing Player History Page: " + page + "/" + maxPage;
                songSuggest.log?.WriteLine("Page Parse: " + page);
                //Parse Player Scores
                foreach (PlayerScore score in playerScoreCollection.playerScores)
                {
                    //Update or add song to library via PlayerScore object.
                    songLibrary.UpsertSong(score);

                    //Some scores may not have a max score listed, so we need to verify this first and set it
                    if (score.leaderboard.maxScore == 0) score.leaderboard.maxScore = ManualData.SongMaxScore($"{score.leaderboard.id}", songSuggest);

                    //Create a score object from the website Score, and add it to the candidates
                    ScoreSaberPlayerScore tmpScore = new ScoreSaberPlayerScore
                    {
                        songID = score.leaderboard.id + "",
                        timeSet = score.score.timeSet,
                        pp = score.score.pp,
                        accuracy = 100.0*score.score.baseScore / score.leaderboard.maxScore,
                        rankPercentile = 100.0*score.score.rank / score.leaderboard.plays,
                        rankScoreSaber = score.score.rank,
                        playsScoreSaber = score.leaderboard.plays
                    };
                    //Attempts to add the found score, if it is a duplicate with same timestamp do not load next score page
                    //TODO: Break foreach as well
                    if (!activePlayer.AddScore(tmpScore)) continueLoad = false;
                }

                songSuggest.log?.WriteLine("Page " + page + "/" + Math.Ceiling((double)playerScoreCollection.metadata.total / 100) + " Done.");
                //Last Page check, sets loop to finish if on it.
                if (playerScoreCollection.metadata.total <= page * 100) continueLoad = false;
            }
            activePlayer.rankedPlayCount = activePlayer.scores.Count();

            //Save updated player
            activePlayer.Save();

            //If new songs was added, save the library.
            if (songLibrary.Updated) songLibrary.Save();

            songSuggest.log?.WriteLine("PlayerScores Count: " + activePlayer.scores.Count());
            songSuggest.activePlayer = activePlayer;
        }
    }
}
 
