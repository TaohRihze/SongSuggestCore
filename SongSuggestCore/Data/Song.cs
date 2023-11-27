using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongSuggestNS;


namespace SongLibraryNS
{
    public class Song 
    {
        private string _songID;
        [JsonIgnore]
        public string songID
        { 
            get
            {
                if (_songID == null)
                {
                    _songID = $"id{characteristic}-{difficulty}-{hash}";
                }
                return _songID;
            }
        }
        public string scoreSaberID { get; set; }
        public string beatLeaderID { get; set; }
        public string name { get; set; }
        public string hash { get; set; }
        public string difficulty { get; set; }
        public string characteristic { get; set; } = "Standard"; //Default might be removed later once all data is updated, for now updating on first load is fine.
        public SongCategory songCategory { get; set; }
        public double starScoreSaber
        {
            get => starBeatSaber;
            set => starBeatSaber = value;
        }
        [Obsolete("Use starScoreSaber instead")]
        public double starBeatSaber { get; set; }
        public double starBeatLeader { get; set; }
        public double complexityAccSaber { get; set; }

        public Song()
        {
        }

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
