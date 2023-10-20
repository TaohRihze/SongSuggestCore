using SongSuggestNS;
using System;

namespace SongLibraryNS
{
    //This class might need another Namespace if data from other than songs are needed. Idea is to place missing data from web sources here as an alternative lookup.
    public class ManualData
    {

        //If a song cannot be given a max score a workaround should be tried, for now a few acc saber songs that might miss max scores has been added.
        //These are calculated via their max notes
        //If need be a lookup for notecount might be added .
        //!!!Current workround will break when a note will not match 115 points (new note types).
        public static int SongMaxScore(string songID, SongSuggest songSuggest)
        {


            //Insert Workaround for songs without max score
            switch (songID)
            {
                //Robber and bouqet     776 : 332538
                case "332538":
                    return SongMaxScore(776);
                //Tell me you know      396 : 463149                        
                case "463149":
                    return SongMaxScore(396);
                //All my love           260 : 418921
                case "418921":
                    return SongMaxScore(260);
                //What you know         242 : 568102
                case "568102":
                    return SongMaxScore(242);
                //Waiting for love      420 : 544407
                case "544407":
                    return SongMaxScore(420);
                //simulation            212 : 368917
                case "368917":
                    return SongMaxScore(212);
                default:
                    songSuggest.log?.WriteLine($"Song has no maxScore, nor known alternate: {songID}");
                    return 0;
            }
        }

        private static int SongMaxScore(int notes)
        {
            int comboLoss = 7245; //Amount of points lost potentially due to missing combo at start.
            return notes * 115 * 8 - comboLoss;
        }
    }
}
