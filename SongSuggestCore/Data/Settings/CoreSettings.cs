using System.IO;

namespace Settings
{
    public class CoreSettings
    {
        public string UserID { get; set; } = "-1";
        public FilePathSettings FilePathSettings { get; set; }
        public TextWriter Log { get; set; } = null;
        public bool UseScoreSaberLeaderboard { get; set; } = true;
        public bool UpdateScoreSaberLeaderboard { get; set; } = true;
        public bool UseAccSaberLeaderboard { get; set; } = true;
        public bool UpdateAccSaberLeaderboard { get; set; } = true;
        public bool UseBeatLeaderLeaderboard { get; set; } = true;
        public bool UpdateBeatLeaderLeaderboard { get; set; } = true;
        //Removes plays with Score Saber shared songs only in the Beat Leader score data.
        public bool FilterScoreSaberBiasInBeatLeader { get; set; } = true;
    }
}
