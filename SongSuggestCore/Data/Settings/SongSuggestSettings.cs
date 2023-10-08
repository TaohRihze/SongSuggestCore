using System;
using Actions;

namespace Settings
{
    public class SongSuggestSettings
    {



        //public String scoreSaberID { get; set; }
        //[Obsolete("Not Used, will be removed in the future")]
        //public int rankFrom { get; set; } = 1;
        //[Obsolete("Not Used, will be removed in the future")]
        //public int rankTo { get; set; } = 10000;
        //public bool ignorePlayedAll { get; set; } = false;
        //public int ignorePlayedDays { get; set; } = 60;
        //public bool ignoreNonImproveable { get; set; } = false;
        //public int requiredMatches { get; set; } = 90;
        //public bool useLikedSongs { get; set; } = false;
        //public bool fillLikedSongs { get; set; } = true;
        //public bool useLocalScores { get; set; } = false;
        //public FilterSettings filterSettings {get;set;}
        //public PlaylistSettings playlistSettings { get; set; }
        //public int extraSongs { get; set; } = 25;
        //[Obsolete("Test function, did not work, will be removed in the future")]
        //public int skipSongsCount { get; set; } = 0;
        //public int playlistLength { get; set; } = 50;

        public String ScoreSaberID { get; set; }
        public bool IgnorePlayedAll { get; set; } = false;
        public int IgnorePlayedDays { get; set; } = 60;
        public bool IgnoreNonImproveable { get; set; } = false;
        public int RequiredMatches { get; set; } = 90;
        public bool UseLikedSongs { get; set; } = false;
        public bool FillLikedSongs { get; set; } = true;
        public bool UseLocalScores { get; set; } = false;
        public int ExtraSongs { get; set; } = 25;
        public int PlaylistLength { get; set; } = 50;
        //Set how many times more pp a person may have be performed on a song before their songs are ignored.
        //1.2 = 120%, 1.1 = 110% etc.
        //1.2 seems to be a good value to cut off low/high acc linkage, while still allowing a player room for growth suggestions
        //Lowered to 10% difference as with flatter cuver and closer new score selection 20% is a large PP jump to search up.
        //WorseAcc Cap can become relevant for filtering once players starts reaching very high acc range. 0.7 should limit impact of filter.
        public double BetterAccCap { get; set; } = 1.2;
        public double WorseAccCap { get; set; } = 0.7;
        public LeaderboardType Leaderboard { get; set; } = LeaderboardType.ScoreSaber;
        public int OriginSongCount { get; set; } = 50;
        public FilterSettings FilterSettings { get; set; }
        public PlaylistSettings PlaylistSettings { get; set; }

        ////To be deleted once UI updated.
        //[Obsolete("Use PascalCase")]
        //public string scoreSaberID { get => ScoreSaberID; set => ScoreSaberID = value; }
        [Obsolete("Not Used, will be removed in the future")]
        public int rankFrom { get; set; } = 1;
        [Obsolete("Not Used, will be removed in the future")]
        public int rankTo { get; set; } = 10000;
        //[Obsolete("Use PascalCase")]
        //public bool ignorePlayedAll { get => IgnorePlayedAll; set => IgnorePlayedAll = value; }
        //[Obsolete("Use PascalCase")]
        //public int ignorePlayedDays { get => IgnorePlayedDays; set => IgnorePlayedDays = value; }
        //[Obsolete("Use PascalCase")]
        //public bool ignoreNonImproveable { get => IgnoreNonImproveable; set => IgnoreNonImproveable = value; }
        //[Obsolete("Use PascalCase")]
        //public int requiredMatches { get => RequiredMatches; set => RequiredMatches = value; }
        //[Obsolete("Use PascalCase")]
        //public bool useLikedSongs { get => UseLikedSongs; set => UseLikedSongs = value; }
        //[Obsolete("Use PascalCase")]
        //public bool fillLikedSongs { get => FillLikedSongs; set => FillLikedSongs = value; }
        //[Obsolete("Use PascalCase")]
        //public bool useLocalScores { get => UseLocalScores; set => UseLocalScores = value; }
        //[Obsolete("Use PascalCase")]
        //public FilterSettings filterSettings { get => FilterSettings; set => FilterSettings = value; }
        //[Obsolete("Use PascalCase")]
        //public PlaylistSettings playlistSettings { get => PlaylistSettings; set => PlaylistSettings = value; }
        //[Obsolete("Use PascalCase")]
        //public int extraSongs { get => ExtraSongs; set => ExtraSongs = value; }
        [Obsolete("Not Used, will be removed in the future")] public int skipSongsCount { get; set; } = 0;
        //public int playlistLength { get => PlaylistLength; set => PlaylistLength = value; }
    }
}
