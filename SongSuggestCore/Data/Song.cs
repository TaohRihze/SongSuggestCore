using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongSuggestNS;


namespace SongLibraryNS
{
    public class Song 
    {
        private string _cachedInternalID;
        [JsonIgnore]
        public string internalID //Internal ID
        { 
            get
            {
                if (_cachedInternalID == null)
                {
                    _cachedInternalID = $"{characteristic}-{difficulty}-{hash}".ToUpperInvariant();
                }

                return _cachedInternalID;
            }
        }
        public string scoreSaberID { get; set; }
        public string beatLeaderID { get; set; }
        public string name { get; set; }
        public string hash { get; set; }
        public string difficulty { get; set; }
        public string characteristic { get; set; } = "Standard"; //Default might be removed later once all data is updated, for now updating on first load is fine.
        public SongCategory songCategory { get; set; }
        public double starScoreSaber { get; set; }
        //{
        //    get => starBeatSaber;
        //    set => starBeatSaber = value;
        //}
        //[Obsolete("Use starScoreSaber instead")]
        //public double starBeatSaber { get; set; }
        public double starBeatLeader { get; set; }
        public double complexityAccSaber { get; set; }

        public String GetDifficultyText()
        {
            switch (difficulty)
            {
                case "1":
                    return "Easy";
                case "3":
                    return "Normal";
                case "5":
                    return "Hard";
                case "7":
                    return "Expert";
                case "9":
                    return "ExpertPlus";
                default:
                    return "Easy";
            }
        }

        public String GetCharacteristicText()
        {
            switch (difficulty)
            {
                case "1":
                    return "Easy";
                case "3":
                    return "Normal";
                case "5":
                    return "Hard";
                case "7":
                    return "Expert";
                case "9":
                    return "ExpertPlus";
                default:
                    return "Easy";
            }
        }

        public static String GetDifficultyText(string difficultyValue)
        {
            switch (difficultyValue)
            {
                case "1":
                    return "Easy";
                case "3":
                    return "Normal";
                case "5":
                    return "Hard";
                case "7":
                    return "Expert";
                case "9":
                    return "ExpertPlus";
                default:
                    return "Easy";
            }
        }

        public static string GetDifficultyValue(string difficultyText)
        {
            switch (difficultyText)
            {
                case "Easy":
                    return "1";
                case "Normal":
                    return "3";
                case "Hard":
                    return "5";
                case "Expert":
                    return "7";
                case "ExpertPlus":
                    return "9";
                case "Expert+":
                    return "9";
                default:
                    return "1";
            }
        }
    }
}
