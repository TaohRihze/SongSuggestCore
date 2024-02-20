using System.Collections.Generic;

namespace PlayerScores
{
    public class BeatLeaderScores
    {
        private static string version = "1.0";
        public List<BeatLeaderPlayerScore> PlayerScores { get; set; } = new List<BeatLeaderPlayerScore>();
        public ScoresMeta ScoresMeta { get; set; } = new ScoresMeta() { Version = version };
        public bool FileVersionMatch()
        {
            return version == ScoresMeta.Version;
        }
    }
}