using SongSuggestNS;

namespace PlayerScores
{
    public static class ModifierParser
    {
        public static SongModifier Parse(string modifierString)
        {
            SongModifier modifiers = 0;

            foreach (var part in modifierString.Split(','))
            {
                switch (part.Trim().ToUpperInvariant())
                {
                    case "SS": modifiers |= SongModifier.SS; break;
                    case "FS": modifiers |= SongModifier.FS; break;
                    case "SF": modifiers |= SongModifier.SF; break;
                    case "IF": modifiers |= SongModifier.IF; break;
                    case "BE": modifiers |= SongModifier.BE; break;
                    case "NF": modifiers |= SongModifier.NF; break;
                    case "PM": modifiers |= SongModifier.PM; break;
                    case "SA": modifiers |= SongModifier.SA; break;
                    case "SC": modifiers |= SongModifier.SC; break;
                    case "DA": modifiers |= SongModifier.DA; break;
                    case "GN": modifiers |= SongModifier.GN; break;
                    case "NO": modifiers |= SongModifier.NO; break;
                    case "NB": modifiers |= SongModifier.NB; break;
                    case "NA": modifiers |= SongModifier.NA; break;
                    case "OD": modifiers |= SongModifier.OD; break;
                    case "OP": modifiers |= SongModifier.OP; break;
                    case "": break;
                    default: SongSuggest.Log?.WriteLine($"Unknown modifier: '{part}'"); break;
                }
            }
            return modifiers;
        }
    }
}