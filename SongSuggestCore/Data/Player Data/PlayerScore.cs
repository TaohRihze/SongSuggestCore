using Newtonsoft.Json;
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
        [JsonIgnore]
        public double SourceRankPercentile { get => (double)SourceRank / SourcePlays; }
        public int SourceRank { get; set; } //Cached Rank on map on the Source Location
        public int SourcePlays { get; set; } = 1; //Cached Total plays on the Source Location. To avoid divide by 0, and the player has a score, we can assume at least 1 play.
    }
}