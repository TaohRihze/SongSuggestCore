namespace SongLibraryNS
{
    public abstract class SongID
    {
        // Abstract property to represent the prefix
        abstract public string Prefix { get; }
        public string Value;
        public string UniqueString => $"{Prefix}{Value}".ToUpperInvariant();

        protected SongID() { }

        public Song GetSong()
        {
            return SongLibrary.SongIDToSong(this);
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
            if (obj is SongID songId)
            {
                if (this.Prefix == songId.Prefix)
                {
                    return this.UniqueString == songId.UniqueString;
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
            var relatedObject = SongLibrary.SongIDToSong(this);
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
}