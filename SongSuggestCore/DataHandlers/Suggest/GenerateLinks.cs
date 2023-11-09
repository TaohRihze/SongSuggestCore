using System;
using System.Linq;
using LinkedData;

namespace Actions
{
    public static class GenerateLinks
    {
        //Create the linking of the songs found to represent a player, with matching songs on the leaderboard.
        //origin -> matching leaderboard player -> other top songs from that leaderboard player
        //Goal with this is creating a link structure for future evaluations of their strength, and place them so they are easy to reach from either side.
        //OriginSongs <-> SongLink <-> Target Songs
        public static void Execute(RankedSongSuggest.DTO data, out SongEndPointCollection originSongs, out SongEndPointCollection targetSongs, out int linkedSongsCount)
        {

            //Generate the initial endpoints dictionary and attach it to the originSongs
            var endPoints = data.originSongIDs
                .Select(songID => new SongEndPoint { songID = songID })
                .ToDictionary(endpoint => endpoint.songID, endpoint => endpoint);

            originSongs = new SongEndPointCollection() { endPoints = endPoints };

            //Set values to local values that are reused multiple times, no need to calculate them every time.
            int maxRank = data.leaderboard.top10kPlayers.Max(c => c.rank);

            //We are preparing the actual linking between a playrs origin songs to their suggested target songs.
            //origin -> matching leaderboard player -> other leaderboard players songs
            var links = data.leaderboard.top10kPlayers                                          //Get reference to the top 10k players data                        
                .Where(player => ValidateLinkPlayer(player, data.playerID))                     //Remove active player so player does not use their own data
                .SelectMany(linkPlayer => linkPlayer.top10kScore                                //Get the players scores
                    .Where(originSongCandidate => ValidateOriginSong(data,originSongCandidate)) //Remove scores that does not fit filtering for Origin Song.
                    .Select(originSong => new { player = linkPlayer, originSong = originSong }) //Keep variables needed for creating SongLinks
                )
                .SelectMany(originLinks => originLinks.player.top10kScore                                                                       //Get the players other scores to link with themselves.
                    .Where(potentialTargetSong => ValidateTargetSong(data, originLinks.originSong, potentialTargetSong))                        //Remove the selflink and bans
                    .Select(targetSong => new { player = originLinks.player, originSong = originLinks.originSong, targetSong = targetSong })    //Store needed variables again
                )
                .Select(linkData => new { link = GenerateSongLink(data, linkData.player, linkData.originSong, linkData.targetSong, maxRank), index = linkData.player.rank })    //Create songlinks for further processing
                .ToList();

            //Update completion to partial completion
            data.songSuggestCompletion = 0.44;

            //Update found links
            linkedSongsCount = links.Count();

            //Reset the targetPoints endpoint
            targetSongs = new SongEndPointCollection();

            //Link the links to the endpoints
            foreach (var item in links)
            {
                //Add the songlink to the origin list
                string originSongID = item.link.originSongScore.songID;
                originSongs.endPoints[originSongID].songLinks.Add(item.link);

                //Create the target endpoint if needed.
                string targetSongID = item.link.targetSongScore.songID;
                if (!targetSongs.endPoints.ContainsKey(targetSongID))
                {
                    targetSongs.endPoints.Add(targetSongID, new SongEndPoint { songID = targetSongID });
                }

                //Add the songlink to the target list
                targetSongs.endPoints[targetSongID].songLinks.Add(item.link);

                //Update complete %
                double localPercentDone = (double)item.index / maxRank;
                double localGroupStart = 0.44;
                double localGroupsSize = 0.22;
                data.songSuggestCompletion = localGroupStart + (localPercentDone * localGroupsSize);
            }

            //Update completion to full completion
            data.songSuggestCompletion = 0.66;
        }

        //Filters players that should not be used. (Only filters out the active player currently, previous versions checked global rank and such, but it did not improve results).
        private static bool ValidateLinkPlayer(Top10kPlayer player, String playerID)
        {
            return player.id != playerID;
        }

        //Removes songs that are to be ignored, as well as songs linking itself.
        private static bool ValidateTargetSong(RankedSongSuggest.DTO data, Top10kScore originSong, Top10kScore suggestedSong)
        {
            return suggestedSong.rank != originSong.rank && !data.ignoreSongs.Contains(suggestedSong.songID);
        }

        //Generate the Song Link, as well as set the aproximate completion, as majority of loop should be in this part
        private static SongLink GenerateSongLink(RankedSongSuggest.DTO data, Top10kPlayer player, Top10kScore originSong, Top10kScore suggestedSong, int maxRank)
        {
            //Update complete %
            double localPercentDone = (double)player.rank / maxRank;
            double localGroupStart = 0.11;
            double localGroupsTotalValue = 0.33;
            data.songSuggestCompletion = localGroupStart + (localPercentDone * localGroupsTotalValue);

            return new SongLink() { playerID = player.id, originSongScore = originSong, targetSongScore = suggestedSong };
        }

        //Filter the active score, we check first for the score having a match with origin songs, then if the score has a Score Value (liked songs will not have this but should be kept).
        //And we make sure it is within the better/worse Acc Cap range. Finally once a score is validated we increase validated score counts.

        private static bool ValidateOriginSong(RankedSongSuggest.DTO data, Top10kScore originSongCandidate)
        {
            //Return false if song is not in the list of songs we are looking for.
            if (!data.originSongIDs.Contains(originSongCandidate.songID)) return false;

            //Score validation check, we need to fail only if the song is not unplayed (0 value), and is outside the given limits. Single lining this is prone to errors.
            double playerSongValue = data.suggestSM.PlayerScoreValue(originSongCandidate.songID);
            bool validatedScore = false;
            if (playerSongValue == 0) validatedScore = true;
            if (originSongCandidate.pp < playerSongValue * data.betterAccCap && originSongCandidate.pp > playerSongValue * data.worseAccCap) validatedScore = true;
            if (!validatedScore) return false;


            //data.manager.linkedPlayers++; //Consider Scrapping or another way to keep track of this ...
            //Count validated songs    
            return true;
        }
    }
}