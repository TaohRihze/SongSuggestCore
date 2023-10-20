using System;
using Actions;
using SongSuggestNS;

namespace Settings
{
    public class SongSuggestSettings
    {
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
        public SongCategory AccSaberPlaylistCategories { get; set; } = SongCategory.AccSaberStandard | SongCategory.AccSaberTrue | SongCategory.AccSaberTech;

    }
}
