using SongLibraryNS;
using ActivePlayerData;
using FileHandling;
using WebDownloading;
using BanLike;
using System;
using Actions;
using Settings;
using LinkedData;
using System.IO;
using System.Collections.Generic;
using Data;
using PlayerScores;
using System.Linq;

namespace SongSuggestNS
{
    public class SongSuggest
    {
        public String status { get; set; } = "Uninitialized";
        public ActivePlayer activePlayer { get; set; }
        //Default as an unset player. Dump ID here and next RefreshActivePlayer() updates it.
        public String activePlayerID { get; set; } = "-1";
        public FileHandler fileHandler { get; set; }
        public SongLibraryInstance songLibrary { get; set; }
        public WebDownloader webDownloader { get; set; }
        public FilesMeta filesMeta { get; set; }
        public SongLiking songLiking { get; set; }
        public SongBanning songBanning { get; set; }
        public Top10kPlayers scoreSaberScoreBoard { get; set; }
        public Top10kPlayers accSaberScoreBoard { get; set; }
        public Top10kPlayers beatLeaderScoreBoard { get; set; }
        public LocalPlayerScoreManager localScores { get; set; }
        public BeatLeaderPlayerScoreManager beatLeaderScores { get; set; }

        //Last used Song Evaluation is stored here if the UI wants to use them for further information than just creation of the playlists
        public RankedSongSuggest songSuggest { get; private set; } = null;
        public OldAndNew oldestSongs { get; private set; }

        //List of last ranked song suggestions
        public LastRankedSuggestions lastSuggestions { get; set; }

        //Boolean set to true if the quality of the found songs was not high enough
        //e.g. Had to remove the betterAcc and/or songs was missing from generating originSongsCount suggestions.
        public Boolean lowQualitySuggestions { get; set; } = false;

        //Log Details Target (null means it is off), else set the writer here.
        public TextWriter log = null;

        private CoreSettings _coreSettings;

        //Configured how the program should be run.
        public SongSuggest(CoreSettings coreSettings)
        {
            _coreSettings = coreSettings;
            Initialize();
        }

        //Initialising based on the _coreSettings.
        private void Initialize()
        {
            //Enable Log
            this.log = _coreSettings.Log;
            log?.WriteLine("Log Enabled in Constructor");

            //Set the active players ID
            activePlayerID = _coreSettings.UserID;

            fileHandler = new FileHandler { songSuggest = this, filePathSettings = _coreSettings.FilePathSettings };
            
            webDownloader = new WebDownloader { songSuggest = this };

            filesMeta = fileHandler.LoadFilesMeta();

            //Validate file versions and checks for new data.
            ValidateCacheFiles();

            //Load data from disk.

            //Load Song Library from File and make it the global lookup
            songLibrary = new SongLibraryInstance { songSuggest = this };
            songLibrary.SetLibrary(fileHandler.LoadSongLibrary());
            songLibrary.SetActive();

            songLiking = new SongLiking
            {
                songSuggest = this,
                likedSongs = fileHandler.LoadLikedSongs()
            };

            songBanning = new SongBanning
            {
                songSuggest = this,
                bannedSongs = fileHandler.LoadBannedSongs()
            };

            status = "Preparing Players Song History";
            //Creates an empty active player object
            activePlayer = new ActivePlayer();
            lastSuggestions = new LastRankedSuggestions { songSuggest = this };
            lastSuggestions.Load();

            localScores = new LocalPlayerScoreManager()
            {
                songSuggest = this
            };
            localScores.Load();

            status = "Preparing Link Data";
            //Load Link Data
            scoreSaberScoreBoard = new Top10kPlayers { songSuggest = this };
            scoreSaberScoreBoard.FormatName = "Score Saber";
            scoreSaberScoreBoard.Load("Top10KPlayers");
           // UpdateIDLinks(scoreSaberScoreBoard, "");

            accSaberScoreBoard = new Top10kPlayers { songSuggest = this };
            accSaberScoreBoard.FormatName = "Acc Saber";
            accSaberScoreBoard.Load("AccSaberLeaderboardData");
            //UpdateIDLinks(accSaberScoreBoard);

            //Load the BeatLeader leaderboard if active.
            if (_coreSettings.UseBeatLeaderLeaderboard) LoadBeatLeaderLeaderBoard();

            status = "Checking loaded data for new Online Files";

            status = "Ready";
        }

        //Validate CacheFiles and download new versions if available.
        private void ValidateCacheFiles()
        {
            //Try and perform the update, if it fails, ohh well we try next time. Suggestions quality do not drop with a missed update.
            try
            {

                //Stores if any format has been updated, and if saves the updated formats at the end
                bool filesUpdated = false;

                //Load current file structure version and generate a version for the expected versions.
                FileFormatVersions fileFormatDiskVersion = fileHandler.LoadFileFormatVersions();

                FileFormatVersions fileFormatExpectedVersion = new FileFormatVersions()
                {
                    top10kVersion = Top10kPlayers.FormatVersion,
                    activePlayerVersion = ActivePlayer.FormatVersion,
                    songLibraryVersion = SongLibraryInstance.FormatVersion
                };

                //Compare web and file versioning
                FilesMeta diskVersion = filesMeta;
                FilesMeta cacheFilesWebVersion = webDownloader.GetFilesMeta();

                //As the web version does not know the date of other external sources, we set those values to the current known in the web cache (local dates are correct)
                //Note song library is downloaded, so its Songs updated is the correct time version of what is in it.
                cacheFilesWebVersion.beatLeaderLeaderboardUpdated = diskVersion.beatLeaderLeaderboardUpdated;

                log?.WriteLine($"Version Current: {diskVersion.top10kVersion} Timestamp: {diskVersion.top10kUpdated}");
                log?.WriteLine($"Version Web:     {cacheFilesWebVersion.top10kVersion} Timestamp: {cacheFilesWebVersion.top10kUpdated}");

                //Perform checks if anything needs updating
                //PlayerCache needs update, if the format has changed, or there is a new major update of the 10k data.
                bool formatChange = fileFormatDiskVersion.activePlayerVersion != fileFormatExpectedVersion.activePlayerVersion;
                bool contentChange = diskVersion.Major(FilesMetaType.Top10kVersion) != cacheFilesWebVersion.Major(FilesMetaType.Top10kVersion);
                if (formatChange || contentChange)
                {
                    if (!fileHandler.CheckPlayerRefresh()) fileHandler.TogglePlayerRefresh();
                    filesUpdated = true;
                    log?.WriteLine("Marked Playerdata for Refresh");
                }

                //SongLibrary is checked
                //All Song library data changes are major. (New Songs/Reweights).
                //All songs are expected to be up to date after an update, so there are no incremental, new songs only.
                formatChange = fileFormatDiskVersion.songLibraryVersion != fileFormatExpectedVersion.songLibraryVersion;
                log?.WriteLine($"Songlibrary Disk Content: {diskVersion.songLibraryVersion} Major: {diskVersion.Major(FilesMetaType.SongLibraryVersion)}");
                log?.WriteLine($"Songlibrary Web Content:  {cacheFilesWebVersion.songLibraryVersion} Major: {cacheFilesWebVersion.Major(FilesMetaType.SongLibraryVersion)}");

                contentChange = diskVersion.Major(FilesMetaType.SongLibraryVersion) != cacheFilesWebVersion.Major(FilesMetaType.SongLibraryVersion);
                log?.WriteLine($"Song Format  - changed? {formatChange}");
                log?.WriteLine($"Song Content - Disk: {diskVersion.Major(FilesMetaType.SongLibraryVersion)} Web: {cacheFilesWebVersion.Major(FilesMetaType.SongLibraryVersion)} {contentChange} Changed: {contentChange} ");
                if (formatChange || contentChange)
                {
                    List<Song> songs = webDownloader.GetSongLibrary();
                    fileHandler.SaveSongLibrary(songs);

                    filesUpdated = true;
                    log?.WriteLine("Downloaded and Updated Song Library");
                }

                //ScoreSaber Leaderboard is Checked
                //New top10k data needs to be downloaded in case of any change to content.
                formatChange = fileFormatDiskVersion.top10kVersion != fileFormatExpectedVersion.top10kVersion;
                contentChange = diskVersion.top10kVersion != cacheFilesWebVersion.top10kVersion;
                if (formatChange || contentChange)
                {
                    List<Top10kPlayer> top10kPlayerData = webDownloader.GetTop10kPlayers();
                    fileHandler.SaveScoreBoard(top10kPlayerData, "Top10KPlayers");
                    //top10kPlayers.Load();

                    filesUpdated = true;
                    log?.WriteLine("Downloaded and Updated top10k data");
                }

                //Save the new local data version if any updates has been completed. If anything fails next restart should attempt full update again.
                if (filesUpdated)
                {
                    fileHandler.SaveFilesMeta(cacheFilesWebVersion);
                    filesMeta = cacheFilesWebVersion;
                    fileHandler.SaveFilesFormatVersions(fileFormatExpectedVersion);
                }

                //Perform Beatleader Updates if active (May update the filesmeta version for its data if needed, so this is done after check with GIT data).
                if (_coreSettings.UpdateBeatLeaderLeaderboard)
                {
                    UpdateBeatLeaderCacheFiles();
                }
            }
            catch
            {
            }
        }

        public void GenerateSongSuggestions(SongSuggestSettings settings)
        {
            //Refresh Player Data (Skip if test user ID -100)
            activePlayerID = settings.ScoreSaberID;
            if (activePlayerID != "-100") RefreshActivePlayer();

            //Create the Song Suggestion (so once the creation has been made additional information can be kept and loaded from it.
            songSuggest = new RankedSongSuggest
            {
                songSuggest = this,
                settings = settings,
            };
            songSuggest.SuggestedSongs();

            //Update nameplate rankings, and save them.
            lastSuggestions.lastSuggestions = SongLibrary
                .SongIDToSong(songSuggest.sortedSuggestions)
                .Where(c => !string.IsNullOrEmpty(c.scoreSaberID))
                .Select(c => c.scoreSaberID)
                .ToList();
            lastSuggestions.Save();

            status = "Ready";
        }

        //Requires a RankedSongsSuggest has been performed, then it evaluates the linked songs without updating the user via new settings.
        //**Consider checks for updates of user, and that RankedSongsSuggest has already been performed**
        public void Recalculate(SongSuggestSettings settings)
        {
            if (songSuggest == null) return;
            songSuggest.settings = settings;
            songSuggest.Recalculate();
        }

        public void ClearSongSuggestions()
        {
            songSuggest = null;
        }

        public void GenerateOldestSongs(OldAndNewSettings settings)
        {
            //Refresh Player Data
            activePlayerID = settings.scoreSaberID;
            RefreshActivePlayer();

            oldestSongs = new OldAndNew(this);
            oldestSongs.GeneratePlaylist(settings);
            status = "Ready";
        }

        public void ClearOldestSongs()
        {
            oldestSongs = null;
        }

        //Provide an accuracy of a 0 to 1 value
        public void AddLocalScore(string hash, string difficulty, double accuracy)
        {
            log?.WriteLine($"hash: {hash} difficulty: {difficulty} accuracy {accuracy} received");
            string songID = songLibrary.GetID(hash, difficulty);
            AddLocalScore(songID, accuracy);
        }

        public void AddLocalScore(string songID, double accuracy)
        {
            log?.WriteLine($"songID {songID} accuracy {accuracy} received");
            log?.WriteLine($"before score added");
            localScores.AddScore(songID, accuracy);
            log?.WriteLine($"after score added");
            log?.WriteLine($"Save Updated? {localScores.updated}");
            if (localScores.updated) localScores.Save();
            log?.WriteLine($"after update");
        }

        //Makes sure the active players data is updated (Load Cache, Reset Cache if needed, Download new data from web).
        public void RefreshActivePlayer()
        {
            ActivePlayerRefreshData activePlayerRefreshData = new ActivePlayerRefreshData { songSuggest = this };
            activePlayerRefreshData.RefreshActivePlayer();
        }

        //Get the placement of a specific song in last RankedSongSuggest, "" if not given a rank.
        public String GetSongRanking(String hash, String difficulty)
        {
            return lastSuggestions.GetRank(hash, difficulty);
        }

        //Amount of linked songs in last RankedSongSuggest evaluation
        public String GetSongRankingCount()
        {
            return lastSuggestions.GetRankCount();
        }

        //Clear all Banned Songs
        public void ClearBan()
        {
            songBanning = new SongBanning()
            {
                songSuggest = this
            };
            fileHandler.SaveBannedSongs(songBanning.bannedSongs);
        }

        //Clears liked Songs
        public void ClearLiked()
        {
            songLiking = new SongLiking()
            {
                songSuggest = this
            };
            fileHandler.SaveLikedSongs(songLiking.likedSongs);
        }

        //Sets a reminder for next RefreshActivePlayer() to perform a full reload of the users data.
        public void ClearUser()
        {
            //Set Reminder file if missing (Can be set to true multiple times, hence check if already set)
            if (!fileHandler.CheckPlayerRefresh()) fileHandler.TogglePlayerRefresh();
        }

        public String SongCategoryText(String lookup)
        {
            if (!Translate.SongCategoryDictionary.ContainsKey(lookup)) return lookup;
            return Translate.SongCategoryDictionary[lookup];
        }

        //For now refresh request is manual.
        public void UpdateBeatLeaderCacheFiles()
        {
            log?.WriteLine("Starting to load BeatLeader data");

            //Get time of latest update to Leaderboard data.
            long lastUpdate = webDownloader.GetBeatLeaderLeaderboardUpdateTime();

            DateTime lastUpdateDateTime = DateTimeOffset.FromUnixTimeSeconds(lastUpdate).DateTime;
            log?.WriteLine($"Leaderboard Web Update Time. Unix: {lastUpdate} Time: {lastUpdateDateTime:d}");

            //Updates the leaderboard if needed.
            if (_coreSettings.UpdateBeatLeaderLeaderboard && filesMeta.beatLeaderLeaderboardUpdated < lastUpdate)
            {
                log?.WriteLine("Pulling new Leaderboard data");
                RefreshBeatLeaderLeaderBoard();
                filesMeta.beatLeaderLeaderboardUpdated = lastUpdate;
            }
        }

        private void LoadBeatLeaderLeaderBoard()
        {
            //Loads player and scoreboard data, as well as updates the SongLibrary if changes was made.
            if (_coreSettings.UseBeatLeaderLeaderboard)
            {
                //Update Song Library to match Scoreboard if needed.
                if (filesMeta.beatLeaderSongsUpdated != filesMeta.beatLeaderLeaderboardUpdated)
                {
                    //Clear leaderboard of current BeatLeader songs
                    var currentSongs = songLibrary.GetAllRankedSongIDs(SongCategory.BeatLeader).Select(c => c.GetSong());
                    foreach (Song song in currentSongs)
                    {
                        songLibrary.RemoveSongCategory(song, SongCategory.BeatLeader);
                        song.starBeatLeader = 0;
                        song.beatLeaderID = null;
                    }

                    //Have Song Library unlink songs
                    songLibrary.RemoveSongsWithoutSongCategories();

                    var songs = webDownloader.GetBeatLeaderRankedSongs(filesMeta.beatLeaderLeaderboardUpdated);
                    var standardSongs = songs
                        .Where(c => c.mode == "Standard")
                        .ToList();

                    foreach (var song in standardSongs)
                    {
                        songLibrary.UpsertSong(song);
                    }
                    //Record the update time and save the file to disk.
                    songLibrary.Save();
                    filesMeta.beatLeaderSongsUpdated = filesMeta.beatLeaderLeaderboardUpdated;
                    fileHandler.SaveFilesMeta(filesMeta);
                }

                //Load Player Scorefile
                beatLeaderScores = new BeatLeaderPlayerScoreManager();
                beatLeaderScores.songSuggest = this;
                beatLeaderScores.Load();
                beatLeaderScores.Refresh();

                //Load the Leaderboard
                var leaderboard = new Top10kPlayers();
                leaderboard.FormatName = "Beat Leader";
                leaderboard.songSuggest = this;
                leaderboard.Load("BeatLeaderLeaderboard");
                beatLeaderScoreBoard = leaderboard;
            }
        }

        //Downloads newest Leaderboard
        private void RefreshBeatLeaderLeaderBoard()
        {
            var beatLeaderPlayerList = webDownloader.GetBeatLeaderLeaderboard();

            //Pruning of recieved data. Hopefully this is temporary and filtering can be moved to server side.
            //Removal of mismatching players
            log?.WriteLine($"--- Check For Large Spread ---");
            int totalSpread = 0;
            var toRemove = new List<Top10kPlayer>();

            foreach (var player in beatLeaderPlayerList)
            {
                double diff = player.top10kScore.Last().pp / player.top10kScore.First().pp;
                if (diff < 0.7)
                {
                    totalSpread++;
                    Console.WriteLine($"Player:{player.name}({player.id}) Rank: {player.rank} Diff: {diff}");
                    toRemove.Add(player);
                }
            }

            log?.WriteLine($"Large Spread: {totalSpread}");
            log?.WriteLine();

            log?.WriteLine($"--- Check For Few Scores ---");
            int fewScores = 0;
            long targetPoint = ((DateTimeOffset)DateTime.UtcNow.AddYears(-1)).ToUnixTimeSeconds();
            foreach (var player in beatLeaderPlayerList)
            {
                if (player.top10kScore.Count < 20)
                {
                    fewScores++;
                    Console.WriteLine($"Player:{player.name}({player.id}) Rank: {player.rank} Scores: {player.top10kScore.Count}");
                    toRemove.Add(player);
                }
            }
            log?.WriteLine($"Few Scores: {fewScores}");

            log?.WriteLine();

            beatLeaderPlayerList = beatLeaderPlayerList.Except(toRemove).ToList();

            //Save the pruned file
            fileHandler.SaveScoreBoard(beatLeaderPlayerList, "BeatLeaderLeaderboard");
        }
    }
}
