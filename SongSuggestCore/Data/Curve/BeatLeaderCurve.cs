using SongLibraryNS;
using System.Collections.Generic;
using System.Linq;

namespace Curve
{
    public class BeatLeaderCurve
    {
        private static readonly List<CurvePoint> curvePoints = new List<CurvePoint>
        {
            new CurvePoint { Accuracy = 0.0, Multiplier = 0.000 },
            new CurvePoint { Accuracy = 0.6, Multiplier = 0.256 },
            new CurvePoint { Accuracy = 0.65, Multiplier = 0.296 },
            new CurvePoint { Accuracy = 0.7, Multiplier = 0.345 },
            new CurvePoint { Accuracy = 0.75, Multiplier = 0.404 },
            new CurvePoint { Accuracy = 0.8, Multiplier = 0.473 },
            new CurvePoint { Accuracy = 0.825, Multiplier = 0.522 },
            new CurvePoint { Accuracy = 0.85, Multiplier = 0.581 },
            new CurvePoint { Accuracy = 0.875, Multiplier = 0.650 },
            new CurvePoint { Accuracy = 0.9, Multiplier = 0.729 },
            new CurvePoint { Accuracy = 0.91, Multiplier = 0.768 },
            new CurvePoint { Accuracy = 0.92, Multiplier = 0.813 },
            new CurvePoint { Accuracy = 0.93, Multiplier = 0.867 },
            new CurvePoint { Accuracy = 0.94, Multiplier = 0.931 },
            new CurvePoint { Accuracy = 0.95, Multiplier = 1.000 },
            new CurvePoint { Accuracy = 0.955, Multiplier = 1.039 },
            new CurvePoint { Accuracy = 0.96, Multiplier = 1.094 },
            new CurvePoint { Accuracy = 0.965, Multiplier = 1.167 },
            new CurvePoint { Accuracy = 0.97, Multiplier = 1.256 },
            new CurvePoint { Accuracy = 0.9725, Multiplier = 1.315 },
            new CurvePoint { Accuracy = 0.975, Multiplier = 1.392 },
            new CurvePoint { Accuracy = 0.9775, Multiplier = 1.490 },
            new CurvePoint { Accuracy = 0.98, Multiplier = 1.618 },
            new CurvePoint { Accuracy = 0.9825, Multiplier = 1.786 },
            new CurvePoint { Accuracy = 0.985, Multiplier = 2.007 },
            new CurvePoint { Accuracy = 0.9875, Multiplier = 2.303 },
            new CurvePoint { Accuracy = 0.99, Multiplier = 2.700 },
            new CurvePoint { Accuracy = 0.9925, Multiplier = 3.241 },
            new CurvePoint { Accuracy = 0.995, Multiplier = 4.010 },
            new CurvePoint { Accuracy = 0.9975, Multiplier = 5.158 },
            new CurvePoint { Accuracy = 0.999, Multiplier = 6.241 },
            new CurvePoint { Accuracy = 1.0, Multiplier = 7.424 },
        };


        public static double Multiplier(double accuracy)
        {
            //Set start and end point to inital points
            CurvePoint startPost = curvePoints.Where(c => c.Accuracy <= accuracy).Last();
            CurvePoint endPost = curvePoints.Where(c => c.Accuracy >= accuracy).First();

            //If the 2 points are the same (excactly on the point often the case with 0 accuracy) return the value directly
            if (startPost == endPost) return startPost.Multiplier;

            // Calculate the percentage of distance traveled along the accuracy range
            double accuracyRange = endPost.Accuracy - startPost.Accuracy;
            double accuracyTraveled = accuracy - startPost.Accuracy;
            double percentTraveled = accuracyTraveled / accuracyRange;

            // Calculate the multiplier contributions from the start and end posts
            // (any traveled distance means distance that get the greater end post reward).
            double startPostContribution = (1.0 - percentTraveled) * startPost.Multiplier;
            double endPostContribution = percentTraveled * endPost.Multiplier;

            // Sum up the contributions and return the overall multiplier
            return startPostContribution + endPostContribution;
        }

        //Expected input values of 0 to 1 for accuracy
        public static double PP(double accuracy, double complexityRating)
        {
            if (accuracy < 0 || accuracy > 1) return 0.0;
            if (complexityRating == 0) return 0;
            return Multiplier(accuracy) * (complexityRating + 18) * 61;
        }

        public static double PP(double accuracy, SongID songID)
        {
            Song song = SongLibrary.SongIDToSong(songID);
            if (song == null) return 0;
            return PP(accuracy, song.complexityAccSaber);
        }
    }
}