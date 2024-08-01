using SongLibraryNS;
using System.Collections.Generic;
using System.Linq;
using System;

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

        //Expected input values of 0 to 1 for accuracy, as well as 3 parameters for calc
        public static double PP(double accuracy, double accRating, double passRating, double techRating)
        {
            //Verify the accuracy given is within the expected 0-1 range
            if (accuracy < 0 || accuracy > 1) return 0.0;
            //Verify song is ranked, and got all 3 parameters filled.
            if (accRating * passRating * techRating == 0) return 0;


            //Calculate the 3 PP values.

            double passPP = 15.2 * Math.Exp(Math.Pow(passRating, 1.0 / 2.62)) - 30.0;
            //Check to ensure value is positive. Reset to 0 if invalid.
            passPP = (passPP >= 0.0 || passPP <= double.MaxValue) ? passPP : 0.0;

            double accPP = Multiplier(accuracy) * accRating * 34.0;
            double techPP = Math.Exp(1.9 * accuracy) * 1.08 * techRating;

            //Find the total PP
            double totalPP = 650.0 * Math.Pow(passPP + accPP + techPP, 1.3) / Math.Pow(650.0, 1.3);
            
            return totalPP;
        }

        //Default method, supply accuracy and songID
        public static double PP(double accuracy, Song song)
        {
            if (song == null) return 0;
            return PP(accuracy, song.starAccBeatLeader,song.starPassBeatLeader,song.starTechBeatLeader);
        }

        ////Official Calculations.
        //private static (float, float, float) GetPp(LeaderboardContexts context, float accuracy, float accRating, float passRating, float techRating)
        //{

        //    float passPP = 15.2f * MathF.Exp(MathF.Pow(passRating, 1 / 2.62f)) - 30f;
        //    if (float.IsInfinity(passPP) || float.IsNaN(passPP) || float.IsNegativeInfinity(passPP) || passPP < 0)
        //    {
        //        passPP = 0;
        //    }
        //    float accPP = context == LeaderboardContexts.Golf ? accuracy * accRating * 42f : Curve2(accuracy) * accRating * 34f;
        //    float techPP = MathF.Exp(1.9f * accuracy) * 1.08f * techRating;

        //    return (passPP, accPP, techPP);
        //}

        ////Must be run on the sum of the calculated PP's
        //private static float Inflate(float peepee)
        //{
        //    return (650f * MathF.Pow(peepee, 1.3f)) / MathF.Pow(650f, 1.3f);
        //}
    }
}