namespace SongLibraryNS
{
    public abstract class SongID
    {
        // Abstract property to represent the prefix
        abstract public string Prefix { get; }
        public string Value;
        public string UniqueID => $"{Prefix}{Value}".ToUpperInvariant();

        protected Song _cachedSong;

        protected SongID() { }

        public Song GetSong()
        {
            //Return cached song. If no song is cached attempt to retrieve it from the library, store value and return found value (can still be null if unknown in Library).
            return _cachedSong ?? (_cachedSong = SongLibrary.SongIDToSong(this));
            //return SongLibrary.SongIDToSong(this);
        }

        //For the Song Library to set the primary song in the _cachedSong
        internal void SetSong(Song song)
        {
            _cachedSong = song;
        }

        public SongID(string value)
        {
            Value = value;
        }
        public static implicit operator string(SongID id) => id.Value;

        //Allow comparison between songID objects.
        public override bool Equals(object obj)
        {
            // If same object type, just compare directly on value
            if (obj is SongID songID)
            {
                if (this.Prefix == songID.Prefix)
                {
                    return this.UniqueID == songID.UniqueID;
                }
                return GetSong() == songID.GetSong();
                
                //return SongLibrary.Compare(this, songId);
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
            var relatedObject = GetSong();
            if (relatedObject == null)
            {
                //Possible add this to log in the future, via Static SongSuggest log.
                //Console.WriteLine($"{UniqueString} not found in SongLibrary");
                return base.GetHashCode(); //For some reason the song is not in the library, so we just return some random hash value that is unlinked
            }
            return relatedObject.GetHashCode();
        }
    }

    //ID in the internal format (Generated automatic from a Song object based on Hash, Difficulty, Characteristic)
    //Offers a Static get UID as well for SongLibrary
    public class InternalID : SongID
    {
        public override string Prefix => "ID";                                                              //Unique Prefix for the ID
        //public static implicit operator InternalID(string value) => new InternalID { Value = value };       //Creation from String
        public static implicit operator InternalID(string value) => (InternalID)SongLibrary.StringIDToSongID(value, SongIDType.Internal);       //Creation from String

        //For Song Library to get the UID string for a given song.
        internal static string GetUID(string ID)
        {
            return $"ID{ID}".ToUpperInvariant();
        }
    }

    //BeatLeader SongID
    public class BeatLeaderID : SongID
    {
        public override string Prefix => "BL";
        //public static implicit operator BeatLeaderID(string value) => new BeatLeaderID { Value = value };
        public static implicit operator BeatLeaderID(string value) => (BeatLeaderID)SongLibrary.StringIDToSongID(value, SongIDType.BeatLeader);       //Creation from String
    }

    //ScoreSaber SongID
    public class ScoreSaberID : SongID
    {
        public override string Prefix => "SS";
        //public static implicit operator ScoreSaberID(string value) => new ScoreSaberID { Value = value };
        public static implicit operator ScoreSaberID(string value) => (ScoreSaberID)SongLibrary.StringIDToSongID(value, SongIDType.ScoreSaber);       //Creation from String
    }
}