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
using PlaylistNS;

namespace SongSuggestNS
{
    public class SongSuggest
    {
        public double songSuggestCompletion { get => songSuggest?.songSuggestCompletion ?? 1; }
        public static SongSuggest MainInstance { get; set; }
        public static TextWriter Log { get; set; }
        public String status { get; set; } = "Uninitialized";
        public ActivePlayer activePlayer { get; set; }
        //Default as an unset player. Place ID here and next RefreshActivePlayer() updates it.
        public String activePlayerID => CoreSettings.UserID;
        public FileHandler fileHandler { get; set; }
        public SongLibraryInstance songLibrary { get; set; }
        public WebDownloader webDownloader { get; set; }
        public FilesMeta filesMeta { get; set; }
        public SongLiking songLiking { get; set; }
        public SongBanning songBanning { get; set; }
        public Top10kPlayers scoreSaberScoreBoard { get; set; }
        public Top10kPlayers accSaberScoreBoard { get; set; }
        public Top10kPlayers beatLeaderScoreBoard { get; set; }

        private bool removeScoreSaberOnlyScoresFromBeatLeaderLeaderBoard = false;

        //Last used Song Evaluation is stored here if the UI wants to use them for further information than just creation of the playlists
        public RankedSongSuggest songSuggest { get; private set; } = null;
        public OldAndNew oldestSongs { get; private set; }

        //List of last ranked song suggestions
        public LastRankedSuggestions lastSuggestions { get; set; }

        //Boolean set to true if the quality of the found songs was not high enough
        //e.g. Had to remove the betterAcc and/or songs was missing from generating originSongsCount suggestions.
        public Boolean lowQualitySuggestions { get; set; } = false;

        //Log Details Target (null means it is off), else set the writer here.
        public TextWriter log => Log; //Been using non global log all over, just use global Log in rewrites.

        internal CoreSettings CoreSettings { get; }

        //Configured how the program should be run. Assigns first created as main.
        public SongSuggest(CoreSettings coreSettings)
        {
            CoreSettings = coreSettings;
            if (SongSuggest.MainInstance == null) SongSuggest.MainInstance = this;
            Initialize();
        }

        //Initialising based on the _coreSettings.
        private void Initialize()
        {
            //Enable Log
            Log = CoreSettings.Log;
            log?.WriteLine("Log Enabled in Constructor");

            fileHandler = new FileHandler { songSuggest = this, filePathSettings = CoreSettings.FilePathSettings };
            //Create any missing paths
            fileHandler.CreatePaths();

            webDownloader = new WebDownloader { songSuggest = this };

            filesMeta = fileHandler.LoadFilesMeta();

            status = "Checking loaded data for new Online Files";

            //Validate file versions and checks for new data.
            ValidateCacheFiles();

            //Load data from disk.

            //Load Song Library from File and make it the global lookup if this is the primary instance
            songLibrary = new SongLibraryInstance { songSuggest = this };
            if (SongSuggest.MainInstance == this) songLibrary.SetActive();
            songLibrary.SetLibrary(fileHandler.LoadSongLibrary());

            songLiking = new SongLiking
            {
                songSuggest = this,
                likedSongs = UpdateOldRecords(fileHandler.LoadLikedSongs())
            };

            songBanning = new SongBanning
            {
                songSuggest = this,
                bannedSongs = UpdateOldRecords(fileHandler.LoadBannedSongs())
            };

            lastSuggestions = new LastRankedSuggestions { songSuggest = this };
            lastSuggestions.Load();

            status = "Preparing Link Data";

            //Load Link Data
            scoreSaberScoreBoard = new Top10kPlayers { songSuggest = this };
            scoreSaberScoreBoard.FormatName = "Score Saber";
            scoreSaberScoreBoard.Load("Top10KPlayers");

            accSaberScoreBoard = new Top10kPlayers { songSuggest = this };
            accSaberScoreBoard.FormatName = "Acc Saber";
            accSaberScoreBoard.Load("AccSaberLeaderboardData");

            //Load the BeatLeader leaderboard if active.
            if (CoreSettings.UseBeatLeaderLeaderboard) LoadBeatLeaderLeaderBoard();

            status = "Preparing Players Song History";
            //Creates an empty active player object
            activePlayer = new ActivePlayer(activePlayerID, this);
            //Load player if assigned
            if (activePlayerID != "-1") activePlayer.Load();
            //update the assigned playerID
            RefreshActivePlayer();



            status = "Ready";
        }

        private List<SongBan> UpdateOldRecords(List<SongBan> songBans)
        {
            //Find the songID's without a hyphen and update them to internal (they are old ScoreSaber IDs)
            foreach (var songBan in songBans.Where(c => !c.songID.Contains("-")))
            {
                songBan.songID = SongLibrary.StringIDToSong(songBan.songID, SongIDType.ScoreSaber).internalID;
            }

            foreach (var songBan in songBans.Where(c => string.IsNullOrEmpty(c.songName)))
            {
                SongID songID = (InternalID)songBan.songID;
                songBan.songName = SongLibrary.GetDisplayName(songID);
            }

            return songBans;
        }

        private List<SongLike> UpdateOldRecords(List<SongLike> songLikes)
        {
            //Find the songID's without a hyphen and update them to internal (they are old ScoreSaber IDs)
            foreach (var songLike in songLikes.Where(c => !c.songID.Contains("-")))
            {
                songLike.songID = SongLibrary.StringIDToSong(songLike.songID, SongIDType.ScoreSaber).internalID;
            }

            foreach (var songLike in songLikes.Where(c => string.IsNullOrEmpty(c.songName)))
            {
                SongID songID = (InternalID)songLike.songID;
                songLike.songName = SongLibrary.GetDisplayName(songID);
            }
            return songLikes;
        }

        private void SetActiveLocations()
        {
            activePlayer.ActiveScoreLocations.Clear();
            if (CoreSettings.UseScoreSaberLeaderboard || CoreSettings.UseAccSaberLeaderboard) activePlayer.ActiveScoreLocations.Add(ScoreLocation.ScoreSaber);
            if (CoreSettings.UseBeatLeaderLeaderboard) activePlayer.ActiveScoreLocations.Add(ScoreLocation.BeatLeader);
        }

        //Validate CacheFiles and download new versions if available.
        private void ValidateCacheFiles()
        {
            //Try and perform the update, if it fails, ohh well we try next time. Suggestions quality do not drop with a missed update.
            try
            {
                //Perform ScoreSaber Updates if active
                //Performs all GIT updates including Song Library, so disabling this will also disable those checks.
                if (CoreSettings.UpdateScoreSaberLeaderboard)
                {
                    UpdateScoreSaberCacheFiles();
                }

                //Perform AccSaber Updates if active
                if (CoreSettings.UpdateAccSaberLeaderboard)
                {
                    UpdateAccSaberCacheFiles();
                }

                //Perform Beatleader Updates if active (May update the filesmeta version for its data if needed, so this is done after check with GIT data).
                if (CoreSettings.UpdateBeatLeaderLeaderboard)
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
            CoreSettings.UserID = settings.PlayerID;
            RefreshActivePlayer();

            //Create the Song Suggestion (so once the creation has been made additional information can be kept and loaded from it.
            songSuggest = new RankedSongSuggest
            {
                songSuggest = this,
                settings = settings,
            };
            songSuggest.SuggestedSongs();

            lastSuggestions.SetSuggestions(songSuggest.sortedSuggestions);

            status = "Ready";
        }

        public void ClearSongSuggestions()
        {
            songSuggest = null;
        }

        public void GenerateOldestSongs(OldAndNewSettings settings)
        {
            //Refresh Player Data
            CoreSettings.UserID = settings.scoreSaberID;
            RefreshActivePlayer();

            oldestSongs = new OldAndNew(this);
            oldestSongs.GeneratePlaylist(settings);
            status = "Ready";
        }

        public void ClearOldestSongs()
        {
            oldestSongs = null;
        }

        //Provide an accuracy of a 0 to 1 value, Sets a local score
        //**Implement Session Score to avoid having to refresh player during session once BeatLeader scores can be calculated**
        public void AddLocalScore(string hash, string difficulty, double accuracy)
        {
            log?.WriteLine($"hash: {hash} difficulty: {difficulty} accuracy {accuracy} received");
            SongID songID = songLibrary.GetID(hash, difficulty);
            AddLocalScore(songID, accuracy);
        }

        LocalPlayerScoreManager localScores => (LocalPlayerScoreManager)activePlayer.GetScoreLocation(ScoreLocation.LocalScores);

        public void AddLocalScore(SongID songID, double accuracy)
        {
            localScores.AddScore(songID, accuracy);
            if (localScores.updated) localScores.Save();
        }

        //Makes sure the active players data is updated (Load Cache, Reset Cache if needed, Download new data from web).
        //If playerID is -1 do nothing.
        public void RefreshActivePlayer()
        {
            if (activePlayerID == "-1") return;

            if (activePlayer.PlayerID != activePlayerID)
            {
                activePlayer = new ActivePlayer(activePlayerID, this);
                activePlayer.Load();
            }

            //Check if there is a localCache file without player ID, and if add the data to the current active players data.
            if (File.Exists(fileHandler.filePathSettings.activePlayerDataPath + $"Local Scores.json"))
                ((LocalPlayerScoreManager)activePlayer.GetScoreLocation(ScoreLocation.LocalScores)).ImportOldLocalScores();

            SetActiveLocations();
            log?.WriteLine($"Refreshing player: {activePlayerID}");
            activePlayer.Refresh();
            log?.WriteLine($"Refreshed player");
            //ActivePlayerRefreshData activePlayerRefreshData = new ActivePlayerRefreshData { songSuggest = this };
            //activePlayerRefreshData.RefreshActivePlayer();

            //if (_coreSettings.UseBeatLeaderLeaderboard) beatLeaderScores.Refresh();
        }

        //Support Functions for RankPlate
        //Get the placement of a specific song in last RankedSongSuggest, "" if not given a rank.
        public string GetSongRanking(string hash, string difficulty)
        {
            SongID songID = songLibrary.GetID(hash, difficulty);
            return lastSuggestions.GetRank(songID);
        }

        //Amount of linked songs in last RankedSongSuggest evaluation
        public string GetSongRankingCount()
        {
            return lastSuggestions.GetRankCount();
        }

        public string GetAPString(string hash, string difficulty)
        {
            var songID = songLibrary.GetID(hash, difficulty);

            //Add Local Scores if needed
            bool localScoresPresent = activePlayer.ActiveScoreLocations.Contains(ScoreLocation.LocalScores);
            if (!localScoresPresent) activePlayer.ActiveScoreLocations.Add(ScoreLocation.LocalScores);

            var AP = activePlayer.GetRatedScore(songID, LeaderboardType.AccSaber);

            //Remove local again if added.
            if (!localScoresPresent) activePlayer.ActiveScoreLocations.Remove(ScoreLocation.LocalScores);

            return AP == 0 ? "" : $"{AP:0.00}AP";


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

        //Clears the stored cache files
        public void ClearUser()
        {
            activePlayer.ClearCache();
        }

        public String SongCategoryText(String lookup)
        {
            if (!Translate.SongCategoryDictionary.ContainsKey(lookup)) return lookup;
            return Translate.SongCategoryDictionary[lookup];
        }

        public void UpdateScoreSaberCacheFiles()
        {
            //Stores if any format has been updated, and if saves the updated formats at the end
            bool filesUpdated = false;

            //Load current file structure version and generate a version for the expected versions.
            FileFormatVersions fileFormatDiskVersion = fileHandler.LoadFileFormatVersions();

            FileFormatVersions fileFormatExpectedVersion = new FileFormatVersions()
            {
                top10kVersion = Top10kPlayers.FormatVersion,
                songLibraryVersion = SongLibraryInstance.FormatVersion
            };

            //Compare web and file versioning
            FilesMeta diskVersion = filesMeta;
            FilesMeta cacheFilesWebVersion = webDownloader.GetFilesMeta();

            //As the web version does not know the date of other external sources, we set those values to the current known in the web cache (local dates are correct)
            //Note song library is downloaded, so its Songs updated is the correct time version of what is in it.
            cacheFilesWebVersion.beatLeaderLeaderboardUpdated = diskVersion.beatLeaderLeaderboardUpdated;

            //Perform checks if anything needs updating
            //PlayerCache needs update, if the format has changed, or there is a new major update of the 10k data.
            bool formatChange = fileFormatDiskVersion.activePlayerVersion != fileFormatExpectedVersion.activePlayerVersion;
            bool contentChange = diskVersion.Major(FilesMetaType.Top10kVersion) != cacheFilesWebVersion.Major(FilesMetaType.Top10kVersion);

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
                log?.WriteLine("Starting Download of Song Library");
                List<Song> songs = webDownloader.GetSongLibrary();
                fileHandler.SaveSongLibrary(songs);

                filesUpdated = true;
                log?.WriteLine("Downloaded and Updated Song Library");
            }

            //ScoreSaber Leaderboard is Checked
            //New top10k data needs to be downloaded in case of any change to content.
            formatChange = fileFormatDiskVersion.top10kVersion != fileFormatExpectedVersion.top10kVersion;
            contentChange = diskVersion.top10kVersion != cacheFilesWebVersion.top10kVersion;

            log?.WriteLine($"ScoreSaber Player Data File Version:  {fileFormatDiskVersion.top10kVersion} Expected: {fileFormatExpectedVersion.top10kVersion}");
            log?.WriteLine($"Scoresaber Player Data Cache Version: {diskVersion.top10kVersion}({diskVersion.top10kUpdated}) Web: {cacheFilesWebVersion.top10kVersion}(Timestamp: {cacheFilesWebVersion.top10kUpdated})");
            if (formatChange || contentChange)
            {
                log?.WriteLine("Starting Download of ScoreSaber Player Data");
                List<Top10kPlayer> top10kPlayerData = webDownloader.GetTop10kPlayers();
                fileHandler.SaveScoreBoard(top10kPlayerData, "Top10KPlayers");
                //top10kPlayers.Load();

                filesUpdated = true;
                log?.WriteLine("Downloaded and Updated ScoreSaber Player data");
            }

            //Save the new local data version if any updates has been completed. If anything fails next restart should attempt full update again.
            if (filesUpdated)
            {
                fileHandler.SaveFilesMeta(cacheFilesWebVersion);
                filesMeta = cacheFilesWebVersion;
                fileHandler.SaveFilesFormatVersions(fileFormatExpectedVersion);
            }
        }

        public void UpdateAccSaberCacheFiles()
        {
            //No online location yet.
        }

        //For now refresh request is manual.
        public void UpdateBeatLeaderCacheFiles()
        {
            log?.WriteLine("Starting to load BeatLeader data");

            //Get time of latest update to Leaderboard data.
            long lastUpdate = webDownloader.GetBeatLeaderLeaderboardUpdateTime();

            DateTime lastUpdateDateTime = DateTimeOffset.FromUnixTimeSeconds(lastUpdate).DateTime;
            log?.WriteLine($"Beat Leader Web Update Time. Unix: {lastUpdate} Time: {lastUpdateDateTime:d}");

            //Updates the leaderboard if needed.
            if (filesMeta.beatLeaderLeaderboardUpdated < lastUpdate)
            {
                log?.WriteLine("Pulling new Leaderboard data");
                RefreshBeatLeaderLeaderBoard();
                filesMeta.beatLeaderLeaderboardUpdated = lastUpdate;
            }
        }

        private void LoadBeatLeaderLeaderBoard()
        {
            //Loads player and scoreboard data, as well as updates the SongLibrary if changes was made.

            log?.WriteLine($"Songs Updated: {filesMeta.beatLeaderSongsUpdated} Leaderboard Updated: {filesMeta.beatLeaderLeaderboardUpdated} ");

            //Update Song Library to match Scoreboard if needed.
            if (filesMeta.beatLeaderSongsUpdated != filesMeta.beatLeaderLeaderboardUpdated)
            {
                //Clear Song Library of current BeatLeader songs
                var currentSongs = songLibrary.GetAllRankedSongIDs(SongCategory.BeatLeader).Select(c => c.GetSong());
                foreach (Song song in currentSongs)
                {
                    songLibrary.RemoveSongCategory(song, SongCategory.BeatLeader);
                    song.starBeatLeader = 0;
                    song.starAccBeatLeader = 0;
                    song.starPassBeatLeader = 0;
                    song.starTechBeatLeader = 0;
                    song.beatLeaderID = null;
                }

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

            //Load the Leaderboard
            var leaderboard = new Top10kPlayers();
            leaderboard.FormatName = "Beat Leader";
            leaderboard.songSuggest = this;
            leaderboard.Load("BeatLeaderLeaderboard");
            beatLeaderScoreBoard = leaderboard;

            //If the loaderboard still needs the onetime filtering perform this.
            if (removeScoreSaberOnlyScoresFromBeatLeaderLeaderBoard) RemoveScoreSaberOnlyScoresFromBeatLeaderLeaderBoard();

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

            //We cannot perform these action until we have loaded the Song Library, which is after we update the files, so we mark this for later when we can do this.
            if (CoreSettings.FilterScoreSaberBiasInBeatLeader) removeScoreSaberOnlyScoresFromBeatLeaderLeaderBoard = true;

            //Save the pruned file
            fileHandler.SaveScoreBoard(beatLeaderPlayerList, "BeatLeaderLeaderboard");
        }

        //Remove Players with ScoreSaberOnly Scores on BL. This is unlikely at "random" play (1-10% chance depending on how you look at it) so we remove
        //a few player samples to get rid of a lot of biased samples. This is activated via the core settings, on per default unless UI overwrites this.
        private void RemoveScoreSaberOnlyScoresFromBeatLeaderLeaderBoard()
        {
            var scoreSaberSongIDs = SongLibrary.GetAllRankedSongIDs(SongCategory.ScoreSaber);

            List<Top10kPlayer> removePlayers = new List<Top10kPlayer>();

            //We populate the list of players with top scores that are all also ScoreSaber songs.
            foreach (var player in beatLeaderScoreBoard.top10kPlayers)
            {
                int nonScoreSaberCount = player.top10kScore.Select(c => (BeatLeaderID)c.songID).Except(scoreSaberSongIDs).ToList().Count();
                if (nonScoreSaberCount == 0) removePlayers.Add(player);
            }

            //We remove these songs and save the updated leaderboard
            beatLeaderScoreBoard.top10kPlayers = beatLeaderScoreBoard.top10kPlayers.Except(removePlayers).ToList();
            fileHandler.SaveScoreBoard(beatLeaderScoreBoard.top10kPlayers, "BeatLeaderLeaderboard");

            //We save the leaderboard.
            beatLeaderScoreBoard.top10kSongMeta.Clear();
            beatLeaderScoreBoard.GenerateTop10kSongMeta();
        }

        public void FilterSyncURL(PlaylistPath loadPath, PlaylistPath savePath)
        {
            var playlistManager = new PlaylistManager(loadPath);
            PlaylistSyncURL url = new PlaylistSyncURL() { SyncURL = playlistManager.syncURL };
            status = "Dowloading Songs";
            playlistManager.LoadSyncURL(url);

            status = "Filtering Songs";
            log?.WriteLine($"Songs Before Filtering: {playlistManager.GetSongs().Count}");

            var bannedSongs = playlistManager.GetSongs()
                .Where(c => songBanning.IsBanned(c))
                .ToList();

            foreach (var song in bannedSongs) playlistManager.RemoveSong(song);

            log?.WriteLine($"Songs After Filtering: {playlistManager.GetSongs().Count}");

            playlistManager.SetFilePath(savePath);
            playlistManager.Generate();

            status = "Ready";
        }

    }
}
