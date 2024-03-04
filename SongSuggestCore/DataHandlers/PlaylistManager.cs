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
            SetFilePath(playlistPath);
            LoadFile(fileName);
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

        public void LoadFile(String playlistFileName)
        {
            SetPlayList(songSuggest.fileHandler.LoadPlaylist(playlistFileName));
        }

        public void LoadSyncURL(PlaylistSyncURL syncURL)
        {
            var playlist = songSuggest.webDownloader.LoadWebURL(syncURL.SyncURL);
            if (playlist == null) return; //Error occured, so lets not ruin current list.
            SetPlayList(playlist);
        }

        public void SetPlayList(Playlist playlist)
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
                    if (difficulty.characteristic == "Standard")
                    {
                        songIDs.Add(SongLibrary.GetID(song.hash, difficulty.name));
                    }
                }
            }
        }

        //Save a playlist file from the added song ID's
        public void Generate()
        {
            Playlist playlist = new Playlist();

            playlist.playlistTitle = title;
            playlist.playlistAuthor = author;
            playlist.image = image;
            playlist.playlistDescription = description;
            playlist.customData.syncURL = syncURL;

            playlist.songs = new List<SongJson>();
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

                playlist.songs.Add(songJSON);
            }
            songSuggest.fileHandler.SavePlaylist(playlist, fileName);
            //songSuggest.fileHandler.SavePlaylist(playlist, "lastsaved.bplist"); //Debug code
        }

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
