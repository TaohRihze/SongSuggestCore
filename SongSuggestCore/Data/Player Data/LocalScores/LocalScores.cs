using System.Collections.Generic;

namespace PlayerScores
{
    public class LocalScores
    {
        private static string version = "1.0";
        public List<LocalPlayerScore> PlayerScores { get; set; } = new List<LocalPlayerScore>();
        public ScoresMeta ScoresMeta { get; set; } = new ScoresMeta() { Version = version};
    }
}