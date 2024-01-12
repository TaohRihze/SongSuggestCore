﻿using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Settings
{
    ////Suggested locations shown, keep in mind the base path should also be included prior
    ////**Consider rewrite to defaulting paths, and have a variable for base path, and use custom Gets
    //public class FilePathSettings
    //{
    //    public String songLibraryPath { get; set; } // = "\\";
    //    public String playlistPath { get; set; } //= "\\Playlists\\";
    //    public String activePlayerDataPath { get; set; } //= "\\Players\\";
    //    public String top10kPlayersPath { get; set; } //= "\\";
    //    public String bannedSongsPath { get; set; } //= "\\";
    //    public String likedSongsPath { get; set; } //= "\\";
    //    public String filesDataPath { get; set; } //= "\\";
    //    public String lastSuggestionsPath { get; set; } //= "\\";
    //    public String rankedData { get; set; } //= "\\RankedSongs";

    //}

    //All modified paths should be given without leading or trailing folder separators. Return values will be given with a trailing separator.
    public class FilePathSettings
    {
        private string _songLibraryPath = $"";
        private string _playlistPath = $"Playlists";
        private string _activePlayerDataPath = $"Players";
        private string _top10kPlayersPath = $"";
        private string _bannedSongsPath = $"";
        private string _likedSongsPath = $"";
        private string _filesDataPath = $"";
        private string _lastSuggestionsPath = $"";
        private string _rankedData = $"RankedSongs";


        private string _basePath;

        public FilePathSettings(string basePath)
        {
            BasePath = basePath;
        }

        public string BasePath { get => _basePath; set => _basePath = value.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar; }
        public string songLibraryPath { get => TrailingPath(_songLibraryPath); set => _songLibraryPath = value; }
        public string playlistPath { get => TrailingPath(_playlistPath); set => _playlistPath = value; }
        public string activePlayerDataPath { get => TrailingPath(_activePlayerDataPath); set => _activePlayerDataPath = value; }
        public string top10kPlayersPath { get => TrailingPath(_top10kPlayersPath); set => _top10kPlayersPath = value; }
        public string bannedSongsPath { get => TrailingPath(_bannedSongsPath); set => _bannedSongsPath = value; }
        public string likedSongsPath { get => TrailingPath(_likedSongsPath); set => _likedSongsPath = value; }
        public string filesDataPath { get => TrailingPath(_filesDataPath); set => _filesDataPath = value; }
        public string lastSuggestionsPath { get => TrailingPath(_lastSuggestionsPath); set => _lastSuggestionsPath = value; }
        public string rankedData { get => TrailingPath(_rankedData); set => _rankedData = value; }


        private string TrailingPath(string extra)
        {
            extra = extra.TrimStart(Path.DirectorySeparatorChar);
            extra = extra.TrimEnd(Path.DirectorySeparatorChar);
            var path = Path.Combine(BasePath, extra);
            path = path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path;
        }
    }
}

