using System;

namespace Settings
{
    public class FilterSettings
    {
        [Obsolete("This modifier has been disabled effective, the variable will be removed in a future version.")]
        public double modifierPP { get; set; } = 0.0;
        public double modifierStyle { get; set; } = 100.0;
        public double modifierOverweight { get; set; } = 81.0;
    }
}
