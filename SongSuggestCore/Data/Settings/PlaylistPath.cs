using System;

namespace Settings
{
    public class PlaylistPath
    {
        public string Subfolders { get; set; } = "";//From after the assigned Playlist path in SongSuggest general settings
        public string FileName { get; set; } //Name of the file minus extension
        public string FileExtension { get; set; } = "bplist"; //Default extension is bplist, but you can assign .json or other if needed.
    }
}
