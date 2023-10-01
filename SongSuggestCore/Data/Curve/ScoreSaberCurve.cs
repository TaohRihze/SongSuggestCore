using System.Collections.Generic;
using System.Linq;

namespace Curve
{
    public class ScoreSaberCurve
    {
        private static double SecretMultiplier = 42.117208413;

        private static readonly List<CurvePoint> curvePoints = new List<CurvePoint>
        {
            new CurvePoint { Accuracy = 0.0, Multiplier = 0.0 },
            new CurvePoint { Accuracy = 0.6, Multiplier = 0.18223233667439062 },
            new CurvePoint { Accuracy = 0.65, Multiplier = 0.5866010012767576 },
            new CurvePoint { Accuracy = 0.7, Multiplier = 0.6125565959114954 },
            new CurvePoint { Accuracy = 0.75, Multiplier = 0.6451808210101443 },
            new CurvePoint { Accuracy = 0.8, Multiplier = 0.6872268862950283 },
            new CurvePoint { Accuracy = 0.825, Multiplier = 0.7150465663454271 },
            new CurvePoint { Accuracy = 0.85, Multiplier = 0.7462290664143185 },
            new CurvePoint { Accuracy = 0.875, Multiplier = 0.7816934560296046 },
            new CurvePoint { Accuracy = 0.9, Multiplier = 0.825756123560842 },
            new CurvePoint { Accuracy = 0.91, Multiplier = 0.8488375988124467 },
            new CurvePoint { Accuracy = 0.92, Multiplier = 0.8728710341448851 },
            new CurvePoint { Accuracy = 0.93, Multiplier = 0.9039994071865736 },
            new CurvePoint { Accuracy = 0.94, Multiplier = 0.9417362980580238 },
            new CurvePoint { Accuracy = 0.95, Multiplier = 1.0 },
            new CurvePoint { Accuracy = 0.955, Multiplier = 1.0388633331418984 },
            new CurvePoint { Accuracy = 0.96, Multiplier = 1.0871883573850478 },
            new CurvePoint { Accuracy = 0.965, Multiplier = 1.1552120359501035 },
            new CurvePoint { Accuracy = 0.97, Multiplier = 1.2485807759957321 },
            new CurvePoint { Accuracy = 0.9725, Multiplier = 1.3090333065057616 },
            new CurvePoint { Accuracy = 0.975, Multiplier = 1.3807102743105126 },
            new CurvePoint { Accuracy = 0.9775, Multiplier = 1.4664726399289512 },
            new CurvePoint { Accuracy = 0.98, Multiplier = 1.5702410055532239 },
            new CurvePoint { Accuracy = 0.9825, Multiplier = 1.697536248647543 },
            new CurvePoint { Accuracy = 0.985, Multiplier = 1.8563887693647105 },
            new CurvePoint { Accuracy = 0.9875, Multiplier = 2.058947159052738 },
            new CurvePoint { Accuracy = 0.99, Multiplier = 2.324506282149922 },
            new CurvePoint { Accuracy = 0.99125, Multiplier = 2.4902905794106913 },
            new CurvePoint { Accuracy = 0.9925, Multiplier = 2.685667856592722 },
            new CurvePoint { Accuracy = 0.99375, Multiplier = 2.9190155639254955 },
            new CurvePoint { Accuracy = 0.995, Multiplier = 3.2022017597337955 },
            new CurvePoint { Accuracy = 0.99625, Multiplier = 3.5526145337555373 },
            new CurvePoint { Accuracy = 0.9975, Multiplier = 3.996793606763322 },
            new CurvePoint { Accuracy = 0.99825, Multiplier = 4.325027383589547 },
            new CurvePoint { Accuracy = 0.999, Multiplier = 4.715470646416203 },
            new CurvePoint { Accuracy = 0.9995, Multiplier = 5.019543595874787 },
            new CurvePoint { Accuracy = 1.0, Multiplier = 5.367394282890631 }
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
    }
}