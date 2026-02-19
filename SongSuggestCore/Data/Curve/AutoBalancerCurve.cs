using System;
using System.Collections.Generic;
using System.Linq;

namespace Curve
{
    public class AutoBalancerCurve
    {
        private static double SecretMultiplier = 50;

        public static List<CurvePoint> curvePoints = new List<CurvePoint>
            {
                new CurvePoint { Accuracy = 0.0, Multiplier = 0.0 },
                new CurvePoint { Accuracy = 0.6, Multiplier = 0.2150822648 },
                new CurvePoint { Accuracy = 0.65, Multiplier = 0.675 },
                new CurvePoint { Accuracy = 0.7, Multiplier = 0.7037013503 },
                new CurvePoint { Accuracy = 0.75, Multiplier = 0.7328650857 },
                new CurvePoint { Accuracy = 0.8, Multiplier = 0.7630545785 },
                new CurvePoint { Accuracy = 0.825, Multiplier = 0.7901826847 },
                new CurvePoint { Accuracy = 0.85, Multiplier = 0.818450017 },
                new CurvePoint { Accuracy = 0.875, Multiplier = 0.8455690457 },
                new CurvePoint { Accuracy = 0.9, Multiplier = 0.8664472938 },
                new CurvePoint { Accuracy = 0.91, Multiplier = 0.8789288551 },
                new CurvePoint { Accuracy = 0.92, Multiplier = 0.8960626347 },
                new CurvePoint { Accuracy = 0.93, Multiplier = 0.9297628503 },
                new CurvePoint { Accuracy = 0.94, Multiplier = 0.9560875979 },
                new CurvePoint { Accuracy = 0.95, Multiplier = 1.0 },
                new CurvePoint { Accuracy = 0.955, Multiplier = 1.021672529 },
                new CurvePoint { Accuracy = 0.96, Multiplier = 1.061386588 },
                new CurvePoint { Accuracy = 0.965, Multiplier = 1.116191989 },
                new CurvePoint { Accuracy = 0.97, Multiplier = 1.18813117 },
                new CurvePoint { Accuracy = 0.9725, Multiplier = 1.21695223 },
                new CurvePoint { Accuracy = 0.975, Multiplier = 1.27811188 },
                new CurvePoint { Accuracy = 0.9775, Multiplier = 1.342562124 },
                new CurvePoint { Accuracy = 0.98, Multiplier = 1.415976399 },
                new CurvePoint { Accuracy = 0.9825, Multiplier = 1.504822421 },
                new CurvePoint { Accuracy = 0.985, Multiplier = 1.607965506 },
                new CurvePoint { Accuracy = 0.9875, Multiplier = 1.733915806 },
                new CurvePoint { Accuracy = 0.99, Multiplier = 1.97 },
                new CurvePoint { Accuracy = 0.99125, Multiplier = 2.06 },
                new CurvePoint { Accuracy = 0.9925, Multiplier = 2.18 },
                new CurvePoint { Accuracy = 0.99375, Multiplier = 2.300805628 },
                new CurvePoint { Accuracy = 0.995, Multiplier = 2.472370362 },
                new CurvePoint { Accuracy = 0.99625, Multiplier = 2.64359469 },
                new CurvePoint { Accuracy = 0.9975, Multiplier = 2.83 },
                new CurvePoint { Accuracy = 0.99825, Multiplier = 2.98 },
                new CurvePoint { Accuracy = 0.999, Multiplier = 3.15 },
                new CurvePoint { Accuracy = 0.9995, Multiplier = 3.3 },
                new CurvePoint { Accuracy = 1.0, Multiplier = 3.5 }
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

        //Expected value of 0 to 1 for accuracy
        public static double PP(double accuracy, double starRating)
        {
            if (accuracy < 0 || accuracy > 1) return 0.0;
            return SecretMultiplier * Multiplier(accuracy) * starRating;
        }

        //Rank is 1 indexed, and result is 0-1 range.
        public static double RankMultiplier(int rank)
        {
            double multiplier = Math.Pow(0.965, rank - 1);
            return multiplier;
        }

    }
}