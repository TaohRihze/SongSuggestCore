﻿using ScoreSabersJson;
using SongSuggestNS;
using System;
using System.Collections.Generic;
using System.Linq;
using BeatLeaderJson;
using System.IO;

namespace SongLibraryNS
{
    //Static SongLibrary that uses a specific instance of a library for lookups. This along with the ID system should provide access to the stored Songs objects.
    public static class SongLibrary
    {
        internal static SongLibraryInstance ActiveLibrary { get; set; }
        [Obsolete("This Will Be Removed")]
        public static Dictionary<string, Song> Songs => ActiveLibrary?.songs ?? throw new InvalidOperationException("No Library Assigned");
        public static bool Compare(SongID id1, SongID id2) { return ActiveLibrary.songs[id1.UniqueString] == ActiveLibrary.songs[id2.UniqueString]; }
        public static string GetID(SongID songID, SongIDType songIDType) { return ActiveLibrary.GetID(songID, songIDType); }
        public static Song SongIDToSong(SongID songID) { return ActiveLibrary.SongIDToSong(songID); }
        public static Song StringIDToSong(string songID, SongIDType songIDType) { return ActiveLibrary.StringIDToSong(songID, songIDType); }
        public static SongID StringIDToSongID(string stringID, SongIDType songIDType) { return ActiveLibrary.StringIDToSongID(stringID, songIDType); }
        public static List<SongID> StringIDToSongID(List<string> stringIDs, SongIDType songIDType) { return ActiveLibrary.StringIDToSongID(stringIDs, songIDType); }
        public static bool HasAnySongCategory(SongID songID, SongCategory songCategory) { return ActiveLibrary.HasAnySongCategory(songID, songCategory); }
    }

    //Data on a specific Song Library.
    public class SongLibraryInstance
    {
        //Statics
        //public static SongLibraryInstance ActiveLibrary { get; set; }
        //public static Dictionary<string, Song> Songs => ActiveLibrary?.songs ?? throw new InvalidOperationException("No Library Assigned");
        //public static bool Compare(SongID id1, SongID id2) { return ActiveLibrary.songs[id1.UniqueString] == ActiveLibrary.songs[id2.UniqueString]; }

        //Dynamics
        public SongSuggest songSuggest { get; set; }
        public const String FormatVersion = "2.0";

        //Returns true if songs has been added/modified since load/last save.
        public bool Updated { get; set; } = false;

        //[Obsolete("Start using Functions to lookup data instead")]
        public Dictionary<String, Song> songs = new Dictionary<String, Song>();

        //Sets this library as the active library
        public void SetActive() { SongLibrary.ActiveLibrary = this; }

        //Add a Song Object to the Libary if Unknown, else ignore it. 
        //Use UpsertSong if you want an update on known songs.
        //Update to work with any SongID object
        public void AddSong(ScoreSaberID song)
        {
            //If song is not in the library, create it, add it, and add it to the idLookup Dictionary
            if (!songs.ContainsKey(song.UniqueString))
            {
                UpsertSong(song);
            }
        }

        //UpsertSong via ID Object
        //**Change to work with any SongID object
        public void UpsertSong(ScoreSaberID songID)
        {
            var leaderboardInfo = songSuggest.webDownloader.GetLeaderboardInfo(songID.Value);
            UpsertSong(leaderboardInfo);
            Updated = true;
        }

        //Add a Song via the BeatLeader SongSuggestSong Json Object
        //Note: BeatLeader provides a song in the SongSuggest format, hence the SongSuggestSong object.
        public void UpsertSong(SongSuggestSong song)
        {
            //Either find the Song or Create a new object for it with basic information.
            Song internalSong;
            if (!songs.TryGetValue(((BeatLeaderID)song.id).UniqueString, out internalSong))
            {
                //Generate a new song object, so we can get the internalID for match
                internalSong = new Song()
                {
                    hash = song.hash.ToUpperInvariant(),
                    difficulty = Song.GetDifficultyValue(song.difficulty),
                    name = song.name
                };
            }

            //Set or Update remaining generic info
            internalSong.beatLeaderID = song.id;
            internalSong.starBeatLeader = song.stars;

            //Internal library links
            SetLibraryLink(internalSong);

            //Ranked Status
            if
                (internalSong.starBeatLeader > 0) AddSongCategory(internalSong, SongCategory.BeatLeader);
            else
                RemoveSongCategory(internalSong, SongCategory.BeatLeader);

            //Note we "likely" updated a song, so we can save Library if needed.
            Updated = true;
        }

        //Adds a song via ScoreSaber LeaderboarDInfo json object
        public void UpsertSong(LeaderboardInfo song)
        {
            //Get the reference object for the ID of the song.
            ScoreSaberID scoreSaberID = (ScoreSaberID)$"{song.id}";

            //Either generate a new song or set it to the known song.
            Song internalSong;
            if (!songs.TryGetValue(scoreSaberID.UniqueString, out internalSong))
            {
                //Generate a new song object, so we can get the internalID for match
                internalSong = new Song()
                {
                    hash = song.songHash.ToUpperInvariant(),
                    difficulty = $"{song.difficulty.difficulty}",
                    name = song.songName
                };
            }

            //Update the scoresaber specific values
            internalSong.scoreSaberID = $"{song.id}";
            internalSong.starScoreSaber = song.stars;

            //Update songlibrary Links after (so any missing references is updated)
            SetLibraryLink(internalSong);

            //Ranked Status
            if
                (internalSong.starScoreSaber > 0) AddSongCategory(internalSong, SongCategory.ScoreSaber);
            else
                RemoveSongCategory(internalSong, SongCategory.ScoreSaber);

            //Note we updated a song, so we can save Library if needed.
            Updated = true;
        }

        //Adds a song via ScoreSaber PlayerScore json object
        public void UpsertSong(PlayerScore song)
        {
            //Get the reference object for the ID of the song.
            ScoreSaberID scoreSaberID = (ScoreSaberID)$"{song.leaderboard.id}";

            //Either generate a new song or set it to the known song.
            Song internalSong;
            if (!songs.TryGetValue(scoreSaberID.UniqueString, out internalSong))
            {
                //Generate a new song object, so we can get the internalID for match
                internalSong = new Song()
                {
                    hash = song.leaderboard.songHash.ToUpperInvariant(),
                    difficulty = $"{song.leaderboard.difficulty.difficulty}",
                    name = song.leaderboard.songName
                };
            }

            //Update the scoresaber specific values
            internalSong.scoreSaberID = $"{song.leaderboard.id}";
            internalSong.starScoreSaber = song.leaderboard.stars;

            //Update songlibrary Links after (so any missing references is updated)
            SetLibraryLink(internalSong);

            //Ranked Status
            if
                (internalSong.starScoreSaber > 0) AddSongCategory(internalSong, SongCategory.ScoreSaber);
            else
                RemoveSongCategory(internalSong, SongCategory.ScoreSaber);

            //Note we updated a song, so we can save Library if needed.
            Updated = true;
        }

        public string GetName(SongID songID)
        {
            try
            {
                return songs[songID.UniqueString].name;
            }
            //Song is not in library, lets try pulling info from web
            catch
            {
                return "";
            }
        }

        public String GetDisplayName(SongID songID)
        {
            try
            {
                Song song = songs[songID.UniqueString];

                //Create the default ID and use a specific ID based on called class type
                string songIDString = song.songID;
                if (songID is BeatLeaderID) songIDString = song.beatLeaderID;
                if (songID is ScoreSaberID) songIDString = song.scoreSaberID;

                return $"{song.name} ({song.GetDifficultyText()} - {song.songID})";
            }
            //Song is not in library
            catch
            {
                return "";
            }
        }

        public String GetHash(SongID songID)
        {
            try
            {
                return songs[songID.UniqueString].hash;
            }
            //Song is not in library
            catch
            {
                return "";
            }
        }

        public String GetDifficultyName(SongID songID)
        {
            try
            {
                return songs[songID.UniqueString].GetDifficultyText();
            }
            //Song is not in library
            catch
            {
                return "";

            }
        }

        //Returns the ID of a known song, or search web.
        public SongID GetID(String hash, String difficulty)
        {
            Song foundSong = null;
            //Try and find the songs information and return it from library
            foreach (Song song in songs.Values)
            {
                if (song.hash.ToUpperInvariant() == hash.ToUpperInvariant() && song.difficulty == GetDifficultyValue(difficulty)) foundSong = song;
            }

            //If the song was not found, try pulling info from web and then find it
            if (foundSong == null)
            {
                //Add missing song from web data, and try and find information again
                GetScoreSaberSongInfo(hash, GetDifficultyValue(difficulty));
                foreach (Song song in songs.Values)
                {
                    if (song.hash.ToUpperInvariant() == hash.ToUpperInvariant() && song.difficulty == GetDifficultyValue(difficulty)) foundSong = song;
                }
            }
            return (ScoreSaberID)foundSong.scoreSaberID;
        }

        //Returns the ID string of an object if available.
        internal string GetID(SongID songID, SongIDType idType)
        {
            //Find the song object assigned to the songID (if any)
            Song song;
            if (!songs.TryGetValue(songID.UniqueString, out song)) return null;

            //Try and find the value for the requested type, or return a null if not found.
            string idText = null;
            switch (idType)
            {
                case SongIDType.Internal:
                    idText = song.songID;
                    break;
                case SongIDType.ScoreSaber:
                    idText = song.scoreSaberID;
                    break;
                case SongIDType.BeatLeader:
                    idText = song.beatLeaderID;
                    break;
            }
            return (string.IsNullOrEmpty(idText)) ? null : idText;
        }

        public void GetScoreSaberSongInfo(String hash, String difficultyValue)
        {
            UpsertSong(songSuggest.webDownloader.GetLeaderboardInfo(hash, difficultyValue));
            Save();
        }

        public void GetScoreSaberSongInfo(String scoreSaberID)
        {
            UpsertSong(songSuggest.webDownloader.GetLeaderboardInfo(scoreSaberID));
            Save();
        }

        //Checks if a song is in the Library
        public Boolean Contains(String hash, String difficulty)
        {
            Boolean foundSong = false;
            //Try and find the songs information and return if it was in the library.
            foreach (Song song in songs.Values)
            {
                if (song.hash.ToUpperInvariant() == hash.ToUpperInvariant() && song.difficulty == GetDifficultyValue(difficulty)) foundSong = true;
            }
            return foundSong;
        }

        //Removes songs that are not actively linked to a supported format
        //**Temporary Workaround, also removes songs without a ScoreSaber ID**
        public void RemoveSongsWithoutSongCategories()
        {
            //**ScoreSaberID Null removal for backward Compatibility
            var keyPairsWithoutScoreSaberID = songs
                .Where(pair => string.IsNullOrEmpty(pair.Value.scoreSaberID))
                .ToList();

            if (keyPairsWithoutScoreSaberID.Count > 0)
            {
                Updated = true;
                foreach (var keyPair in keyPairsWithoutScoreSaberID)
                {
                    songs.Remove(keyPair.Key);
                }
            }

            // Removal of any keypair without any songCategory
            var keyPairsToRemove = songs
                .Where(pair => pair.Value.songCategory == 0)
                .ToList();

            if (keyPairsToRemove.Count > 0)
            {
                Updated = true;
                foreach (var keyPair in keyPairsToRemove)
                {
                    songs.Remove(keyPair.Key);
                }
            }
        }

        //Activate SongType/s
        public void AddSongCategory(Song song, SongCategory songCategory)
        {
            //Check if update is needed and then perform it.
            if ((song.songCategory & songCategory) != songCategory)
            {
                Updated = true;
                song.songCategory = songCategory | song.songCategory;
            }
        }

        //Deactives SongType/s
        public void RemoveSongCategory(Song song, SongCategory songCategory)
        {
            // Check if update is needed and then perform it.
            if ((song.songCategory & songCategory) != 0)
            {
                Updated = true;
                song.songCategory = song.songCategory & (~songCategory);
            }
        }




        //Saves an updated library
        public void Save()
        {
            songSuggest.fileHandler.SaveSongLibrary(new List<Song>(songs.Values.Distinct()));
            Updated = false;
        }

        //Sets the library to the current list of songs.
        public void SetLibrary(List<Song> songs)
        {
            this.songs.Clear();
            foreach (Song song in songs)
            {
                SetLibraryLink(song);
                //this.songs.Add(song.scoreSaberID, song);
            }
        }

        //Returns True/False if hte song is recorded with an active SongCategory
        public bool HasAnySongCategory(String songID)
        {
            return (songs.ContainsKey(songID) && songs[songID].songCategory != 0);
        }

        //Returns True/False if hte song is recorded with an active SongCategory
        public bool HasAnySongCategory(SongID songID)
        {
            return (songs.ContainsKey(songID.UniqueString) && songs[songID.UniqueString].songCategory != 0);
        }


        //Returns True/False if the song is recorded with an active SongCategory
        public bool HasAnySongCategory(String hash, String difficulty)
        {
            return HasAnySongCategory(GetID(hash, difficulty));
        }

        public bool HasAllSongCategory(string songID, SongCategory category)
        {
            SongID id = (ScoreSaberID)songID;
            if (!songs.ContainsKey(id.UniqueString)) return false;
            return (songs[id.UniqueString].songCategory & category) == category;
        }

        //Returns True/False if hte song is recorded with an active SongCategory
        public bool HasAllSongCategory(SongID songID, SongCategory category)
        {
            if (!songs.ContainsKey(songID.UniqueString)) return false;
            return (songs[songID.UniqueString].songCategory & category) == category;
        }

        //public bool HasAnySongCategory(string songID, SongCategory category)
        //{
        //    if (!songs.ContainsKey(songID)) return false;
        //    return (songs[songID].songCategory & category) > 0;
        //}

        public bool HasAnySongCategory(SongID songID, SongCategory category)
        {
            if (!songs.ContainsKey(songID.UniqueString)) return false;
            return (songs[songID.UniqueString].songCategory & category) > 0;
        }

        //Returns the song or null if not found.
        public Song SongIDToSong(SongID songID)
        {
            return songs.TryGetValue(songID.UniqueString, out Song song) ? song : null;
        }

        //Returns the song or null if not found.
        public Song StringIDToSong(string stringID, SongIDType songIDType)
        {
            SongID songID;
            switch (songIDType)
            {
                case SongIDType.Internal:
                    songID = (InternalID)stringID;
                    break;
                case SongIDType.ScoreSaber:
                    songID = (ScoreSaberID)stringID;
                    break;
                case SongIDType.BeatLeader:
                    songID = (BeatLeaderID)stringID;
                    break;
                default:
                    throw new InvalidOperationException("Unhandled SongIDType used");
            }
            return SongIDToSong(songID);
        }

        //Return IDs of all known ranked songs (As ScoreSaberIDs for now ... larger rewrite needed in Old&New else)
        public List<SongID> GetAllRankedSongIDs(SongCategory songCategory)
        {
            var rankedSongIDs = songs.Values
                .Where(c => (c.songCategory & songCategory) != 0)
                .Distinct()
                .Select(c => (SongID)(ScoreSaberID)c.scoreSaberID)
                .ToList();
            return rankedSongIDs;
        }

        //Return IDs of all known ranked songs from the given list
        public List<String> GetAllRankedSongIDs(SongCategory songCategory, List<String> songs)
        {
            return this.songs.Where(c => (c.Value.songCategory & songCategory) != 0).Select(c => c.Key).Intersect(songs).ToList();
        }

        //Adds the internal ID's to all songs after batch imports
        public void ResetLibraryLinks()
        {
            //Get each unique Song
            var tempSongs = songs.Values.Distinct().ToList();

            //Perform counts of what is expected (and links we have)
            int songsCount = tempSongs.Count();
            int scoreSaberCount = tempSongs.Where(c => !string.IsNullOrEmpty(c.scoreSaberID)).Count();
            int beatLeaderCount = tempSongs.Where(c => !string.IsNullOrEmpty(c.beatLeaderID)).Count();
            int songsCountDirect = songs.Count;
            int totalCount = songsCount + scoreSaberCount + beatLeaderCount;
            songSuggest.log?.WriteLine($"Reset Library Link Expected Direct: {songsCountDirect} Calculated: {totalCount} (Songs: {songsCount} SS: {scoreSaberCount} BL: {beatLeaderCount})");

            //Clear the library and reset links
            songs.Clear();
            foreach (var song in tempSongs)
            {
                SetLibraryLink(song);
            }

            //Update the values of expected and found
            songsCount = songs.Select(c => c.Value).Distinct().Count();
            scoreSaberCount = songs.Select(c => c.Value).Distinct().Where(c => !string.IsNullOrEmpty(c.scoreSaberID)).Count();
            beatLeaderCount = songs.Select(c => c.Value).Distinct().Where(c => !string.IsNullOrEmpty(c.beatLeaderID)).Count();
            songsCountDirect = songs.Count;
            totalCount = songsCount + scoreSaberCount + beatLeaderCount;
            songSuggest.log?.WriteLine($"Reset Library Link Found Direct: {songsCountDirect} Calculated: {totalCount} (Songs: {songsCount} SS: {scoreSaberCount} BL: {beatLeaderCount})");
        }

        //Sets all ID links for a specific Song
        public void SetLibraryLink(Song song)
        {
            songs[song.songID.ToUpperInvariant()] = song;
            if (!string.IsNullOrEmpty(song.scoreSaberID)) songs[((ScoreSaberID)song.scoreSaberID).UniqueString] = song;
            if (!string.IsNullOrEmpty(song.beatLeaderID)) songs[((BeatLeaderID)song.beatLeaderID).UniqueString] = song;
        }

        //Conversion of a String ID to a wanted SongIDType
        public SongID StringIDToSongID(string stringID, SongIDType songIDType)
        {
            switch (songIDType)
            {
                case SongIDType.Internal:
                    return (InternalID)stringID;
                case SongIDType.ScoreSaber:
                    return (ScoreSaberID)stringID;
                case SongIDType.BeatLeader:
                    return (BeatLeaderID)stringID;
                default:
                    throw new InvalidOperationException("Unhandled SongIDType used");
            }
        }

        public List<SongID> StringIDToSongID(List<string> stringIDs, SongIDType songIDType)
        {
            switch (songIDType)
            {
                case SongIDType.Internal:
                    return stringIDs.Select(c => (SongID)(InternalID)c).ToList();
                case SongIDType.ScoreSaber:
                    return stringIDs.Select(c => (SongID)(ScoreSaberID)c).ToList();
                case SongIDType.BeatLeader:
                    return stringIDs.Select(c => (SongID)(BeatLeaderID)c).ToList();
                default:
                    throw new InvalidOperationException("Unhandled SongIDType used");
            }
        }



        //Translate the difficulty name with the assigned value.
        public String GetDifficultyValue(String difficultyText)
        {
            difficultyText = difficultyText.ToLowerInvariant();
            switch (difficultyText)
            {
                case "easy":
                    return "1";
                case "normal":
                    return "3";
                case "hard":
                    return "5";
                case "expert":
                    return "7";
                case "expert+":
                    return "9";
                //Playlists Expert+ reference
                case "expertplus":
                    return "9";
                default:
                    return "0";
            }
        }
    }

    public abstract class SongID
    {
        // Abstract property to represent the prefix
        abstract public string Prefix { get; }
        public string Value;
        public string UniqueString => $"{Prefix}{Value}".ToUpperInvariant();

        protected SongID() { }

        public SongID(string value)
        {
            Value = value;
        }
        public static implicit operator string(SongID id) => id.Value;

        //Allow comparison between songID objects.
        public override bool Equals(object obj)
        {
            // If same object type, just compare directly on value
            if (obj is SongID songId)
            {
                if (this.Prefix == songId.Prefix)
                {
                    return this.Value == songId.Value;
                }
                return SongLibrary.Compare(this, songId);
            }
            return false;
        }
        public static bool operator ==(SongID left, SongID right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(SongID left, SongID right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            // Use the hash code from another object (assuming GetViaSongID returns an object with its own GetHashCode implementation)
            var relatedObject = SongLibrary.ActiveLibrary.SongIDToSong(this);
            return relatedObject.GetHashCode();
        }
    }

    //ID in the internal format (Generated automatic from a Song object based on Hash, Difficulty, Characteristic)
    public class InternalID : SongID
    {
        public override string Prefix => "ID";                                                              //Unique Prefix for the ID
        public static implicit operator InternalID(string value) => new InternalID { Value = value };       //Creation from String
    }

    //BeatLeader SongID
    public class BeatLeaderID : SongID
    {
        public override string Prefix => "BL";
        public static implicit operator BeatLeaderID(string value) => new BeatLeaderID { Value = value };
    }

    //ScoreSaber SongID
    public class ScoreSaberID : SongID
    {
        public override string Prefix => "SS";
        public static implicit operator ScoreSaberID(string value) => new ScoreSaberID { Value = value };
    }

    //Different songIDs based on active sources
    public enum SongIDType
    {
        Internal,       //Internal ID created by hash, difc, characteristic
        ScoreSaber,     //ScoreSaberID
        BeatLeader,     //BeatLeaderID
    }
}