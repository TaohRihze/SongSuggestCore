using System.Collections.Generic;
using System.Linq;
using SongLibraryNS;
using SongSuggestNS;


namespace LinkedData
{
    public class SongEndPointCollection
    {
        public Dictionary<SongID, SongEndPoint> endPoints = new Dictionary<SongID, SongEndPoint>();
       
        public void SetRelevance(Actions.RankedSongSuggest actions,int originPoints,int requiredMatches,SongIDType songIDType)
        {
            int percentDoneCalc = 0;
            foreach (SongEndPoint songEndPoint in endPoints.Values)
            {
                songEndPoint.SetRelevance(originPoints,requiredMatches,songIDType);
                percentDoneCalc++;
                actions.songSuggestCompletion = (5.5 + (0.5 * percentDoneCalc / endPoints.Values.Count())) / 6.0;
            }
        }

        //public void SetDistance(SongSuggest songSuggest, SongIDType songIDType)
        //{
        //    foreach (SongEndPoint songEndPoint in endPoints.Values)
        //    {
        //        songEndPoint.SetDistance(songSuggest,songIDType);
        //    }
        //}

        public void SetStyle(SongEndPointCollection originSongs, SongIDType songIDType)
        {
            foreach (SongEndPoint songEndPoint in endPoints.Values)
            {
                songEndPoint.SetStyle(originSongs, songIDType);
            }
        }

        //public void SetPP(SongSuggest songSuggest, SongIDType songIDType)
        //{
        //    foreach (SongEndPoint songEndPoint in endPoints.Values)
        //    {
        //        songEndPoint.SetPP(songSuggest, songIDType);
        //    }
        //}

        //public void SetLocalPP(SongSuggest songSuggest, SongIDType songIDType)
        //{
        //    foreach (SongEndPoint songEndPoint in endPoints.Values)
        //    {
        //        songEndPoint.SetLocalPP(songSuggest, songIDType);
        //    }
        //}
    }
}