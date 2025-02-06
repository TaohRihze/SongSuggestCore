using ScoreSabersJson;
using SongSuggestNS;
using System;
using System.Collections.Generic;
using System.Linq;
using BeatLeaderJson;
using Actions;
using PlaylistJson;

namespace SongLibraryNS
{
    //Data on a specific Song Library.
    public class SongLibraryInstance
    {
        //Dynamics
        public SongSuggest songSuggest { get; set; }
        public const String FormatVersion = "2.0";

        //Returns true if songs has been added/modified since load/last save.
        public bool Updated { get; set; } = false;

        //Storage of Songs and ID's (ID's get created once requested on Inserts)

        //For 1.37- client backward compatibility
        //Also delete adding stuff in Link Creation once songs is removed, marked in comments there.
        [Obsolete("Use UIDStringToSong instead")]

        public Dictionary<string, Song> songs = new Dictionary<string, Song>();
        public Dictionary<string, SongID> UIDStringToSongID = new Dictionary<string, SongID>();
        public Dictionary<string, Song> UIDStringToSong = new Dictionary<string, Song>();

        //Sets this library as the active library
        public void SetActive() { SongLibrary.SetAsActiveLibrary(this); }

        //Add a Song Object to the Libary if Unknown, else ignore it. 
        //Use UpsertSong if you want an update on known songs.
        //Update to work with any SongID object
        [Obsolete("Use Upserts Only")]
        public void AddSong(ScoreSaberID song)
        {
            //If song is not in the library, create it, add it, and add it to the idLookup Dictionary
            if (!UIDStringToSong.ContainsKey(song.UniqueID))
            {
                UpsertSong(song);
            }
        }

        #region Upserts

        //UpsertSong via ID Object
        //**Change to work with any SongID object
        public SongID UpsertSong(ScoreSaberID songID)
        {
            var leaderboardInfo = songSuggest.webDownloader.GetLeaderboardInfo(songID.Value);
            return UpsertSong(leaderboardInfo);
        }

        //Creates a new song from given ID, if the string is a legal internalIDString
        public SongID UpsertInternalIDString(string internalIDString)
        {
            if (string.IsNullOrEmpty(internalIDString)) return null;

            //We expect there to be 3 parts in the format characteristic, difficulty, hash ... we try and split, and work on the assumption inputs are correct.
            var parts = internalIDString.Split('-');
            if (parts.Length == 3)
            {

                string characteristic = parts[0];
                string difficulty = parts[1];
                string hash = parts[2];

                return UpsertSong(characteristic, difficulty, hash);
            }
            return null;
        }

        //Base Creation
        //Creates and adds an empty song to the requested ID if not created already.
        public SongID UpsertSong(string characteristic, string difficulty, string hash)
        {
            //Find the song if known, and return it, else create a new and have it linked
            string UID = InternalID.GetUID(Song.GetInternalID(characteristic, Song.GetDifficultyValue(difficulty), hash));
            if (UIDStringToSongID.TryGetValue(UID, out var songID)) return songID;

            //Create a new song and return its link
            Song internalSong = new Song()
            {
                hash = hash.ToUpperInvariant(),
                difficulty = Song.GetDifficultyValue(difficulty).ToUpperInvariant(),
                characteristic = characteristic,
            };

            return SetLibraryLink(internalSong);
        }

        //Add a Song via the BeatLeader SongSuggestSong Json Object
        //Note: BeatLeader provides a song in the SongSuggest format, hence the SongSuggestSong object.
        //All of these are ranked songs, so no need to check if ranked.
        public SongID UpsertSong(SongSuggestSong song)
        {
            string hash = song.hash.ToUpperInvariant();
            string difficulty = song.difficulty;
            string characteristic = song.mode;

            SongID songID = UpsertSong(characteristic, difficulty, hash);
            Song internalSong = songID.GetSong();

            //Set or Update remaining generic info
            internalSong.name = song.name;
            internalSong.beatLeaderID = song.id;
            internalSong.starBeatLeader = song.stars;
            internalSong.starAccBeatLeader = song.accRating;
            internalSong.starPassBeatLeader = song.passRating;
            internalSong.starTechBeatLeader = song.techRating;

            //Ranked Status
            if
                (internalSong.starBeatLeader > 0) AddSongCategory(internalSong, SongCategory.BeatLeader);
            else
                RemoveSongCategory(internalSong, SongCategory.BeatLeader);

            Updated = true;

            //We updated the song, so now we must update its links.
            SetLibraryLink(internalSong);

            return songID;
        }

        //Adds a song via ScoreSaber LeaderboardInfo json object
        public SongID UpsertSong(LeaderboardInfo song)
        {
            //ScoreSaber prefix with Solo mode so we have to remove it.
            string characteristic = song.difficulty.gameMode.Substring(4);
            string difc = Song.GetDifficultyText($"{song.difficulty.difficulty}");
            string hash = song.songHash.ToUpperInvariant();

            ////Get or Create Song
            var songID = UpsertSong(characteristic, difc, hash);
            var internalSong = songID.GetSong();

            //Update the scoresaber specific values
            internalSong.scoreSaberID = $"{song.id}";
            internalSong.name = song.songName;

            if (song.ranked)
            {
                AddSongCategory(internalSong, SongCategory.ScoreSaber);
                internalSong.starScoreSaber = song.stars;
            }
            else
            {
                RemoveSongCategory(internalSong, SongCategory.ScoreSaber);
                internalSong.starScoreSaber = 0;
            }

            //Note we updated a song, so we can save Library if needed.
            Updated = true;

            //We updated the song, so now we must update its links.
            SetLibraryLink(internalSong);

            return songID;
        }

        //Adds a song via ScoreSaber PlayerScore json object (We just pass the Leaderboard on)
        public SongID UpsertSong(PlayerScore song)
        {
            return UpsertSong(song.leaderboard);
        }
        #endregion

        #region GetSongDetails (future work to avoid direct manipulation of the Songs (as format may be changed between versions))
        public string GetName(SongID songID)
        {
            if (!UIDStringToSong.TryGetValue(songID.UniqueID, out Song song)) return "-Unknown Song-";
            return song.name;
        }

        public String GetDisplayName(SongID songID)
        {
            if (!UIDStringToSong.TryGetValue(songID.UniqueID, out Song song)) return "-Unknown Song-";

            //Create the default ID and use a specific ID based on called class type
            string songIDString = song.internalID;
            if (songID is BeatLeaderID) songIDString = song.beatLeaderID;
            if (songID is ScoreSaberID) songIDString = song.scoreSaberID;

            return $"{song.name} ({song.characteristic} - {song.GetDifficultyText()} - {songIDString})";
        }

        public String GetHash(SongID songID)
        {
            if (!UIDStringToSong.TryGetValue(songID.UniqueID, out Song song)) return "";
            return song.hash;
        }

        public String GetDifficultyName(SongID songID)
        {
            if (!UIDStringToSong.TryGetValue(songID.UniqueID, out Song song)) return "";
            return song.GetDifficultyText();
        }
        
        [Obsolete("Should also include characteristic")]
        public SongID GetID(string hash, string difficulty)
        {
            return GetID("Standard", difficulty, hash);
        }

        // Returns a valid SongID for the given parameters. Creates a new Song entry and returns its InternalID link if necessary.
        public SongID GetID(string characteristic, string difficulty, string hash)
        {
            string UID = InternalID.GetUID(Song.GetInternalID(characteristic, Song.GetDifficultyValue(difficulty), hash));

            SongID songID = UIDStringToSongID.TryGetValue(UID, out var cachedID)
                ? (InternalID)cachedID
                : UpsertSong(characteristic, difficulty, hash);

            return songID;
        }

        //Returns the ID string of an object if available.
        internal string GetID(SongID songID, SongIDType idType)
        {
            //Find the song object assigned to the songID (if any)
            Song song;
            if (!UIDStringToSong.TryGetValue(songID.UniqueID, out song)) return null;

            //Try and find the value for the requested type, or return a null if not found.
            string idText = null;
            switch (idType)
            {
                case SongIDType.Internal:
                    idText = song.internalID;
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

        public void GetScoreSaberSongInfo(string hash, string difficultyValue)
        {
            UpsertSong(songSuggest.webDownloader.GetLeaderboardInfo(hash, difficultyValue));
            Save();
        }

        public void GetScoreSaberSongInfo(string scoreSaberID)
        {
            UpsertSong(songSuggest.webDownloader.GetLeaderboardInfo(scoreSaberID));
            Save();
        }
        #endregion

        //Removes songs that are not actively linked to a supported format
        //**Temporary Workaround, also removes songs without a ScoreSaber ID**
        //**Resets the libraries update time to 0 for Beat Leader as Beat Leader ID's may be removed by this**
        public void RemoveSongsWithoutSongCategories()
        {
            //**ScoreSaberID Null removal for backward Compatibility
            var keyPairsWithoutScoreSaberID = UIDStringToSong
                .Where(pair => string.IsNullOrEmpty(pair.Value.scoreSaberID))
                .ToList();

            if (keyPairsWithoutScoreSaberID.Count > 0)
            {
                Updated = true;
                foreach (var keyPair in keyPairsWithoutScoreSaberID)
                {
                    UIDStringToSong.Remove(keyPair.Key);
                }
            }

            // Removal of any keypair without any songCategory
            var keyPairsToRemove = UIDStringToSong
                .Where(pair => pair.Value.songCategory == 0)
                .ToList();

            if (keyPairsToRemove.Count > 0)
            {
                Updated = true;
                foreach (var keyPair in keyPairsToRemove)
                {
                    UIDStringToSong.Remove(keyPair.Key);
                }
            }

            //Reset the update time on Beat Leader Songs as they are no longer valid (Leaderboard File is not impacted be so no update there)
            //**This is a temporary workaround along with the ScoreSaberID Null
            songSuggest.filesMeta.beatLeaderSongsUpdated = 0;
            songSuggest.fileHandler.SaveFilesMeta(songSuggest.filesMeta);
            UIDStringToSongID.Clear(); //Songs may have been removed, so we should not hand out incorrect SongID links moving forward.
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
            songSuggest.fileHandler.SaveSongLibrary(new List<Song>(UIDStringToSong.Values.Distinct()));
            Updated = false;
        }

        //Sets the library to the current list of songs.
        public void SetLibrary(List<Song> songs)
        {
            this.UIDStringToSong.Clear();
            this.UIDStringToSongID.Clear();
            //TMP remove once songs is removed.
            this.songs.Clear();
            foreach (Song song in songs)
            {
                SetLibraryLink(song);
            }
        }

        //Returns True/False if hte song is recorded with an active SongCategory
        public bool HasAnySongCategory(SongID songID)
        {
            return songID.GetSong().songCategory != 0;
        }


        //Returns True/False if the song is recorded with an active SongCategory
        [Obsolete("Use Song ID Version")]
        public bool HasAnySongCategory(string hash, string difficulty)
        {
            return HasAnySongCategory(GetID(hash, difficulty));
        }

        ////Returns True/False if the song is recorded with an active SongCategory
        //public bool HasAnySongCategory(string hash, string difficulty)
        //{
        //    return HasAnySongCategory(GetID(hash, difficulty));
        //}

        public bool HasAllSongCategory(string songID, SongCategory category)
        {
            SongID id = (ScoreSaberID)songID;
            if (!UIDStringToSong.ContainsKey(id.UniqueID)) return false;
            return (UIDStringToSong[id.UniqueID].songCategory & category) == category;
        }

        //Returns True/False if hte song is recorded with an active SongCategory
        public bool HasAllSongCategory(SongID songID, SongCategory category)
        {
            if (!UIDStringToSong.ContainsKey(songID.UniqueID)) return false;
            return (UIDStringToSong[songID.UniqueID].songCategory & category) == category;
        }


        public bool HasAnySongCategory(SongID songID, SongCategory category)
        {
            if (!UIDStringToSong.ContainsKey(songID.UniqueID)) return false;
            return (UIDStringToSong[songID.UniqueID].songCategory & category) > 0;
        }

        //Returns the song or null if not found.
        public Song SongIDToSong(SongID songID)
        {
            return UIDStringToSong.TryGetValue(songID.UniqueID, out Song song) ? song : null;
        }

        //Return a list of songs that is found.
        public List<Song> SongIDToSong(List<SongID> songIDs)
        {
            List<Song> returnSongs = new List<Song>();
            foreach (var songID in songIDs)
            {
                if (UIDStringToSong.TryGetValue(songID.UniqueID, out Song song)) returnSongs.Add(song);
            }
            return returnSongs;
        }

        //Convert the strings to a SongID, and returns related record (if any)
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

        //Return IDs of all known ranked songs
        public List<SongID> GetAllRankedSongIDs(SongCategory songCategory)
        {
            var rankedSongIDs = UIDStringToSong.Values
                .Where(c => (c.songCategory & songCategory) != 0)
                .Distinct()
                .Select(c => (SongID)(InternalID)c.internalID)
                .ToList();

            return rankedSongIDs;
        }

        //Adds the internal ID's to all songs after batch imports
        public void ResetLibraryLinks()
        {
            //Get each unique Song
            var tempSongs = UIDStringToSong.Values.Distinct().ToList();

            //Perform counts of what is expected (and links we have)
            int songsCount = tempSongs.Count();
            int scoreSaberCount = tempSongs.Where(c => !string.IsNullOrEmpty(c.scoreSaberID)).Count();
            int beatLeaderCount = tempSongs.Where(c => !string.IsNullOrEmpty(c.beatLeaderID)).Count();
            int songsCountDirect = UIDStringToSong.Count;
            int totalCount = songsCount + scoreSaberCount + beatLeaderCount;
            songSuggest.log?.WriteLine($"Reset Library Link Expected Direct: {songsCountDirect} Calculated: {totalCount} (Songs: {songsCount} SS: {scoreSaberCount} BL: {beatLeaderCount})");

            //Clear the libraries and reset links
            UIDStringToSong.Clear();
            UIDStringToSongID.Clear();
            //**Delete once removed**
            songs.Clear();
            foreach (var song in tempSongs)
            {
                SetLibraryLink(song);
            }

            //Update the values of expected and found
            songsCount = UIDStringToSong.Select(c => c.Value).Distinct().Count();
            scoreSaberCount = UIDStringToSong.Select(c => c.Value).Distinct().Where(c => !string.IsNullOrEmpty(c.scoreSaberID)).Count();
            beatLeaderCount = UIDStringToSong.Select(c => c.Value).Distinct().Where(c => !string.IsNullOrEmpty(c.beatLeaderID)).Count();
            songsCountDirect = UIDStringToSong.Count;
            totalCount = songsCount + scoreSaberCount + beatLeaderCount;
            songSuggest.log?.WriteLine($"Reset Library Link Found Direct: {songsCountDirect} Calculated: {totalCount} (Songs: {songsCount} SS: {scoreSaberCount} BL: {beatLeaderCount})");
        }

        //Sets all ID links for a specific Song (And prepares the primary songlink)
        public InternalID SetLibraryLink(Song song)
        {
            string internalIDString = Song.GetInternalID(song.characteristic, song.difficulty, song.hash);
            InternalID internalID = new InternalID { Value = internalIDString };
            string uniqueInternalIDString = internalID.UniqueID;
            UIDStringToSong[uniqueInternalIDString] = song;
            //Remove later
            songs[uniqueInternalIDString] = song;
            UIDStringToSongID[uniqueInternalIDString] = internalID;

            if (!string.IsNullOrEmpty(song.scoreSaberID))
            {
                ScoreSaberID scoreSaberID = new ScoreSaberID { Value = song.scoreSaberID };
                scoreSaberID.SetSong(song);
                string uniqueScoreSaberIDString = scoreSaberID.UniqueID;
                UIDStringToSong[uniqueScoreSaberIDString] = song;
                //Remove later
                songs[uniqueInternalIDString] = song;
                UIDStringToSongID[uniqueScoreSaberIDString] = scoreSaberID;
            }

            if (!string.IsNullOrEmpty(song.beatLeaderID))
            {
                BeatLeaderID beatLeaderID = new BeatLeaderID { Value = song.beatLeaderID };
                beatLeaderID.SetSong(song);
                string uniqueBeatLeaderIDString = beatLeaderID.UniqueID;
                UIDStringToSong[uniqueBeatLeaderIDString] = song;
                //Remove later
                songs[uniqueInternalIDString] = song;
                UIDStringToSongID[uniqueBeatLeaderIDString] = beatLeaderID;
            }

            return internalID;
        }


        //Used in SongID implicit creation. So should always return an object. Try and return cached version if one is available.
        //Unknown ID creation should be supported.
        public SongID StringIDToSongID(string stringID, SongIDType songIDType)
        {
            _songIDRequest++;

            //Create a new ID of the given type to ensure correct InternalID can be used for cache lookup
            string songIDstring;

            SongID songID;

            //Generate an ID on the given value
            switch (songIDType)
            {
                case SongIDType.Internal:
                    songID = new InternalID() { Value = stringID };
                    break;
                case SongIDType.ScoreSaber:
                    songID = new ScoreSaberID() { Value = stringID };
                    break;
                case SongIDType.BeatLeader:
                    songID = new BeatLeaderID() { Value = stringID };
                    break;
                default:
                    throw new InvalidOperationException("Unhandled SongIDType used");
            }

            //Update to cached version if available.
            songIDstring = songID.UniqueID;
            if (UIDStringToSongID.TryGetValue(songIDstring, out SongID foundSongID)) songID = foundSongID;
                
            return songID;
        }

        public void ShowCache()
        {
            songSuggest.log?.WriteLine($"SongID Cache");
            songSuggest.log?.WriteLine($"Count : {UIDStringToSongID.Count()}");
            songSuggest.log?.WriteLine($"Unique : {UIDStringToSongID.Keys.Distinct().ToList().Count()}");
            songSuggest.log?.WriteLine($"");
            songSuggest.log?.WriteLine($"Song Cache");
            songSuggest.log?.WriteLine($"Count : {UIDStringToSong.Count()}");
            songSuggest.log?.WriteLine($"Unique : {UIDStringToSong.Keys.Distinct().ToList().Count()}");
        }

        int _songIDRequest;
        public int GetSongIDRequests()
        {
            return _songIDRequest;
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

        //Returns the Max "Star" Rating of a given Leaderboard. If unknown leaderboard, "default" is 15 stars, as that seems to match all used boards for now.
        internal double GetMaxRating(LeaderboardType leaderboardType)
        {
            switch(leaderboardType)
            {
                case LeaderboardType.ScoreSaber:
                    return UIDStringToSong.Values.Max(c => c.starScoreSaber);
                case LeaderboardType.AccSaber:
                    return UIDStringToSong.Values.Max(c => c.complexityAccSaber);
                case LeaderboardType.BeatLeader:
                    return UIDStringToSong.Values.Max(c => c.starBeatLeader);
            }

            return 15;
        }
    }
}