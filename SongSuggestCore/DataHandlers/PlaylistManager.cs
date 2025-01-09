using Newtonsoft.Json.Linq;
using PlaylistJson;
using Settings;
using SongLibraryNS;
using SongSuggestNS;
using System;
using System.Collections.Generic;
using System.IO;

namespace PlaylistNS
{
    public partial class PlaylistManager
    {
        public SongSuggest songSuggest { get; set; } = SongSuggest.MainInstance;
        public String title { get; set; } = "Try This";
        public String author { get; set; } = "Song Suggest";
        public String image { get; set; } = "";//"data:image/png;base64,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";
        public String fileName { get; set; } = "Playlist";
        public String description { get; set; } = "";
        public String syncURL { get; set; } = null;
        public JObject jObject { get; set; } = new JObject();

        //List of songID's on added songs
        public List<SongID> songIDs = new List<SongID>();

        //Constructor via PlaylistSettings
        public PlaylistManager(PlaylistSettings playlistSettings)
        {
            if (playlistSettings.title != null) this.title = playlistSettings.title;
            if (playlistSettings.author != null) this.author = playlistSettings.author;
            if (playlistSettings.image != null) this.image = playlistSettings.image;
            if (playlistSettings.fileName != null) this.fileName = $"{playlistSettings.fileName}.bplist"; //Default all playlists to .bplist ... if you need json or other overwrite this after
            if (playlistSettings.description != null) this.description = playlistSettings.description;
            if (playlistSettings.syncURL != null) this.syncURL = playlistSettings.syncURL;
        }

        //Constructor via FilePath
        public PlaylistManager(PlaylistPath playlistPath)
        {
            //SetFilePath(playlistPath);
            LoadFile(playlistPath);
        }

        //Constructor via SyncURL
        public PlaylistManager(PlaylistSyncURL syncURL)
        {
            LoadSyncURL(syncURL);
            //this.syncURL = syncURL.SyncURL;
        }

        //Set FilePath via a PlaylistPath
        public void SetFilePath(PlaylistPath path)
        {
            path.Subfolders = path.Subfolders.Trim('\\');
            path.FileExtension = path.FileExtension.Trim('.');
            fileName = Path.Combine(path.Subfolders, $"{path.FileName}.{path.FileExtension}");
        }

        //public void LoadFileOld(String playlistFileName)
        //{
        //    SetPlayList(songSuggest.fileHandler.LoadPlaylist(playlistFileName));
        //}

        public void LoadFile(PlaylistPath filePath)
        {
            SetFilePath(filePath);
            LoadFile();
        }

        public void LoadFile()
        {
            SetPlayList(songSuggest.fileHandler.LoadPlaylist(fileName));
        }

        public void LoadSyncURL(PlaylistSyncURL syncURL)
        {
            var playlist = songSuggest.webDownloader.LoadWebURL(syncURL);

            if (playlist == null) return; //Error occured, so lets not ruin current list.

            //Check if the SyncURL is in correct place, if not, then add it.
            if (!playlist.ContainsKey("customData")) { playlist["customData"] = new JObject(); }
            if (!((JObject)playlist["customData"]).ContainsKey("syncURL")) playlist["customData"]["syncURL"] = syncURL.SyncURL;

            SetPlayList(playlist);
        }

        //Sets the playlist via a JObject (from file or web) Is to replace current functions once working.
        //Internal as public is eithr a filepath or syncurl.
        internal void SetPlayList(JObject playlist)
        {
            jObject = playlist;

            title = (string)playlist["playlistTitle"];
            author = (string)playlist["playlistAuthor"];
            image = (string)playlist["image"];
            description = (string)playlist["playlistDescription"];
            syncURL = (string)playlist["customData"]?["syncURL"];
            //syncURL = (string)playlist["customData"]["syncURL"];
            songIDs.Clear();

            foreach (var song in (JArray)playlist["songs"])
            {
                foreach (var difficulty in (JArray)song["difficulties"])
                {
                    string characteristic = (string)difficulty["characteristic"];
                    string dif = (string)difficulty["name"];
                    string hash = (string)song["hash"];

                    SongID songID = SongLibrary.GetID(characteristic, dif, hash);
                    songIDs.Add(songID);

                    //if ((string)difficulty["characteristic"] == "Standard")
                    //{
                    //    songIDs.Add(SongLibrary.GetID((string)song["hash"], (string)difficulty["name"]));
                    //}
                }
            }
        }

        internal void SetPlayList(Playlist playlist)
        {
            title = playlist.playlistTitle;
            author = playlist.playlistAuthor;
            image = playlist.image;
            description = playlist.playlistDescription;
            syncURL = playlist.customData.syncURL;
            songIDs.Clear();

            foreach (var song in playlist.songs)
            {
                foreach (var difficulty in song.difficulties)
                {
                    string characteristic = difficulty.characteristic;
                    string dif = difficulty.name;
                    string hash = song.hash;

                    SongID songID = SongLibrary.GetID(characteristic, dif, hash);
                    songIDs.Add(songID);

                    //if (difficulty.characteristic == "Standard")
                    //{
                    //    songIDs.Add(SongLibrary.GetID(song.hash, difficulty.name));
                    //}
                }
            }
        }

        //Save a playlist file from the added song ID's
        public void Generate()
        {
            //Overwrite the stored full object with values that could have been replaced (managed by PlaylistManager)
            jObject["playlistTitle"] = title;
            jObject["playlistAuthor"] = author;
            jObject["playlistDescription"] = description;

            //Not all SyncURLs will contain their own URL in the web download, or save it in root,
            //to ensure it is kept in place we always generate the customData and store a syncURL if we got a value in our syncURL
            if (!jObject.ContainsKey("customData") && !string.IsNullOrEmpty(syncURL)) { jObject["customData"] = new JObject(); }
            if (!string.IsNullOrEmpty(syncURL)) jObject["customData"]["syncURL"] = syncURL;

            //Modify songs and update if present
            //Songs that was loaded/received from web, can contain more data
            JArray jObjectArray = (JArray)jObject["songs"] ?? new JArray();
            //Songs that was selected for the playlist
            JArray selectedSongsArray = JArray.FromObject(GetSongJsons());
            //Combined data of the above 2 sources (we try and maintain as much of the original data as possible).
            JArray mergedSongsArray = new JArray();

            foreach (JObject selectedSong in selectedSongsArray)
            {
                bool noOriginal = true;
                foreach (JObject songjObject in jObjectArray)
                {
                    if (CompareSongs(selectedSong, songjObject)) 
                    {
                        //We found a matching song, but as the difficulties in the loaded/web URL playlist may be stacked, we have to replace it only for the matching difficulty.
                        //Note that any other data in the difficulties object may be lost, but there should not be other values. Will need examples of this before making a more complex
                        //Handling (such as how do you define which of the values goes where?)
                        var songjObjectClone = songjObject.DeepClone();
                        songjObjectClone["difficulties"] = selectedSong["difficulties"];
                        mergedSongsArray.Add(songjObjectClone);
                        noOriginal = false;
                        break;
                    }
                }
                if (noOriginal) mergedSongsArray.Add(selectedSong);
            }

            //Move songs after other data for better overview (except image).
            jObject.Remove("songs");
            jObject["songs"] = mergedSongsArray;

            //Set image at end as it is normally the largest element and it is the other elements we are interested in viewing normally.
            jObject.Remove("image");
            jObject["image"] = image;

            songSuggest.fileHandler.SavePlaylist(jObject, fileName);
        }

        //Working on the assumption song1 does not contain multiple difficulties.
        public bool CompareSongs(JObject song1, JObject song2)
        {
            //Check if hash is the same
            if ((string)song1["hash"] != (string)song2["hash"]) return false;

            //Now we need to loop the difficulties if there is any match in the 2nd song (

            string newChar = (string)song1["difficulties"][0]["characteristic"];
            string newName = (string)song1["difficulties"][0]["name"];

            foreach (JObject difc in song2["difficulties"])
            {
                string oldChar = (string)difc["characteristic"];
                string oldName = (string)difc["name"];

                if (oldChar.ToLowerInvariant() == newChar.ToLowerInvariant() && oldName.ToLowerInvariant() == newName.ToLowerInvariant()) return true;
            }
            return false;
        }

        private List<SongJson> GetSongJsons()
        {
            List<SongJson> returnSongs = new List<SongJson>();

            foreach (var songID in songIDs)
            {
                var song = songID.GetSong();

                SongJson songJSON = new SongJson();

                songJSON.hash = song.hash;// songSuggest.songLibrary.GetHash(songID);

                Difficulty difficultyJSON = new Difficulty();
                //difficultyJSON.characteristic = "Standard";
                difficultyJSON.characteristic = song.characteristic;//SongLibrary.SongIDToSong(songID).characteristic;
                difficultyJSON.name = song.GetDifficultyText();//songSuggest.songLibrary.GetDifficultyName(songID);

                songJSON.difficulties = new List<Difficulty>();
                songJSON.difficulties.Add(difficultyJSON);

                returnSongs.Add(songJSON);
            }
            return returnSongs;
        }

        ////Save a playlist file from the added song ID's
        //public void GenerateOld()
        //{
        //    Playlist playlist = new Playlist();

        //    playlist.playlistTitle = title;
        //    playlist.playlistAuthor = author;
        //    playlist.image = image;
        //    playlist.playlistDescription = description;
        //    playlist.customData.syncURL = syncURL;

        //    playlist.songs = new List<SongJson>();
        //    foreach (var songID in songIDs)
        //    {
        //        var song = songID.GetSong();

        //        SongJson songJSON = new SongJson();

        //        songJSON.hash = song.hash;// songSuggest.songLibrary.GetHash(songID);

        //        Difficulty difficultyJSON = new Difficulty();
        //        //difficultyJSON.characteristic = "Standard";
        //        difficultyJSON.characteristic = song.characteristic;//SongLibrary.SongIDToSong(songID).characteristic;
        //        difficultyJSON.name = song.GetDifficultyText();//songSuggest.songLibrary.GetDifficultyName(songID);

        //        songJSON.difficulties = new List<Difficulty>();
        //        songJSON.difficulties.Add(difficultyJSON);

        //        playlist.songs.Add(songJSON);
        //    }
        //    songSuggest.fileHandler.SavePlaylist(playlist, fileName);
        //    //songSuggest.fileHandler.SavePlaylist(playlist, "lastsaved.bplist"); //Debug code
        //    //GenerateJObject();
        //}

        //Add a song to the playlist
        public void AddSong(SongID songID)
        {
            songIDs.Add(songID);
        }

        public void AddSongs(List<SongID> songID)
        {
            songIDs.AddRange(songID);
        }

        //Remove a song from the playlist
        public void RemoveSong(SongID songID)
        {
            songIDs.Remove(songID);
        }

        //Clear all songs
        public void ClearSongs()
        {
            songIDs.Clear();
        }

        public List<SongID> GetSongs()
        {
            return songIDs;
        }
    }

    public enum SongSorting
    {
        Oldest = 0,
        WeightedRandom = 1
    }

}
