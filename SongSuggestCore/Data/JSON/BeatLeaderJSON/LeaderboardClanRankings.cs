using System.Collections.Generic;

namespace BeatLeaderJson
{
    public class LeaderboardClanRankings
    {
        public List<clanRanking> clanRanking { get; set; }
        public Clan clan { get; set; }
        public string id{ get; set; }

    }

    public class clanRanking
    {
        public double pp { get; set; }
        public int totalScore { get; set; }
    }

    public class Leaderboard
    {
        public string id { get; set; }
        public List<Score> scores { get; set; }
    }

    public class Clan
    {
        public string tag { get; set; }
    }
}