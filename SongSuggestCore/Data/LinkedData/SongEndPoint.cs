﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SongSuggestNS;
using SongLibraryNS;

namespace LinkedData
{
    public class SongEndPoint
    {
        public SongID songID { get; set; }
        public List<SongLink> songLinks = new List<SongLink>();
        public float totalRelevanceScore = 0;
        public double totalRank = 0;
        public float weightedRelevanceScore = 0;
        public int matchedSongs = 0;
        public float weightedSongs = 0;
        public double averageRank = 0;

        //Total of all unique grouping of Origin -> Endpoint links distance averages.
        public double totalDistance = 0;
        public double averageDistance = 0;

        //StyleFilter data
        public double proportionalStyle = 0;

        //PP Estimate
        public double estimatedPP = 0;

        //Local PP vs Global
        public double localPPAverage = 0;
        public double localVSGlobalPP = 0;


        public void SetRelevance(int originPoints,int requiredMatches, SongIDType songIDType)
        {
            //Get a list of all origin songs
            List<SongID> originSongIDs = songLinks
                .Select(c => SongLibrary.StringIDToSongID(c.originSongScore.songID,songIDType))
                .Distinct()
                .ToList();
            matchedSongs = originSongIDs.Count();

            //Get the average of each songs links values and add them to this songs Relevance Score
            foreach (SongID songID in originSongIDs)
            {
                totalRelevanceScore += songLinks.Where(c => c.originSongScore.songID == songID).Select(c => c.Strength()).Average();
            //    totalRank += songLinks.Where(c => c.playerSongScore.songID == songID).Select(c => c.suggestedSongScore.rank).Average();
            }
            //Reduces the required endpoints for maximum efficiency by a % of total endpoints
            //(e.g. 50 endpoints and 90% requiredMatches means a 45+ matched song gets same score as a 50 matched song with same base weight)
            float songBonus = 0.01f * (100.0f-requiredMatches) * originPoints;
            weightedSongs = Math.Max(0.0f + originPoints-songBonus, matchedSongs); //.  0.0f+matchedSongs+(0.0f+originPoints-matchedSongs)*(100.0f-focusPercentage)/100;
            weightedRelevanceScore = totalRelevanceScore / weightedSongs;
            //averageRank = totalRank / matchedSongs;

            //Calculate averageRank with a minimum link amount, center vs 10.50 (1+2+...20)/20
            float minRankLinks = 50;
            float rankSum = songLinks.Select(c => c.targetSongScore.rank).Sum();
            float rankLinks = songLinks.Count;
            rankSum += Math.Max(minRankLinks - rankLinks, 0.0f)*10.5f;
            averageRank = rankSum / Math.Max(minRankLinks, rankLinks);

        }

        ////Average of the songLinks (per song) distance. Require the Players Origin Song score to set 1,1 coordinates.
        //public void SetDistance (SongSuggest songSuggest, SongIDType songIDType)
        //{
        //    List<SongID> originSongIDs = songLinks
        //        .Select(c => SongLibrary.StringIDToSongID(c.originSongScore.songID, songIDType))
        //        .Distinct()
        //        .ToList();
        //    matchedSongs = originSongIDs.Count();
        //    foreach (SongID songID in originSongIDs)
        //    {
        //        totalDistance += songLinks.Where(c => c.originSongScore.songID == songID).Select(c => c.Distance(songSuggest)).Average();
        //    }
        //    averageDistance = totalDistance / matchedSongs;
        //}

        public void SetStyle(SongEndPointCollection originSongs, SongIDType songIDType)
        {
            List<SongID> originSongIDs = songLinks
                .Select(c => SongLibrary.StringIDToSongID(c.originSongScore.songID, songIDType))
                .Distinct()
                .ToList();
            foreach (SongID originSongID in originSongIDs)
            {
                int originSongCount = originSongs.endPoints[originSongID].songLinks.Count();
                int linkedCount = songLinks.Select(c => c.originSongScore.songID == songID).Count();
                proportionalStyle += 1.0*linkedCount/originSongCount;
            }
        }

        ////Tries and estimate the PP of a song.
        ////Current strategy is to sort through all links, find out how many Originsongs have a better score than the players score
        ////Then add the next score as the estimate. (In case all scores are better, last link is selected instead).
        //public void SetPP(SongSuggest songSuggest, SongIDType songIDType)
        //{
        //    int songIndex = 0;
        //    List<SongID> originSongIDs = songLinks
        //        .Select(c => SongLibrary.StringIDToSongID(c.originSongScore.songID, songIDType))
        //        .Distinct()
        //        .ToList();
        //    foreach (SongID originSongID in originSongIDs)
        //    {
        //        //Gets the players score on the song being looked at
        //        //***NEEDS to consider how to handle a -1 score from "Favorite not played" songs***
        //        double activePlayerScore = songSuggest.activePlayer.GetRatedScore(originSongID,Actions.LeaderboardType.ScoreSaber);
                
        //        //Finds the amount of links with a better score than the players score, and ups the index with this amount
        //        songIndex += songLinks.Where(c => c.originSongScore.songID == originSongID&& c.originSongScore.pp > activePlayerScore).ToList().Count();
        //    }
        //    if (songIndex >= songLinks.Count()) songIndex = songLinks.Count() - 1;
        //    estimatedPP = songLinks.OrderByDescending(c => c.targetSongScore.pp).ToList()[songIndex].targetSongScore.pp;

        //    //Console Debug
        //    //String songName = songSuggest.songLibrary.GetName(songID);
        //    //String songDifc = songSuggest.songLibrary.GetDifficultyName(songID);
        //    int totalSongs = songLinks.Count();

        //    //double playerScore = songSuggest.activePlayer.GetScore(songID);
        //    //double diffPP = playerScore - estimatedPP;
        //    //songSuggest.log?.WriteLine("diffPP;estimatedPP;playerScore;songIndex;totalLinks;songName(songDifc-songID)");
        //    //if (songIndex != 0&&totalSongs > 50) songSuggest.log?.WriteLine("{0};{1};{2};{3};{4};{5}({6}-{7})",diffPP,estimatedPP,playerScore,songIndex,totalSongs,songName,songDifc,songID);

        //    //!!!CONSIDER REMOVING/ALTERNATIVE FOR HANDLING UNKNOWN MATCHES WHEN SPLITTING PP!!!
        //    //Sets the estimate to 0 if there is low amount of links to a song.
        //    if (totalSongs < 50) estimatedPP = 0;
        //}

        //public void SetLocalPP(SongSuggest songSuggest, SongIDType songIDType)
        //{
        //    //Get linked originSongs
        //    List<SongID> originSongIDs = songLinks
        //        .Select(c => SongLibrary.StringIDToSongID(c.originSongScore.songID, songIDType))
        //        .Distinct()
        //        .ToList();
        //    //Count the averages for each song (so we end up with weighted averages).
        //    foreach (SongID originSongID in originSongIDs)
        //    {
        //        localPPAverage += songLinks.Where(c => c.originSongScore.songID == originSongID).Average(c => c.targetSongScore.pp);
        //    }
        //    //Reduce it to average per song
        //    localPPAverage = localPPAverage / 50;// originSongIDs.Count();
        //    //localVSGlobalPP = localPPAverage / songSuggest.scoreSaberScoreBoard.top10kSongMeta[songID].averageScore;
        //    localVSGlobalPP = localPPAverage / songSuggest.songSuggest.Leaderboard().top10kSongMeta[songID].averageScore;
        //}
    }
}