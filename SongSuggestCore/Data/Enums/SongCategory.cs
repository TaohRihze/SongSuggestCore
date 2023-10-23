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
        BrokenDownloads = 16          
    }
}
