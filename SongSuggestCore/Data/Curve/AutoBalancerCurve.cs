using System;
using System.Collections.Generic;
using System.Linq;

namespace Curve
{
    public class AutoBalancerCurve
    {
        private static double SecretMultiplier = 50;

        public static List<CurvePoint> curvePoints = new List<CurvePoint>
        //New Test
        {
            new CurvePoint { Accuracy = 0.0, Multiplier = 0 },
            new CurvePoint { Accuracy = 0.6, Multiplier = 0.4697536774 },
            new CurvePoint { Accuracy = 0.65, Multiplier = 0.5440899991 },
            new CurvePoint { Accuracy = 0.7, Multiplier = 0.602809644 },
            new CurvePoint { Accuracy = 0.75, Multiplier = 0.6708364523 },
            new CurvePoint { Accuracy = 0.8, Multiplier = 0.7394098703 },
            new CurvePoint { Accuracy = 0.825, Multiplier = 0.8114936026 },
            new CurvePoint { Accuracy = 0.85, Multiplier = 0.882409261 },
            new CurvePoint { Accuracy = 0.875, Multiplier = 0.8946818696 },
            new CurvePoint { Accuracy = 0.9, Multiplier = 0.9114805466 },
            new CurvePoint { Accuracy = 0.91, Multiplier = 0.9205326834 },
            new CurvePoint { Accuracy = 0.92, Multiplier = 0.9316737749 },
            new CurvePoint { Accuracy = 0.93, Multiplier = 0.9455130995 },
            new CurvePoint { Accuracy = 0.94, Multiplier = 0.9638784925 },
            new CurvePoint { Accuracy = 0.95, Multiplier = 1 },
            new CurvePoint { Accuracy = 0.955, Multiplier = 1.026198973 },
            new CurvePoint { Accuracy = 0.96, Multiplier = 1.065453912 },
            new CurvePoint { Accuracy = 0.965, Multiplier = 1.119418574 },
            new CurvePoint { Accuracy = 0.97, Multiplier = 1.196883976 },
            new CurvePoint { Accuracy = 0.9725, Multiplier = 1.24606145 },
            new CurvePoint { Accuracy = 0.975, Multiplier = 1.299329794 },
            new CurvePoint { Accuracy = 0.9775, Multiplier = 1.361128036 },
            new CurvePoint { Accuracy = 0.98, Multiplier = 1.431717295 },
            new CurvePoint { Accuracy = 0.9825, Multiplier = 1.516058839 },
            new CurvePoint { Accuracy = 0.985, Multiplier = 1.61336931 },
            new CurvePoint { Accuracy = 0.9875, Multiplier = 1.729393333 },
            new CurvePoint { Accuracy = 0.99, Multiplier = 1.88267038 },
            new CurvePoint { Accuracy = 0.99125, Multiplier = 1.97258247 },
            new CurvePoint { Accuracy = 0.9925, Multiplier = 2.075811646 },
            new CurvePoint { Accuracy = 0.99375, Multiplier = 2.181390896 },
            new CurvePoint { Accuracy = 0.995, Multiplier = 2.346766472 },
            new CurvePoint { Accuracy = 0.99625, Multiplier = 2.482026286 },
            new CurvePoint { Accuracy = 0.9975, Multiplier = 2.656889198 },
            new CurvePoint { Accuracy = 0.99825, Multiplier = 2.761806946 },
            new CurvePoint { Accuracy = 0.999, Multiplier = 2.866724693 },
            new CurvePoint { Accuracy = 0.9995, Multiplier = 2.936669858 },
            new CurvePoint { Accuracy = 1.0, Multiplier = 3.006615023 }
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