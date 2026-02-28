using Newtonsoft.Json;
using SongSuggestNS;
using System;

namespace PlayerScores
{
    public class PlayerScore
    {
        public String SongName { get; set; } //Songs name, only used for viewing json
        public String SongID { get; set; } //ID of the song, Internal ID recommended.
        public DateTime TimeSet { get; set; }
        public float RatedScore { get; set; } //Cached PP value from Source Location
        public double Accuracy { get; set; }

        private string _modifiers;

        public string Modifiers //Raw received text string from UI's ... Needs to be run through parser to get "relevant modifiers". Saved as is for future use. Sources are BL leaderboard, and Client
        {
            get => _modifiers;
            set
            {
                _modifiers = value;
                ParsedModifiers = ModifierParser.Parse(value);
            }
        }

        [JsonIgnore]
        public SongModifier ParsedModifiers { get; set; }
        [JsonIgnore]
        public double SourceRankPercentile { get => (double)SourceRank / SourcePlays; }
        public int SourceRank { get; set; } //Cached Rank on map on the Source Location
        public int SourcePlays { get; set; } = 1; //Cached Total plays on the Source Location. To avoid divide by 0, and the player has a score, we can assume at least 1 play.
    }
}