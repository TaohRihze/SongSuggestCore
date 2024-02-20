using System;
using System.Collections.Generic;

namespace PlayerScores
{
    public class ScoreCollection
    {
        private static string _formatVersion = "1.0";
        public List<PlayerScore> PlayerScores { get; set; } = new List<PlayerScore>();
        public ScoresMeta ScoresMeta { get; set; } = new ScoresMeta() { FormatVersion = _formatVersion};
        public bool Validate(String expectedDataVersion)
        {
            if (ScoresMeta.FormatVersion != _formatVersion) return false;
            if (ScoresMeta.DataVersion != expectedDataVersion) return false;
            return true; //Validated
        }
    }
}