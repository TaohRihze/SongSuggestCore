using System;
using System.Collections.Generic;
using System.Linq;
using SongSuggestNS;
using SongLibraryNS;

namespace BanLike
{
    public class SongBanning
    {
        public SongSuggest songSuggest { get; set; }
        public List<SongBan> bannedSongs = new List<SongBan>();

        public List<SongID> GetBannedIDs()
        {
            return bannedSongs
                .Where(p => p.expire > DateTime.UtcNow)
                .Select(p => (SongID)(InternalID)p.songID)
                .Distinct()
                .ToList();
        }

        //Returns a list of all Permabanned Songs (this is from ALL BanTypes, as they all are set at max expire)
        public List<SongID> GetPermaBannedIDs()
        {
            return bannedSongs.Where(p => p.expire == DateTime.MaxValue).Select(p => (SongID)(InternalID)p.songID).Distinct().ToList();
        }

        public Boolean IsBanned(String songHash, String difficulty)
        {
            SongID songID = SongLibrary.GetID(songHash, difficulty);
            return IsBanned(songID);
        }
        public Boolean IsBanned(SongID songID)
        {
            return IsBanned(songID, BanType.Global);
        }
        public Boolean IsBanned(SongID songID, BanType banType)
        {
            return bannedSongs.Any(p => p.songID == songID.GetSong().internalID && p.expire > DateTime.UtcNow && p.banType == banType);
        }

        public Boolean IsPermaBanned(String songHash, String difficulty)
        {
            return IsPermaBanned(SongLibrary.GetID(songHash, difficulty));
        }
        public Boolean IsPermaBanned(SongID songID)
        {
            return IsPermaBanned(songID, BanType.Global);
        }
        public Boolean IsPermaBanned(SongID songID, BanType banType)
        {
            return bannedSongs.Any(p => p.songID == songID.GetSong().internalID && p.expire == DateTime.MaxValue && p.banType == banType);
        }

        public void LiftBan(String songHash, String difficulty)
        {
            SongID songID = SongLibrary.GetID(songHash, difficulty);
            LiftBan(songID);
        }

        public void LiftBan(SongID songID)
        {
            LiftBan(songID, BanType.Global);
        }

        public void LiftBan(SongID songID, BanType banType)
        {
            bannedSongs.RemoveAll(p => p.songID == songID.GetSong().internalID && p.banType == banType);
            Save();
        }

        public void SetBan(String songHash, String difficulty, int days)
        {
            SongID songID = SongLibrary.GetID(songHash, difficulty);
            SetBan(songID, days);
        }

        public void SetBan(SongID songID, int days)
        {
            //If a ban is in place, remove it before setting the new ban.
            if (IsBanned(songID)) LiftBan(songID);

            bannedSongs.Add(new SongBan
            {
                expire = DateTime.UtcNow.AddDays(days),
                activated = DateTime.UtcNow,
                songID = songID.GetSong().internalID,
                banType = BanType.Global,
                songName = SongLibrary.GetDisplayName(songID)
            });
            Save();
        }

        public void SetPermaBan(String songHash, String difficulty)
        {
            SongID songID = SongLibrary.GetID(songHash, difficulty);
            SetPermaBan(songID);
        }
        public void SetPermaBan(SongID songID)
        {
            SetPermaBan(songID, BanType.Global);
        }
        public void SetPermaBan(SongID songID, BanType banType)
        {
            //If a ban is in place, remove it before setting the new ban.
            if (IsBanned(songID, banType)) LiftBan(songID, banType);
            bannedSongs.Add(new SongBan
            {
                expire = DateTime.MaxValue,
                activated = DateTime.UtcNow,
                songID = songID.GetSong().internalID,
                banType = banType
            });
            Save();
        }

        public DateTime GetBanExpire(SongID songID)
        {
            if (IsBanned(songID))
            {
                return bannedSongs.First(p => p.songID == songID.GetSong().internalID).expire;
            }
            return DateTime.MinValue;
        }

        public void Save()
        {
            var orderedBannedSongs = bannedSongs
                .Where(p => p.expire > DateTime.UtcNow)
                .OrderBy(c => c.songName)
                .ToList();
            songSuggest.fileHandler.SaveBannedSongs(orderedBannedSongs);
        }
    }
}
