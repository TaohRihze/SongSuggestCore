using System;
using System.Collections.Generic;
using System.Linq;
using SongLibraryNS;

namespace SongSuggestNS
{
    public class LastRankedSuggestions
    {
        public SongSuggest songSuggest { get; set; }
        private Dictionary<SongID, int> lastSuggestions = new Dictionary<SongID, int>();
        private bool shouldSave = true;

        public void Load()
        {
            List<SongID> songIDs = songSuggest.fileHandler.LoadRankedSuggestions()
                .Select(suggestion => suggestion.Contains("-") ? (SongID)(InternalID)suggestion : (SongID)(ScoreSaberID)suggestion)
                .ToList();

            songSuggest.log?.WriteLine($"Last Suggestion Loaded Count: {songIDs.Count}");

            shouldSave = false;
            SetSuggestions(songIDs);
        }

        public void SetSuggestions(List<SongID> songIDs)
        {
            lastSuggestions.Clear();
            int rank = 1;
            foreach (var suggestion in songIDs)
            {
                lastSuggestions.Add(suggestion, rank++);
            }
            if (shouldSave) Save();
            shouldSave = true;
        }

        //Saves any Id via the internalID.
        public void Save()
        {
            songSuggest.fileHandler.SaveRankedSuggestions(Suggestions());
        }

        public List<String> Suggestions()
        {
            List<String> saveSuggestions = lastSuggestions
                .OrderBy(c => c.Value)
                .Select(c => c.Key.GetSong().internalID)
                .ToList();
            return (saveSuggestions);
        }

        public String GetRank(SongID songID)
        {
            return lastSuggestions.ContainsKey(songID) ? $"{lastSuggestions[songID]}" : "";
        }

        public String GetRankCount()
        {
            return "" + lastSuggestions.Count;
        }
    }
}
