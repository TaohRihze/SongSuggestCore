using BeatLeaderJson;
using ScoreSabersJson;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace BeatLeaderJson
{
    public class LeaderboardClanRankings
    {
        public List<clanRanking> clanRanking { get; set; }
        public Clan clan { get; set; }
        public string id { get; set; }
        public double pp { get; set; }

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

    //Reduced data holders for getting a list of Clan Members ... more information can be added if needed.
    public class ClanMembersList
    {
        public Metadata metadata { get; set; }
        public List<ClanMember> data {get;set;}
    }

    public class ClanMember
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}