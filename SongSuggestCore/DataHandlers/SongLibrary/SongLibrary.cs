using SongSuggestNS;
using System;
using System.Collections.Generic;

namespace SongLibraryNS
{
    //Static SongLibrary that uses a specific instance of a library for lookups. This along with the ID system should provide access to the stored Songs objects.
    public static class SongLibrary
    {
        private static SongLibraryInstance _activeLibrary;
        public static Dictionary<string, Song> Songs => _activeLibrary?.UIDStringToSong ?? throw new InvalidOperationException("No Library Assigned");
        public static SongID GetID(string hash, String difficulty) { return _activeLibrary.GetID(hash, difficulty); }
        public static Song SongIDToSong(SongID songID) { return _activeLibrary.SongIDToSong(songID); }
        public static Song StringIDToSong(string songID, SongIDType songIDType) { return _activeLibrary.StringIDToSong(songID, songIDType); }
        public static List<Song> SongIDToSong(List<SongID> songIDs) { return _activeLibrary.SongIDToSong(songIDs); }
        public static SongID StringIDToSongID(string stringID, SongIDType songIDType) { return _activeLibrary.StringIDToSongID(stringID, songIDType); }
        public static List<SongID> StringIDToSongID(List<string> stringIDs, SongIDType songIDType) { return _activeLibrary.StringIDToSongID(stringIDs, songIDType); }
        public static bool HasAnySongCategory(SongID songID, SongCategory songCategory) { return _activeLibrary.HasAnySongCategory(songID, songCategory); }
        public static String GetDisplayName(SongID songID) { return _activeLibrary.GetDisplayName(songID); }
        public static List<SongID> GetAllRankedSongIDs(SongCategory songCategory) { return _activeLibrary.GetAllRankedSongIDs(songCategory); }
        //Should only be activated by a SongLibraryInstance's SetAsActiveLibrary();
        internal static void SetAsActiveLibrary(SongLibraryInstance songLibrary)
        {
            _activeLibrary = songLibrary;
        }
    }
}