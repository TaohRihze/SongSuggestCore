using System.Collections.Generic;

namespace BeatLeaderJson
{
    public class ScoresCompact
    {
        public Metadata metadata { get; set; }
        public List<DataItem> data { get; set; }
    }

    public class Metadata
    {
        public int itemsPerPage { get; set; }
        public int page { get; set; }
        public int total { get; set; }
    }

    public class Score
    {
        public int id { get; set; }
        public int baseScore { get; set; }
        public int modifiedScore { get; set; }
        public string modifiers { get; set; }
        public int maxCombo { get; set; }
        public int missedNotes { get; set; }
        public int badCuts { get; set; }
        public int hmd { get; set; }
        public int controller { get; set; }
        public float accuracy { get; set; }
        public float pp { get; set; }
        public int epochTime { get; set; }
    }

    public class LeaderboardCompact
    {
        public string id { get; set; }
        public string songHash { get; set; }
        public string modeName { get; set; }
        public int difficulty { get; set; }
    }

    public class DataItem
    {
        public Score score { get; set; }
        public LeaderboardCompact leaderboard { get; set; }
    }
}