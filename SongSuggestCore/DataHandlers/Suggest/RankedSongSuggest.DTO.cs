using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinkedData;
using SongLibraryNS;
using SongSuggestNS;

namespace Actions
{
    public partial class RankedSongSuggest
    {
        public class DTO
        {
            private RankedSongSuggest manager;
            internal DTO(RankedSongSuggest manager) {this.manager = manager;}


            //Manager DTO Values
            public List<SongID> originSongIDs { get => manager.originSongIDs; }
            public List<SongID> fillerSongs { get => manager.GetFillerSongs(); }
            public List<SongID> playedOriginSongs { get => manager.SelectPlayedOriginSongs(); }
            public List<SongID> ignoreSongs { get => manager.ignoreSongs; }
            public Top10kPlayers leaderboard { get => manager.suggestSM.Leaderboard(); }
            public SuggestSourceManager suggestSM { get => manager.suggestSM; }
            public string playerID { get => manager.songSuggest.activePlayerID; }
            public int targetFillers { get => manager.targetFillers; }


            //SongSuggest DTO Values
            public TextWriter log { get => manager.songSuggest.log; }

            //Settings DTO Values
            //public double betterAccCap { get => manager.settings.BetterAccCap; }
            //public double worseAccCap { get => manager.settings.WorseAccCap; }
            public double LinkKeepPercent { get => manager.LinkKeepPercent; }


            public bool useLikedSongs { get => manager.settings.UseLikedSongs; }
            public bool fillLikedSongs { get => manager.settings.FillLikedSongs; }
            public int originSongsCount { get => manager.settings.OriginSongCount; }


            //Workaround for Progress
            public double songSuggestCompletion { get => manager.songSuggestCompletion; set => manager.songSuggestCompletion = value; }

        }
    }
}