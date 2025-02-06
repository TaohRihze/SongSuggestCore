using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SongSuggestNS;
using SongLibraryNS;
using Newtonsoft.Json.Converters;

namespace LinkedData
{
    public class Top10kPlayers
    {
        public const String FormatVersion = "1.0";
        public SongSuggest songSuggest { get; set; }
        public String FormatName { get; set; }
        public List<Top10kPlayer> top10kPlayers = new List<Top10kPlayer>();
        public SortedDictionary<String, Top10kSongMeta> top10kSongMeta = new SortedDictionary<String, Top10kSongMeta>();

        public void Save()
        {
            Save("Top10KPlayers");
        }

        public void Save(string leaderboardName)
        {
            songSuggest.fileHandler.SaveScoreBoard(top10kPlayers, leaderboardName);
        }

        public void Load(string scoreBoardName)
        {
            top10kPlayers = songSuggest.fileHandler.LoadScoreBoard(scoreBoardName);
            GenerateTop10kSongMeta();
            SetParentLinks();
        }

        public void SetParentLinks()
        {
            foreach (var parent in top10kPlayers)
            {
                foreach (var song in parent.top10kScore)
                {
                    song.parent = parent;
                }
            }
        }

        public void GenerateTop10kSongMeta()
        {
            foreach (Top10kPlayer player in top10kPlayers)
            {
                foreach (Top10kScore score in player.top10kScore)
                {
                    //Add any missing songs.
                    if (!top10kSongMeta.ContainsKey(score.songID))
                    {
                        top10kSongMeta.Add(score.songID, new Top10kSongMeta { songID = score.songID });
                    }
                    Top10kSongMeta songMeta = top10kSongMeta[score.songID];
                    songMeta.count++;
                    songMeta.totalRank += score.rank;
                    songMeta.maxScore = Math.Max(songMeta.maxScore, score.pp);
                    songMeta.minScore = Math.Min(songMeta.minScore, score.pp);
                    songMeta.totalScore += score.pp;
                }
            }

            //set average for localvsglobal PP values
            foreach (Top10kSongMeta songMeta in top10kSongMeta.Values)
            {
                songMeta.averageScore = songMeta.totalScore / songMeta.count;
            }
            songSuggest.log?.WriteLine($"*Total Songs*: {top10kSongMeta.Count} in {FormatName}");
        }

        public void Add(String id, String name, int rank)
        {
            Top10kPlayer newPlayer = new Top10kPlayer();
            newPlayer.id = id;
            newPlayer.name = name;
            newPlayer.rank = rank;
            top10kPlayers.Add(newPlayer);
        }
    }

    public class BeatLeaderPlayersLD : Top10kPlayers
    {
        new public List<BeatLeaderPlayerLD> top10kPlayers = new List<BeatLeaderPlayerLD>();
        new public void Load(string scoreBoardName)
        {
            top10kPlayers = songSuggest.fileHandler.LoadScoreBoard<BeatLeaderPlayerLD>(scoreBoardName);
            GenerateTop10kSongMeta();
        }

        new public void Save(string leaderboardName)
        {
            songSuggest.fileHandler.SaveScoreBoard(top10kPlayers, leaderboardName);
        }
    }

    public class BeatLeaderPlayerLD : Top10kPlayer
    {
        new public List<BeatLeaderScoreLD> top10kScore = new List<BeatLeaderScoreLD>();
    }

    public class BeatLeaderScoreLD : Top10kScore
    {
        [JsonIgnore]
        new public BeatLeaderID songID { get; set; }

        [JsonProperty("songID")]
        public string _songID { get => songID; set => songID = value; }
    }


    public class ScoreSaberPlayersLD : Top10kPlayers
    {
        new public List<ScoreSaberPlayerLD> top10kPlayers = new List<ScoreSaberPlayerLD>();
        new public void Load(string scoreBoardName)
        {
            top10kPlayers = songSuggest.fileHandler.LoadScoreBoard<ScoreSaberPlayerLD>(scoreBoardName);
            GenerateTop10kSongMeta();
        }

        new public void Save(string leaderboardName)
        {
            songSuggest.fileHandler.SaveScoreBoard(top10kPlayers, leaderboardName);
        }
    }

    public class ScoreSaberPlayerLD : Top10kPlayer
    {
        new public List<ScoreSaberScoreLD> top10kScore = new List<ScoreSaberScoreLD>();
    }

    public class ScoreSaberScoreLD : Top10kScore
    {
        [JsonIgnore]
        new public ScoreSaberID songID { get; set; }

        [JsonProperty("songID")]
        public string _songID { get => songID; set => songID = value; }
    }
}