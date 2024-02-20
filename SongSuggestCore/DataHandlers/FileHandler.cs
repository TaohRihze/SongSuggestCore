﻿using System;
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

namespace FileHandling
{
    public class FileHandler
    {
        private JsonSerializerSettings serializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore };

        public SongSuggest songSuggest { get; set; }
        public FilePathSettings filePathSettings { get; set; }

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
        public void SavePlaylist(String playlistString, String fileName)
        {
            File.WriteAllText(filePathSettings.playlistPath + fileName + ".bplist", playlistString);
        }

        //Load a bplist playlist (json format)
        public Playlist LoadPlaylist(String fileName)
        {
            if (!File.Exists(filePathSettings.playlistPath + fileName + ".bplist")) SavePlaylist(new Playlist(), fileName);
            String playlistString = File.ReadAllText(filePathSettings.playlistPath + fileName + ".bplist");
            return JsonConvert.DeserializeObject<Playlist>(playlistString, serializerSettings);
        }

        //save Playlist json
        public void SavePlaylist(Playlist playlist, String fileName)
        {
            File.WriteAllText(filePathSettings.playlistPath + fileName + ".bplist", JsonConvert.SerializeObject(playlist, serializerSettings));
        }

        ////Load Active Players Data
        //public ActivePlayer LoadActivePlayer()
        //{
        //    string activePlayer = songSuggest.activePlayerID;
        //    if (!File.Exists(filePathSettings.activePlayerDataPath + activePlayer + ".json")) SaveActivePlayer(new ActivePlayer());
        //    String activePlayerString = File.ReadAllText(filePathSettings.activePlayerDataPath + activePlayer + ".json");
        //    return JsonConvert.DeserializeObject<ActivePlayer>(activePlayerString, serializerSettings);
        //}

        ////Save Active Players Data
        //public void SaveActivePlayer(ActivePlayer scoreSaberScores)
        //{
        //    string activePlayer = songSuggest.activePlayerID;
        //    File.WriteAllText(filePathSettings.activePlayerDataPath + $"{activePlayer}.json", JsonConvert.SerializeObject(scoreSaberScores));
        //}

        ////Load Active Players Local Scores
        //public List<LocalPlayerScore> LoadLocalScores()
        //{
        //    if (!File.Exists(filePathSettings.activePlayerDataPath + "Local Scores.json")) SaveLocalScores(new List<LocalPlayerScore>());
        //    String loadedString = File.ReadAllText(filePathSettings.activePlayerDataPath + "Local Scores.json");
        //    return JsonConvert.DeserializeObject<List<LocalPlayerScore>>(loadedString, serializerSettings);
        //}

        ////Save Active Players Local Scores
        //public void SaveLocalScores(List<LocalPlayerScore> localScores)
        //{
        //    File.WriteAllText(filePathSettings.activePlayerDataPath + "Local Scores.json", JsonConvert.SerializeObject(localScores));
        //}

        ////Load Active Players ScoreSaber scores
        //public LocalScores LoadScoresCollection()
        //{
        //    return LoadScoresCollection(songSuggest.activePlayer.PlayerID);
        //}

        ////Save Active Players ScoreSaber Scores
        //public void SaveScoresCollection(LocalScores scoreSaberScores)
        //{
        //    SaveScoresCollection(scoreSaberScores, songSuggest.activePlayer.PlayerID);
        //}

        //Load Players ScoreSaber Scores
        public ScoreCollection LoadScoreCollection(string filename)
        {
            if (!File.Exists(filePathSettings.activePlayerDataPath + $"{filename}.json")) SaveScoreCollection(new ScoreCollection(), filename);
            String loadedString = File.ReadAllText(filePathSettings.activePlayerDataPath + $"{filename}.json");
            return JsonConvert.DeserializeObject<ScoreCollection>(loadedString, serializerSettings);
        }

        //Save Players ScoreSaber Scores
        public void SaveScoreCollection(ScoreCollection playerScores, string filename)
        {
            File.WriteAllText(filePathSettings.activePlayerDataPath + $"{filename}.json", JsonConvert.SerializeObject(playerScores));
        }

        ////Load Active Players BeatLeader scores
        //public BeatLeaderScores LoadBeatLeaderScores()
        //{
        //    return LoadBeatLeaderScores(songSuggest.activePlayer.PlayerID);
        //}

        ////Load Any Active Players BeatLeader Scores
        //public BeatLeaderScores LoadBeatLeaderScores(string playerID)
        //{
        //    if (!File.Exists(filePathSettings.activePlayerDataPath + $"BL{playerID}.json")) SaveBeatLeaderScores(new BeatLeaderScores());
        //    String loadedString = File.ReadAllText(filePathSettings.activePlayerDataPath + $"BL{playerID}.json");
        //    return JsonConvert.DeserializeObject<BeatLeaderScores>(loadedString, serializerSettings);
        //}

        ////Save Active Players BeatLeader Scores
        //public void SaveBeatLeaderScores(BeatLeaderScores beatLeaderScores)
        //{
        //    SaveBeatLeaderScores(beatLeaderScores, songSuggest.activePlayer.PlayerID);
        //}

        ////Save Any Players Beat Leader Scores
        //public void SaveBeatLeaderScores(BeatLeaderScores beatLeaderScores, string playerID)
        //{
        //    File.WriteAllText(filePathSettings.activePlayerDataPath + $"BL{playerID}.json", JsonConvert.SerializeObject(beatLeaderScores));
        //}

        //Is there a player Refresh File
        public Boolean CheckPlayerRefresh()
        {
            return File.Exists(filePathSettings.activePlayerDataPath + "RefreshPlayer.txt");
        }

        //Adds/removes the player refresh file
        public void TogglePlayerRefresh()
        {
            if(CheckPlayerRefresh())
            //Remove File
            {
                File.Delete(filePathSettings.activePlayerDataPath + "RefreshPlayer.txt");
            }
            //Add File
            else
            {
                File.WriteAllText(filePathSettings.activePlayerDataPath + "RefreshPlayer.txt", "Remind Me: Reset Player Profile");
            }
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