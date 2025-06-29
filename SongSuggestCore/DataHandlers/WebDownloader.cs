﻿using System;
using System.Net;
using ScoreSabersJson;
using BeatSaverJson;
using BeatLeaderJson;
using AccSaberJson;
using Newtonsoft.Json;
using SongSuggestNS;
using Data;
using System.Collections.Generic;
using SongLibraryNS;
using LinkedData;
using System.Text;
using Settings;
using PlaylistJson;
using Newtonsoft.Json.Linq;

namespace WebDownloading
{
    public class WebDownloader
    {
        public SongSuggest songSuggest { get; set; }

        private WebClient client = new WebClient();
        private JsonSerializerSettings serializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        //Throttlers
        private Throttler _ScoreSaberThrottler = new Throttler() {callPerPeriod = 100, callPeriodSeconds = 15 }; //400/60
        private Throttler _BeatLeaderThrottler = new Throttler() {callPerPeriod = 19, callPeriodSeconds = 4 }; //50/10

        public WebDownloader()
        {
            //Adding Tls12 to allowed protocols to be able to download from the GIT.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            client.Encoding = Encoding.UTF8;
        }

        //Generic web puller for scores (starts page 1, page 0 and 1 gives same results)
        public PlayerScoreCollection GetScoreSaberPlayerScores(String playerID, String sorting, int count, int page)
        {
            try
            {
                _ScoreSaberThrottler.Call();
                //https://scoresaber.com/api/player/76561197993806676/scores?limit=20&sort=recent&page=2
                String scoresJSON = client.DownloadString("https://scoresaber.com/api/player/" + playerID + "/scores?limit=" + count + "&sort=" + sorting + "&page=" + page);
                return JsonConvert.DeserializeObject<PlayerScoreCollection>(scoresJSON, serializerSettings);
            }
            catch (Exception ex)
            {
                string path = $"https://scoresaber.com/api/player/{playerID}/scores?limit={count}&sort={sorting}&page={page}";
                songSuggest.log?.WriteLine($"Error on user: {playerID} page: {page}\nPath:{path}\n{ex.Message}");
            }
            return new PlayerScoreCollection();
        }

        //Generic web puller for top players (starts page 1, page 0 and 1 gives same results)
        public PlayerCollection GetPlayers(int page)
        {
            try
            {
                _ScoreSaberThrottler.Call();
                //https://scoresaber.com/api/players?page=2
                String playersJSON = client.DownloadString("https://scoresaber.com/api/players?page=" + page);
                return JsonConvert.DeserializeObject<PlayerCollection>(playersJSON, serializerSettings);

            }
            catch
            {
                songSuggest.log?.WriteLine("Error on " + page);
            }
            return new PlayerCollection();
        }

        //Generic web puller for song leaderboards general data via Hash and Difficulty
        public LeaderboardInfo GetLeaderboardInfo(String hash, String difficulty)
        {
            try
            {
                _ScoreSaberThrottler.Call();
                //https://scoresaber.com/api/leaderboard/by-hash/E42BCDF50EA1F961CB8CEFE502E82806866F6479/info?difficulty=9&gameMode=SoloStandard
                String webLink = $"https://scoresaber.com/api/leaderboard/by-hash/{hash}/info?difficulty={difficulty}&gameMode=SoloStandard";
                String songInfo = client.DownloadString(webLink);
                songSuggest.log?.WriteLine("Unknown Song found and downloaded");
                return JsonConvert.DeserializeObject<LeaderboardInfo>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding song Hash: " + hash + " Difficulty: " + difficulty);
            }
            return new LeaderboardInfo();
        }

        //Generic web puller for song leaderboards general data via SongID
        public LeaderboardInfo GetLeaderboardInfo(String songID)
        {
            try
            {
                _ScoreSaberThrottler.Call();
                //https://scoresaber.com/api/leaderboard/by-id/348871/info
                String songInfo = client.DownloadString("https://scoresaber.com/api/leaderboard/by-id/" + songID + "/info");
                return JsonConvert.DeserializeObject<LeaderboardInfo>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding song with ID: " + songID);
            }
            return new LeaderboardInfo();
        }

        //Generic web puller for a leaderboard collection
        public LeaderboardInfoCollection GetLeaderBoardCollection(int page)
        {
            try
            {
                _ScoreSaberThrottler.Call();
                //https://scoresaber.com/api/leaderboards?ranked=true&page=1
                String songInfo = client.DownloadString("https://scoresaber.com/api/leaderboards?ranked=true&page=" + page);
                return JsonConvert.DeserializeObject<LeaderboardInfoCollection>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding leaderboard page: " + page);
            }
            return new LeaderboardInfoCollection();
        }

        //Generic web puller for song leaderboards scores via SongID and page #
        public ScoreCollection GetLeaderboardScores(String songID, int page)
        {
            try
            {
                _ScoreSaberThrottler.Call();
                //https://scoresaber.com/api/leaderboard/by-id/348871/scores?page=1
                String songInfo = client.DownloadString("https://scoresaber.com/api/leaderboard/by-id/" + songID + "/scores?page=" + page);
                return JsonConvert.DeserializeObject<ScoreCollection>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding song with ID: " + songID);
            }
            return new ScoreCollection();
        }

        //Generic web puller for song leaderboards scores via SongID and page #, countrycode
        public ScoreCollection GetLeaderboardScores(String songID, int page, String countryCode)
        {
            try
            {
                _ScoreSaberThrottler.Call();
                //https://scoresaber.com/api/leaderboard/by-id/348871/scores?page=1
                String songInfo = client.DownloadString("https://scoresaber.com/api/leaderboard/by-id/" + songID + "/scores?page=" + page + "&countries=" + countryCode);
                return JsonConvert.DeserializeObject<ScoreCollection>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding song with ID: " + songID);
            }
            return new ScoreCollection();
        }

        //Generic web puller for BeatSaver song ID
        public BeatSaverSongInfo GetBeatSaverSongInfo(String songHash)
        {
            try
            {
                //https://api.beatsaver.com/maps/hash/fda568fc27c20d21f8dc6f3709b49b5cc96723be
                String songInfo = client.DownloadString("https://api.beatsaver.com/maps/hash/" + songHash);
                return JsonConvert.DeserializeObject<BeatSaverSongInfo>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding song on BeatSaver with Hash: " + songHash);
            }
            return new BeatSaverSongInfo();
        }

        //Generic web puller for AccSaber Ranked Songs
        public List<AccSaberSongMeta> GetAccSaberSongInfo()
        {
            try
            {
                //https://api.accsaber.com/ranked-maps
                String songInfo = client.DownloadString("https://api.accsaber.com/ranked-maps");
                return JsonConvert.DeserializeObject<List<AccSaberSongMeta>>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error getting AccSaber Ranked Songs");
            }
            return new List<AccSaberSongMeta>();
        }

        //Generic web puller for BeatLeader songScore
        public BeatLeaderJson.Score GetBeatLeaderScore(SongID songID)
        {
            try
            {
                _BeatLeaderThrottler.Call();
                //https://api.beatleader.com/score/76561197993806676/6F316D488C43288F3079407829C1028A5E998EBC/ExpertPlus/Standard
                string playerID = songSuggest.activePlayerID;
                string songHash = songID.GetSong().hash;// songSuggest.songLibrary.GetHash(songID);
                string songDifcName = songID.GetSong().GetDifficultyText();//songSuggest.songLibrary.GetDifficultyName(songID);
                string songCharacteristic = songID.GetSong().characteristic;
                string webString = $"https://api.beatleader.com/score/{playerID}/{songHash}/{songDifcName}/{songCharacteristic}";
                string songInfo = client.DownloadString(webString);
                return JsonConvert.DeserializeObject<BeatLeaderJson.Score>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding song on BeatLeader with ID: " + songID);
            }
            return new BeatLeaderJson.Score();
        }

        //https://api.beatleader.com/leaderboard/clanRankings/2bf61xx51?page=1&count=1
        //Generic web puller for BeatLeader Clan Leaderboard
        public BeatLeaderJson.LeaderboardClanRankings GetBeatLeaderClanLeaderboard(SongID songID, int page, int count)
        {
            try
            {
                _BeatLeaderThrottler.Call();
                //https://api.beatleader.com/leaderboard/2bf61xx51?page=1&count=1
                string beatLeaderLeaderboardID = songID.GetSong().beatLeaderID;

                //This is internal handling as it is data missing in SongLibrary that could have been there, but is not maintained currently, only ranked songs are kept.
                if (string.IsNullOrEmpty(beatLeaderLeaderboardID))
                {
                    songSuggest.log?.WriteLine("This Clan Leaderboard is not in known Beat Leader data (Ranked Songs Only): " + songID);
                    return new BeatLeaderJson.LeaderboardClanRankings();
                }
                string webString = $"https://api.beatleader.com/leaderboard/clanRankings/{beatLeaderLeaderboardID}/?page={page}&count={count}";
                String songInfo = client.DownloadString(webString);
                return JsonConvert.DeserializeObject<BeatLeaderJson.LeaderboardClanRankings>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding Clan Leaderboard on BeatLeader with ID: " + songID);
            }
            return new BeatLeaderJson.LeaderboardClanRankings();
        }

        //https://api.beatleader.com/leaderboard/clanRankings/3da61xxxx91/clan/85?page=1&count=5
        //Generic web puller for BeatLeader Specified Clans Leaderboard (by ClanID)
        public BeatLeaderJson.LeaderboardClanRankings GetBeatLeaderClanLeaderboardByID(SongID songID, int clanID, int page, int count)
        {
            try
            {
                _BeatLeaderThrottler.Call();
                //https://api.beatleader.com/leaderboard/clanRankings/3da61xxxx91/clan/85?page=1&count=5
                string beatLeaderLeaderboardID = songID.GetSong().beatLeaderID;

                //This is internal handling as it is data missing in SongLibrary that could have been there, but is not maintained currently, only ranked songs are kept.
                if (string.IsNullOrEmpty(beatLeaderLeaderboardID))
                {
                    songSuggest.log?.WriteLine("This Clan Leaderboard is not in known Beat Leader data (Ranked Songs Only): " + songID);
                    return new BeatLeaderJson.LeaderboardClanRankings();
                }
                //https://api.beatleader.com/leaderboard/clanRankings/3da61xxxx91/clan/85?page=1&count=5
                string webString = $"https://api.beatleader.com/leaderboard/clanRankings/{beatLeaderLeaderboardID}/clan/{clanID}?page={page}&count={count}";
                String songInfo = client.DownloadString(webString);
                return JsonConvert.DeserializeObject<BeatLeaderJson.LeaderboardClanRankings>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding Clan Leaderboard on BeatLeader with ID: " + songID);
            }
            return new BeatLeaderJson.LeaderboardClanRankings();
        }


        //Generic web puller for BeatLeader Leaderboard
        public BeatLeaderJson.Leaderboard GetBeatLeaderLeaderboard(SongID songID, int page, int count)
        {
            try
            {
                _BeatLeaderThrottler.Call();
                //https://api.beatleader.com/leaderboard/2bf61xx51?page=1&count=1
                string beatLeaderLeaderboardID = songID.GetSong().beatLeaderID;
                
                //This is internal handling as it is data missing in SongLibrary that could have been there, but is not maintained currently, only ranked songs are kept.
                if (string.IsNullOrEmpty(beatLeaderLeaderboardID))
                {
                    songSuggest.log?.WriteLine("This leaderboard is not in known Beat Leader data (Ranked Songs Only): " + songID);
                    return new BeatLeaderJson.Leaderboard();
                }
                string webString = $"https://api.beatleader.com/leaderboard/{beatLeaderLeaderboardID}/?page={page}&count={count}";
                String songInfo = client.DownloadString(webString);
                return JsonConvert.DeserializeObject<BeatLeaderJson.Leaderboard>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding Leaderboard on BeatLeader with ID: " + songID);
            }
            return new BeatLeaderJson.Leaderboard();
        }

        //Generic web puller for BeatLeader Compact Scores
        public ScoresCompact GetBeatLeaderScoresCompact(string playerID, int count, int page ,long fromUnixTimeStamp)
        {
            string webString = "";
            try
            {
                _BeatLeaderThrottler.Call();
                //We grab oldest scores first, so updates are performed chronological
                //https://api.beatleader.com/player/76561197993806676/scores/compact?sortBy=date&order=1&page=1&count=100&time_from=1700000000
                if (playerID == "-1") return new ScoresCompact();
                webString = $"https://api.beatleader.com/player/{playerID}/scores/compact?sortBy=pp&order=0&page={page}&count={count}&time_from={fromUnixTimeStamp}";
                String songInfo = client.DownloadString(webString);
                return JsonConvert.DeserializeObject<ScoresCompact>(songInfo, serializerSettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                songSuggest.log?.WriteLine($"Error getting: {webString}");
            }
            return new ScoresCompact();
        }

        //BeatLeader last updated Leaderboard Time
        public long GetBeatLeaderLeaderboardUpdateTime()
        {
            string webString = "";
            try
            {
                _BeatLeaderThrottler.Call();
                //We grab oldest scores first, so updates are performed chronological
                //https://api.beatleader.com/songsuggest/refreshTime
                String playerID = songSuggest.activePlayerID;
                webString = $"https://api.beatleader.com/songsuggest/refreshTime";
                String updated = client.DownloadString(webString);
                return JsonConvert.DeserializeObject<long>(updated, serializerSettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                songSuggest.log?.WriteLine($"Error getting: {webString}");
            }
            return 0;
        }


        //Beatleader Leaderboard
        public List<Top10kPlayer> GetBeatLeaderLeaderboard()
        {
            try
            {
                _BeatLeaderThrottler.Call();
                //https://api.beatleader.com/songsuggest/
                //String songInfo = client.DownloadString("https://api.beatleader.com/songsuggest/?leaderboardContext=general");
                //String songInfo = client.DownloadString("https://api.beatleader.com/songsuggest/?leaderboardContext=noMods");
                String songInfo = client.DownloadString("https://api.beatleader.com/songsuggest/");
                return JsonConvert.DeserializeObject<List<Top10kPlayer>>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding BeatLeader Leaderboard");
            }
            return new List<Top10kPlayer>();
        }

        //Beatleader Leaderboard
        public List<Top10kPlayer> GetBeatLeaderLeaderboard(string mode)
        {
            try
            {
                _BeatLeaderThrottler.Call();
                //https://api.beatleader.com/songsuggest/
                //String songInfo = client.DownloadString("https://api.beatleader.com/songsuggest/?leaderboardContext=general");
                //String songInfo = client.DownloadString("https://api.beatleader.com/songsuggest/?leaderboardContext=noMods");
                String request = $"https://api.beatleader.com/songsuggest/?leaderboardContext={mode}";
                String songInfo = client.DownloadString(request);
                return JsonConvert.DeserializeObject<List<Top10kPlayer>>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error finding BeatLeader Leaderboard");
            }
            return new List<Top10kPlayer>();
        }

        //Beatleader New Songs
        public List<BeatLeaderJson.SongSuggestSong> GetBeatLeaderRankedSongs(long unixTimestamp)
        {
            //Original ranked songs are not ranked, so they have no rank time, so to include them we need to use -1
            if (unixTimestamp == 0) unixTimestamp = -1;

            try
            {
                _BeatLeaderThrottler.Call();
                //https://api.beatleader.com/songsuggest/songs?after_time=1600000000
                String songInfo = client.DownloadString($"https://api.beatleader.com/songsuggest/songs?after_time={unixTimestamp}");
                return JsonConvert.DeserializeObject<List<BeatLeaderJson.SongSuggestSong>>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine($"Error receiving Beatleader Songlist past Timestamp: {unixTimestamp}");
            }
            return new List<BeatLeaderJson.SongSuggestSong>();
        }

        //Beatleader New Songs
        public BeatLeaderJson.ClanMembersList GetBeatLeaderClanMembers(string clanTag, int page, int count, bool primary)
        {
            try
            {
                _BeatLeaderThrottler.Call();
                //https://api.beatleader.com/clan/genx?page=1&count=100&primary=true
                string downloadstring = $"https://api.beatleader.com/clan/{clanTag}?page={page}&count={count}&primary={primary}";
                String songInfo = client.DownloadString(downloadstring);
                return JsonConvert.DeserializeObject<BeatLeaderJson.ClanMembersList>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine($"Error receiving Beatleader New songs: ClanTag: {clanTag} page: {page} count: {count} primary {primary}");
            }
            return new BeatLeaderJson.ClanMembersList();
        }



        public FilesMeta GetFilesMeta()
        {
            songSuggest.status = "Getting Files.meta";
            //Let us check meta for updates.
            string webPath = "https://raw.githubusercontent.com/HypersonicSharkz/SmartSongSuggest/master/TaohSongSuggest/Configuration/InitialData/Files.meta";
            string metaInfo = client.DownloadString(webPath);
            return JsonConvert.DeserializeObject<FilesMeta>(metaInfo, serializerSettings);
        }

        public List<Song> GetSongLibrary()
        {
            songSuggest.status = "Downloading Song Library";
            //Song Library pull
            string webPath = "https://raw.githubusercontent.com/HypersonicSharkz/SmartSongSuggest/master/TaohSongSuggest/Configuration/InitialData/SongLibrary.json";
            //Download the file to a tmp file, add contents, and delete tmp when done.
            string songLibraryInfo = client.DownloadString(webPath);
            List<Song> songLibrary = JsonConvert.DeserializeObject<List<Song>>(songLibraryInfo, serializerSettings);
            return songLibrary;
        }

        public List<Top10kPlayer> GetTop10kPlayers()
        {
            songSuggest.status = "Downloading Player Scores";
            //Top 10k Download
            //where to get the files from
            string webPath = "https://raw.githubusercontent.com/HypersonicSharkz/SmartSongSuggest/master/TaohSongSuggest/Configuration/InitialData/Top10KPlayers.json";
            string downloadString = client.DownloadString(webPath);

            return JsonConvert.DeserializeObject<List<Top10kPlayer>>(downloadString, serializerSettings);
        }

        internal Playlist LoadWebURL(string weblink)
        {
            try
            {
                //https://api.accsaber.com/ranked-maps
                String songInfo = client.DownloadString(weblink);
                return JsonConvert.DeserializeObject<Playlist>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error downloading given WebURL");
                return null;
            }
        }

        internal JObject LoadWebURL(PlaylistSyncURL weblink)
        {
            try
            {
                //https://api.accsaber.com/ranked-maps
                String songInfo = client.DownloadString(weblink.SyncURL);
                return JsonConvert.DeserializeObject<JObject>(songInfo, serializerSettings);
            }
            catch
            {
                songSuggest.log?.WriteLine("Error downloading given WebURL");
                return null;
            }
        }
    }

    internal class Throttler
    {
        int calls = 0;
        DateTime periodStart = DateTime.UtcNow;

        public int callPerPeriod { get; set; }
        public int callPeriodSeconds { get; set; }

        public void Call()
        {
            //Count up calls, check if we hit the 15 second mark of 100 calls
            calls++;
            if (calls < callPerPeriod) return;


            //Figure out how long the 100 calls have taken.
            double difference = (DateTime.UtcNow - periodStart).TotalSeconds;

            //Sleep missing time to 15 seconds
            if (difference < callPeriodSeconds)
            {
                double sleepMS = (callPeriodSeconds - difference) * 1000;
                Console.WriteLine("Sleeping: {0}ms", sleepMS);
                System.Threading.Thread.Sleep((int)sleepMS);
            }

            //Reset Counter
            calls = 0;
            periodStart = DateTime.UtcNow;

        }
    }
}