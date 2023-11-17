using System;
using Newtonsoft.Json;
using SongSuggestNS;


namespace SongLibraryNS
{
    public class Song 
    {
        public String scoreSaberID { get; set; }
        public String beatLeaderID { get; set; }
        public String name { get; set; }
        public String hash { get; set; }
        public String difficulty { get; set; }
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
