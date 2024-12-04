using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongSuggestNS;


namespace SongLibraryNS
{
    public class Song 
    {
        public string name { get; set; }
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
        public string beatSaverID { get; set; }
        public string hash { get; set; }
        public string difficulty { get; set; }
        public string characteristic { get; set; } = "Standard"; //Default might be removed later once all data is updated, for now updating on first load is fine.
        
        //Valid options are these ... not sure if there should be made a validation on this or not.
        //Standard: "Standard" - Regular gameplay with walls, notes, and bombs.
        //OneSaber: "OneSaber" - Designed for one-handed play.
        //NoArrows: "NoArrows" - Notes appear without directional arrows.
        //90Degree: "90Degree" - Rotational gameplay, 90-degree patterns.
        //360Degree: "360Degree" - Full 360-degree rotational patterns.
        //Lightshow: "Lightshow" - No notes, focuses on lighting effects.
        //Lawless: "Lawless" - Maps that do not adhere to standard Beat Saber conventions.
        //Legacy: "Legacy" - ???


        //All star ratings, BL takes a lot, so should likely be refactored later to a list of "rating objects", and those can have a reference name and value. Allowing different
        //Rating structures. Also will support multi rankings in a single leaderboard (e.g. overlapping BL names depending on modifiers).
        public SongCategory songCategory { get; set; }
        
        public double starScoreSaber { get; set; }
        //---BL Ratings
        public double starBeatLeader { get; set; }
        public double starAccBeatLeader { get; set; }
        public double starPassBeatLeader { get; set; }
        public double starTechBeatLeader { get; set; }
        //---
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
