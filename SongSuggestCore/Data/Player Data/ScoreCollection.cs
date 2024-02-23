using System;
using System.Collections.Generic;
using SongSuggestNS;

namespace PlayerScores
{
    public class ScoreCollection
    {
        private static string _formatVersion = "1.0";
        public List<PlayerScore> PlayerScores { get; set; } = new List<PlayerScore>();
        public ScoresMeta ScoresMeta { get; set; } = new ScoresMeta() { FormatVersion = _formatVersion};
        public bool Validate(String expectedDataVersion)
        {
            SongSuggest.Log?.WriteLine($"ScoresMeta.FormatVersion({ScoresMeta.FormatVersion}) vs _formatVersion({_formatVersion})");
            if (ScoresMeta.FormatVersion != _formatVersion) return false;
            SongSuggest.Log?.WriteLine($"ScoresMeta.DataVersion({ScoresMeta.DataVersion}) vs expectedDataVersion({expectedDataVersion})");
            if (ScoresMeta.DataVersion != expectedDataVersion) return false;
            return true; //Validated
        }
    }
}