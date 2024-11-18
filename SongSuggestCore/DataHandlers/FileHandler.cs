using System;
using SongLibraryNS;
using System.IO;
using ActivePlayerData;
using Newtonsoft.Json;
using LinkedData;
using BanLike;
using System.Collections.Generic;
using Data;
using SongSuggestNS;
using Settings;
using PlaylistJson;
using PlayerScores;
using AccSaberData;
using Newtonsoft.Json.Linq;
using Actions;

namespace FileHandling
{
    public class FileHandler
    {
        private JsonSerializerSettings serializerSettings = new JsonSerializerSettings 
        { 
            NullValueHandling = NullValueHandling.Ignore, 
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        public SongSuggest songSuggest { get; set; }
        public FilePathSettings filePathSettings { get; set; }

        public void CreatePaths()
        {
            if (filePathSettings == null) throw new Exception("No FilePathSettings given");
            foreach (var pathString in filePathSettings.GetAllPaths())
            {
                if (!Directory.Exists(pathString))
                {
                    Directory.CreateDirectory(pathString);
                }
            }
        }

        public void SaveOldAndNewRequest(OldAndNewSettings settings)
        {
            File.WriteAllText(filePathSettings.BasePath + "OldAndNewRequest.json", JsonConvert.SerializeObject(settings, serializerSettings));
        }

        public void SaveSongSuggestRequest(SongSuggestSettings settings)
        {
            File.WriteAllText(filePathSettings.BasePath + "SongSuggestRequest.json", JsonConvert.SerializeObject(settings, serializerSettings));
        }


        //Loads the primary song library from disc
        public List<Song> LoadSongLibrary()
        {
            if (!File.Exists(filePathSettings.songLibraryPath + "SongLibrary.json")) SaveSongLibrary(new List<Song>());
            String songLibraryJSON = File.ReadAllText(filePathSettings.songLibraryPath + "SongLibrary.json");
            return JsonConvert.DeserializeObject<List<Song>>(songLibraryJSON, serializerSettings);
        }

        //Save the known Songs in the Library
        public void SaveSongLibrary(List<Song> songLibrary)
        {
            File.WriteAllText(filePathSettings.songLibraryPath + "SongLibrary.json", JsonConvert.SerializeObject(songLibrary,serializerSettings));
        }

        //Save a Playlist to a bplist (json format)
        public void SavePlaylist(JObject jObject, String fileName)
        {
            string playlistString = JsonConvert.SerializeObject(jObject, serializerSettings);
            File.WriteAllText(filePathSettings.playlistPath + fileName, playlistString);
        }

        //Load a playlist (bplist or json), must include file extension. Path is from the Playlist Library location. (combined Folder\filename.extension)
        public JObject LoadPlaylist(String fileName)
        {
            string fullPath = Path.Combine(filePathSettings.playlistPath, fileName);
            if (!File.Exists(fullPath)) SavePlaylist(new JObject(), fileName);
            String playlistString = File.ReadAllText(fullPath);
            return JsonConvert.DeserializeObject<JObject>(playlistString, serializerSettings);
        }

        //save Playlist json
        public void SavePlaylist(Playlist playlist, String fileName)
        {
            string fullPath = Path.Combine(filePathSettings.playlistPath, fileName);

            string directoryPath = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllText(fullPath, JsonConvert.SerializeObject(playlist, serializerSettings));
        }

        //Load Players Cached Scores
        public ScoreCollection LoadScoreCollection(string filename)
        {
            if (!File.Exists(filePathSettings.activePlayerDataPath + $"{filename}.json")) SaveScoreCollection(new ScoreCollection(), filename);
            String loadedString = File.ReadAllText(filePathSettings.activePlayerDataPath + $"{filename}.json");
            //Collections may be in old format, if that is the case replace them with an empty one.            
            try
            {
                return JsonConvert.DeserializeObject<ScoreCollection>(loadedString, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine($"Failed to convert: {filePathSettings.activePlayerDataPath}{filename}. Creating new");
                SaveScoreCollection(new ScoreCollection(), filename);
                return new ScoreCollection();
            }
        }

        //Save Players Cached Scores
        public void SaveScoreCollection(ScoreCollection playerScores, string filename)
        {
            File.WriteAllText(filePathSettings.activePlayerDataPath + $"{filename}.json", JsonConvert.SerializeObject(playerScores));
        }

        //Load Old Format of Local Scores
        public List<PlayerScore> LoadOldLocalScores()
        {
            String loadedString = File.ReadAllText(filePathSettings.activePlayerDataPath + "Local Scores.json");
            return JsonConvert.DeserializeObject<List<PlayerScore>>(loadedString, serializerSettings);
        }

        public void RemoveOldLocalScores()
        {
            File.Delete(filePathSettings.activePlayerDataPath + "Local Scores.json");
        }

        public List<Top10kPlayer> LoadScoreBoard(string scoreBoardName)
        {
            string fileName = $"{filePathSettings.likedSongsPath}{scoreBoardName}.json";
            if (!File.Exists(fileName)) SaveScoreBoard(new List<Top10kPlayer>(), scoreBoardName);
            String linkPlayerJSON = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<List<Top10kPlayer>>(linkPlayerJSON, serializerSettings);
        }

        public void SaveScoreBoard(List<Top10kPlayer> players, string scoreBoardName)
        {
            string fileName = $"{filePathSettings.likedSongsPath}{scoreBoardName}.json";
            File.WriteAllText(fileName, JsonConvert.SerializeObject(players));
        }

        public List<T> LoadScoreBoard<T>(string scoreBoardName) where T : Top10kPlayer, new()
        {
            string fileName = $"{filePathSettings.likedSongsPath}{scoreBoardName}.json";

            if (!File.Exists(fileName))
            {
                SaveScoreBoard(new List<T>(), scoreBoardName);
            }

            string leaderboardJson = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<List<T>>(leaderboardJson, serializerSettings);
        }

        public void SaveScoreBoard<T>(List<T> scoreBoard, string scoreBoardName)
        {
            string fileName = $"{filePathSettings.likedSongsPath}{scoreBoardName}.json";
            File.WriteAllText(fileName, JsonConvert.SerializeObject(scoreBoard, serializerSettings));
        }

        public Boolean LinkedDataExist()
        {
            return File.Exists(filePathSettings.top10kPlayersPath + "Top10KPlayers.json");
        }

        public List<SongLike> LoadLikedSongs()
        {
            if (!File.Exists(filePathSettings.likedSongsPath +"Liked Songs.json")) SaveLikedSongs(new List<SongLike>());
            String likedSongsString = File.ReadAllText(filePathSettings.likedSongsPath + "Liked Songs.json");
            return JsonConvert.DeserializeObject<List<SongLike>>(likedSongsString, serializerSettings);
        }

        public void SaveLikedSongs(List<SongLike> songLiking)
        {
            File.WriteAllText(filePathSettings.likedSongsPath + "Liked Songs.json", JsonConvert.SerializeObject(songLiking));
        }

        public List<SongBan> LoadBannedSongs()
        {
            if (!File.Exists(filePathSettings.bannedSongsPath +"Banned Songs.json")) SaveBannedSongs(new List<SongBan>());
            String bannedSongsString = File.ReadAllText(filePathSettings.bannedSongsPath + "Banned Songs.json");
            return JsonConvert.DeserializeObject<List<SongBan>>(bannedSongsString, serializerSettings);            
        }

        public void SaveBannedSongs(List<SongBan> songBans)
        {
            File.WriteAllText(filePathSettings.bannedSongsPath + "Banned Songs.json", JsonConvert.SerializeObject(songBans));
        }

        public FilesMeta LoadFilesMeta()
        {
            if (!File.Exists(filePathSettings.filesDataPath + "Files.meta")) SaveFilesMeta(new FilesMeta());
            String filesDataString = File.ReadAllText(filePathSettings.filesDataPath + "Files.meta");
            return JsonConvert.DeserializeObject<FilesMeta>(filesDataString, serializerSettings);
        }

        public void SaveFilesMeta(FilesMeta filesData)
        {
            File.WriteAllText(filePathSettings.filesDataPath + "Files.meta", JsonConvert.SerializeObject(filesData));
        }
        public List<String> LoadRankedSuggestions()
        {
            if (!File.Exists(filePathSettings.lastSuggestionsPath + "LastSuggestions.json")) SaveRankedSuggestions(new List<String>());
            String filesDataString = File.ReadAllText(filePathSettings.lastSuggestionsPath + "LastSuggestions.json");
            return JsonConvert.DeserializeObject<List<String>>(filesDataString, serializerSettings);
        }

        public void SaveRankedSuggestions(List<String> rankedSuggestions)
        {
            File.WriteAllText(filePathSettings.lastSuggestionsPath + "LastSuggestions.json", JsonConvert.SerializeObject(rankedSuggestions));
        }

        public List<String> LoadAllRankedSongs()
        {
            if (!File.Exists(filePathSettings.rankedData + "allrankedsongs.json")) SaveAllRankedSongs(new List<String>());
            String fileDataString = File.ReadAllText(filePathSettings.rankedData + "allrankedsongs.json");
            return JsonConvert.DeserializeObject<List<String>>(fileDataString, serializerSettings);
        }

        public void SaveAllRankedSongs(List<String> allRankedSongs)
        {
            File.WriteAllText(filePathSettings.rankedData + "allrankedsongs.json", JsonConvert.SerializeObject(allRankedSongs));
        }

        public FileFormatVersions LoadFileFormatVersions()
        {
            if (!File.Exists(filePathSettings.filesDataPath + "FileFormatVersions.json")) SaveFilesFormatVersions(new FileFormatVersions());
            String fileDataString = File.ReadAllText(filePathSettings.filesDataPath + "FileFormatVersions.json");
            return JsonConvert.DeserializeObject<FileFormatVersions>(fileDataString, serializerSettings);
        }

        public void SaveFilesFormatVersions(FileFormatVersions versions)
        {
            File.WriteAllText(filePathSettings.filesDataPath + "FileFormatVersions.json", JsonConvert.SerializeObject(versions));
        }

        internal void SaveAccSaberSongs(List<SongData> songs)
        {
            File.WriteAllText(filePathSettings.rankedData + "AccSaberRaw.json", JsonConvert.SerializeObject(songs));
        }

        public List<SongData> LoadAccSaberSongs()
        {
            if (!File.Exists(filePathSettings.rankedData + "AccSaberRaw.json")) SaveAccSaberSongs(new List<SongData>());
            String fileDataString = File.ReadAllText(filePathSettings.rankedData + "AccSaberRaw.json");
            return JsonConvert.DeserializeObject<List<SongData>>(fileDataString, serializerSettings);
        }
    }
}