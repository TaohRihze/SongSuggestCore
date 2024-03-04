using System;

namespace BanLike
{
    public enum BanType
    {
        Global,    //Banned in the Suggestions
        Oldest,      //Banned in the Oldest Songs
        AccSaber        //Banned from the AccSaber list
    }
    public class SongBan
    {
        public String songName { get; set; }
        public DateTime expire { get; set; }
        public DateTime activated { get; set; }
        public String songID { get; set; }
        public BanType banType { get; set; }
    }
}
