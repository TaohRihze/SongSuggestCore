using System;

namespace SongSuggestNS
{
    [Flags]
    public enum SongCategory
    {
        ScoreSaber = 1,          
        AccSaberTrue = 2,
        AccSaberStandard = 4,
        AccSaberTech = 8,
        BeatLeader = 16,
        BrokenDownloads = 1073741824
    }
}
