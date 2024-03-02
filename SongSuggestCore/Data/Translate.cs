using System.Collections.Generic;

namespace SongSuggestNS
{
    public static class Translate
    {
        internal static readonly Dictionary<string, string> SongCategoryDictionary = new Dictionary<string, string>();
        static Translate()
        {
            SongCategoryDictionary.Add($"{SongCategory.ScoreSaber}Label", "Score Saber");
            SongCategoryDictionary.Add($"{SongCategory.ScoreSaber}Hover", "Ranked Score Saber Songs");
            SongCategoryDictionary.Add($"{SongCategory.AccSaberTrue}Label", "AccSaber - True");
            SongCategoryDictionary.Add($"{SongCategory.AccSaberTrue}Hover", "AccSabers True Acc Leaderboard");
            SongCategoryDictionary.Add($"{SongCategory.AccSaberStandard}Label", "AccSaber - Standard");
            SongCategoryDictionary.Add($"{SongCategory.AccSaberStandard}Hover", "AccSabers Standard Acc Leaderboard");
            SongCategoryDictionary.Add($"{SongCategory.AccSaberTech}Label", "AccSaber - Tech");
            SongCategoryDictionary.Add($"{SongCategory.AccSaberTech}Hover", "AccSabers Tech Acc Leaderboard");
            SongCategoryDictionary.Add($"{SongCategory.BrokenDownloads}Label", "Broken Downloads");
            SongCategoryDictionary.Add($"{SongCategory.BrokenDownloads}Hover", "Songs that may break in download for various reasons. Turn on if you do not mind a Missing Download icon and/or have the songs already.");
            SongCategoryDictionary.Add($"{SongCategory.BeatLeader}Label", "Beat Leader");
            SongCategoryDictionary.Add($"{SongCategory.BeatLeader}Hover", "Ranked Beat Leader Songs");
        }
    }
}
