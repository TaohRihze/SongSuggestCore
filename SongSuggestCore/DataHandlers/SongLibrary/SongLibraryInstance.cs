using ScoreSabersJson;
using SongSuggestNS;
using System;
using System.Collections.Generic;
using System.Linq;
using BeatLeaderJson;
using FileHandling;

namespace SongLibraryNS
{
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

        public Dictionary<String, Song> songs = new Dictionary<String, Song>();

        //Sets this library as the active library
        public void SetActive() { SongLibrary.SetAsActiveLibrary(this); }

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
            //Create new basic song to extract the internal ID from
            Song internalSong = new Song()
            {
                hash = song.hash.ToUpperInvariant(),
                difficulty = Song.GetDifficultyValue(song.difficulty),
                name = song.name
            };

            //If it is known replace target song with already known song
            Song foundSong = SongIDToSong((InternalID)internalSong.internalID);
            if (foundSong != null) internalSong = foundSong;


            //Set or Update remaining generic info
            internalSong.beatLeaderID = song.id;
            internalSong.starBeatLeader = song.stars;
            internalSong.starAccBeatLeader = song.accRating;
            internalSong.starPassBeatLeader = song.passRating;
            internalSong.starTechBeatLeader = song.techRating;

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
            //We have to generate a new song to get the InternalID
            Song internalSong = new Song()
            {
                hash = song.leaderboard.songHash.ToUpperInvariant(),
                difficulty = $"{song.leaderboard.difficulty.difficulty}",
                name = song.leaderboard.songName
            };

            //replace internal song if there is a library song
            internalSong = SongIDToSong((InternalID)internalSong.internalID) ?? internalSong;

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
                string songIDString = song.internalID;
                if (songID is BeatLeaderID) songIDString = song.beatLeaderID;
                if (songID is ScoreSaberID) songIDString = song.scoreSaberID;

                return $"{song.name} ({song.GetDifficultyText()} - {songIDString})";
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
        public SongID GetID(string hash, String difficulty)
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
            return (InternalID)foundSong.internalID;
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
        //**Resets the libraries update time to 0 for Beat Leader as Beat Leader ID's may be removed by this**
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

            //Reset the update time on Beat Leader Songs as they are no longer valid (Leaderboard File is not impacted be so no update there)
            //**This is a temporary workaround along with the ScoreSaberID Null
            songSuggest.filesMeta.beatLeaderSongsUpdated = 0;
            songSuggest.fileHandler.SaveFilesMeta(songSuggest.filesMeta);
            _CachedSongIDs.Clear(); //Songs may have been removed, so we should not hand out incorrect SongID links moving forward.
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

        //Return a list of songs that is found.
        public List<Song> SongIDToSong(List<SongID> songIDs)
        {
            List<Song> returnSongs = new List<Song>();
            foreach (var songID in songIDs)
            {
                if (songs.TryGetValue(songID.UniqueString, out Song song)) returnSongs.Add(song);
            }
            return returnSongs;
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
                .Select(c => (SongID)(InternalID)c.internalID)
                .ToList();

            return rankedSongIDs;
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
            songs[((InternalID)song.internalID).UniqueString] = song;
            if (!string.IsNullOrEmpty(song.scoreSaberID)) songs[((ScoreSaberID)song.scoreSaberID).UniqueString] = song;
            if (!string.IsNullOrEmpty(song.beatLeaderID)) songs[((BeatLeaderID)song.beatLeaderID).UniqueString] = song;
        }


        private Dictionary<string, SongID> _CachedSongIDs = new Dictionary<string, SongID>();
        //Conversion of a String ID to a wanted SongIDType
        public SongID StringIDToSongID(string stringID, SongIDType songIDType)
        {
            _songIDRequest++;

            SongID songID;
            switch (songIDType)
            {
                case SongIDType.Internal:
                    //songID = (InternalID)stringID;
                    songID = new InternalID() {Value = stringID};
                    break;
                case SongIDType.ScoreSaber:
                    //songID = (ScoreSaberID)stringID;
                    songID = new ScoreSaberID() { Value = stringID };
                    break;
                case SongIDType.BeatLeader:
                    //songID = (BeatLeaderID)stringID;
                    songID = new BeatLeaderID() { Value = stringID };
                    break;
                default:
                    throw new InvalidOperationException("Unhandled SongIDType used");
            }

            // Retrieve cache if available.
            if (_CachedSongIDs.TryGetValue(songID.UniqueString, out SongID foundSong))
                songID = foundSong;

            // Assign/Reassign correct value to Cache
            _CachedSongIDs[songID.UniqueString] = songID;

            return songID;
        }

        public void ShowCache()
        {
            songSuggest.log?.WriteLine($"SongID Cache");
            songSuggest.log?.WriteLine($"Count : {_CachedSongIDs.Count()}");
            songSuggest.log?.WriteLine($"Unique : {_CachedSongIDs.Keys.Distinct().ToList().Count()}");
            songSuggest.log?.WriteLine($"");
            songSuggest.log?.WriteLine($"Song Cache");
            songSuggest.log?.WriteLine($"Count : {songs.Count()}");
            songSuggest.log?.WriteLine($"Unique : {songs.Keys.Distinct().ToList().Count()}");
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

        ////Compares 2 SongID's if they are referencing the same object. Returns false if either object is missing.
        //internal bool Compare(SongID id1, SongID id2)
        //{
        //    Song song1;
        //    Song song2;   
        //    if (!songs.TryGetValue(id1.UniqueString, out song1)) return false;
        //    if (!songs.TryGetValue(id2.UniqueString, out song2)) return false;
        //    return song1 == song2;
        //}
    }
}