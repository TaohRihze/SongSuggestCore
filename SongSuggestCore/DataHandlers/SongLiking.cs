using System;
using System.Collections.Generic;
using System.Linq;
using SongSuggestNS;
using SongLibraryNS;

namespace BanLike
{
    public class SongLiking
    {
        public SongSuggest songSuggest { get; set; }
        
        public List<SongLike> likedSongs = new List<SongLike>();

        public List<SongID> GetLikedIDs()
        {
            return likedSongs.Select(p => (SongID)(InternalID)p.songID).ToList();
        }

        //Returns true if Liked
        public Boolean IsLiked(String songHash, String difficulty)
        {
            SongID songID = SongLibrary.GetID(songHash, difficulty);
            return IsLiked(songID);
        }

        public Boolean IsLiked(SongID songID)
        {
            return likedSongs.Any(p => p.songID == songID.GetSong().internalID);
        }

        public void RemoveLike(String songHash, String difficulty)
        {
            SongID songID = SongLibrary.GetID(songHash, difficulty);
            RemoveLike(songID);
        }
        public void RemoveLike(SongID songID)
        {
            likedSongs.RemoveAll(p => p.songID == songID.GetSong().internalID);
            Save();
        }

        public void SetLike(String songHash, String difficulty)
        {
            SongID songID = SongLibrary.GetID(songHash, difficulty);
            SetLike(songID);
        }
        public void SetLike(SongID songID)
        {
            //If a Like is in place, remove it before setting the new Like.
            if (IsLiked(songID)) RemoveLike(songID);
            likedSongs.Add(new SongLike { 
                activated = DateTime.UtcNow, 
                songID = songID.GetSong().internalID,
                songName = SongLibrary.GetDisplayName(songID)
            });
            Save();
        }

        public void Save()
        {
            var orderedLikedSongs = likedSongs
                .OrderBy(c => c.songName)
                .ToList();
            songSuggest.fileHandler.SaveLikedSongs(orderedLikedSongs);
        }
    }
}
