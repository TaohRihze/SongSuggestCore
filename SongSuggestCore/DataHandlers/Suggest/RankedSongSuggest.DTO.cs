using System.Collections.Generic;
using LinkedData;

namespace Actions
{
    public partial class RankedSongSuggest
    {
        public class DTO
        {
            public RankedSongSuggest manager;

            public List<string> originSongIDs { get => manager.originSongIDs; }
            public List<string> ignoreSongs { get => manager.ignoreSongs; }
            public Top10kPlayers leaderboard { get => manager.suggestSM.Leaderboard(); }
            public SuggestSourceManager suggestSM { get => manager.suggestSM; }
            public double betterAccCap { get => manager.betterAccCap; }
            public double worseAccCap { get => manager.worseAccCap; }

            public string playerID { get => manager.songSuggest.activePlayerID; }

            internal DTO(RankedSongSuggest manager)
            {
                this.manager = manager;
            }

            public RankedSongSuggest.DTO dto { get => manager.dto; }

            //Workaround for Progress
            public double songSuggestCompletion { get => manager.songSuggestCompletion; set => manager.songSuggestCompletion = value; }

        }
    }
}