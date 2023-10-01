using System;

namespace Settings
{
    public class SongSuggestSettings
    {
        public String scoreSaberID { get; set; }
        [Obsolete("Not Used, will be removed in the future")]
        public int rankFrom { get; set; } = 1;
        [Obsolete("Not Used, will be removed in the future")]
        public int rankTo { get; set; } = 10000;
        public bool ignorePlayedAll { get; set; } = false;
        public int ignorePlayedDays { get; set; } = 60;
        public bool ignoreNonImproveable { get; set; } = false;
        public int requiredMatches { get; set; } = 90;
        public bool useLikedSongs { get; set; } = false;
        public bool fillLikedSongs { get; set; } = true;
        public bool useLocalScores { get; set; } = false;
        public FilterSettings filterSettings {get;set;}
        public PlaylistSettings playlistSettings { get; set; }
        public int extraSongs { get; set; } = 25;
        [Obsolete("Test function, did not work, will be removed in the future")]
        public int skipSongsCount { get; set; } = 0;
        public int playlistLength { get; set; } = 50;
    }
}
