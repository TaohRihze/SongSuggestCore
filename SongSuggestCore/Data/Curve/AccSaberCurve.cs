using SongLibraryNS;
using System.Collections.Generic;
using System.Linq;

namespace Curve
{
    public class AccSaberCurve
    {
        private static readonly List<CurvePoint> curvePoints = new List<CurvePoint>
        {
            new CurvePoint { Accuracy = 0, Multiplier = 0 },
            new CurvePoint { Accuracy = 0.9409324581850277, Multiplier = 0.22864617746193472 },
            new CurvePoint { Accuracy = 0.9421364984358537, Multiplier = 0.2344589535514457 },
            new CurvePoint { Accuracy = 0.943340858876901, Multiplier = 0.24028320272237846 },
            new CurvePoint { Accuracy = 0.9445528451823, Multiplier = 0.24615905401143912 },
            new CurvePoint { Accuracy = 0.9457521950057954, Multiplier = 0.2519929536323373 },
            new CurvePoint { Accuracy = 0.9469652757511278, Multiplier = 0.25791818908545416 },
            new CurvePoint { Accuracy = 0.9481613689691947, Multiplier = 0.26378971348304325 },
            new CurvePoint { Accuracy = 0.9493682127874609, Multiplier = 0.26974882033448855 },
            new CurvePoint { Accuracy = 0.9505744372202971, Multiplier = 0.27574542941084734 },
            new CurvePoint { Accuracy = 0.9517783524884541, Multiplier = 0.2817769019690621 },
            new CurvePoint { Accuracy = 0.9529892330175649, Multiplier = 0.287896269816159 },
            new CurvePoint { Accuracy = 0.9541947185853665, Multiplier = 0.2940478274623703 },
            new CurvePoint { Accuracy = 0.9554044516127758, Multiplier = 0.30028781869718807 },
            new CurvePoint { Accuracy = 0.9566054381494079, Multiplier = 0.30655637705110717 },
            new CurvePoint { Accuracy = 0.957807698698665, Multiplier = 0.31291317977345784 },
            new CurvePoint { Accuracy = 0.9590221672423604, Multiplier = 0.3194262585871035 },
            new CurvePoint { Accuracy = 0.9602231628864696, Multiplier = 0.32596720585696376 },
            new CurvePoint { Accuracy = 0.961433998563471, Multiplier = 0.3326729095043305 },
            new CurvePoint { Accuracy = 0.9626279572859802, Multiplier = 0.339405506964074 },
            new CurvePoint { Accuracy = 0.963842342827163, Multiplier = 0.3463882776436415 },
            new CurvePoint { Accuracy = 0.965050103040447, Multiplier = 0.3534815900715788 },
            new CurvePoint { Accuracy = 0.96624960935703, Multiplier = 0.36068799949007235 },
            new CurvePoint { Accuracy = 0.9674529869368587, Multiplier = 0.3680959084197356 },
            new CurvePoint { Accuracy = 0.9686591348667645, Multiplier = 0.3757183982279203 },
            new CurvePoint { Accuracy = 0.9698668993297, Multiplier = 0.38356990598376894 },
            new CurvePoint { Accuracy = 0.9710750806787853, Multiplier = 0.39166641973515987 },
            new CurvePoint { Accuracy = 0.9722824425660789, Multiplier = 0.4000257103100814 },
            new CurvePoint { Accuracy = 0.9734877233230004, Multiplier = 0.408667608162045 },
            new CurvePoint { Accuracy = 0.9746896497445529, Multiplier = 0.41761433619185573 },
            new CurvePoint { Accuracy = 0.9759017934808459, Multiplier = 0.42700834856603836 },
            new CurvePoint { Accuracy = 0.977108280868104, Multiplier = 0.43677295486970896 },
            new CurvePoint { Accuracy = 0.9783078742661878, Multiplier = 0.44694216753773164 },
            new CurvePoint { Accuracy = 0.9795145289674262, Multiplier = 0.4576932612610576 },
            new CurvePoint { Accuracy = 0.9807121922419519, Multiplier = 0.46894998821359746 },
            new CurvePoint { Accuracy = 0.981930401543003, Multiplier = 0.48108071091217086 },
            new CurvePoint { Accuracy = 0.9831227248036967, Multiplier = 0.4937134311802671 },
            new CurvePoint { Accuracy = 0.9843344315883069, Multiplier = 0.5074369975183908 },
            new CurvePoint { Accuracy = 0.9855345565106794, Multiplier = 0.5220472890164811 },
            new CurvePoint { Accuracy = 0.9867538435462135, Multiplier = 0.5381018792987169 },
            new CurvePoint { Accuracy = 0.9879462160499057, Multiplier = 0.5551918317559756 },
            new CurvePoint { Accuracy = 0.989158767583543, Multiplier = 0.5742496799950565 },
            new CurvePoint { Accuracy = 0.9903616429313051, Multiplier = 0.5951727896238259 },
            new CurvePoint { Accuracy = 0.9915723216173138, Multiplier = 0.6187204908473445 },
            new CurvePoint { Accuracy = 0.9927779343719173, Multiplier = 0.6452713618738384 },
            new CurvePoint { Accuracy = 0.9939826353978779, Multiplier = 0.6757582832177143 },
            new CurvePoint { Accuracy = 0.9951928260723995, Multiplier = 0.7116318568448161 },
            new CurvePoint { Accuracy = 0.99638391362715, Multiplier = 0.7539893920553304 },
            new CurvePoint { Accuracy = 0.9975978174482817, Multiplier = 0.8078649708462118 },
            new CurvePoint { Accuracy = 0.9988016676122579, Multiplier = 0.8810362590039038 },
            new CurvePoint { Accuracy = 0.9997988680153226, Multiplier = 1 },
            new CurvePoint { Accuracy = 1, Multiplier = 1 }
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
        public static double AP(double accuracy, double complexityRating)
        {
            if (accuracy < 0 || accuracy > 1) return 0.0;
            if (complexityRating == 0) return 0;
            return Multiplier(accuracy) * (complexityRating + 18) * 61;
        }

        public static double AP(double accuracy, SongID songID)
        {
            Song song = SongLibrary.SongIDToSong(songID);
            if (song == null) return 0;
            return AP(accuracy, song.complexityAccSaber);
        }
    }
}