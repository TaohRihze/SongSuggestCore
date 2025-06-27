using System;
using SongLibraryNS;
using SongSuggestNS;


namespace LinkedData
{
    public class SongLink
    {
        public String playerID { get; set; }
        public Top10kScore originSongScore { get; set; }
        public Top10kScore targetSongScore { get; set; }
        public double distance;

        //---2025-03-17: New Calculations Data---
        public SongEndPoint OriginScoreEndPoint;
        public SongEndPoint TargetScoreEndPoint;
    }
}