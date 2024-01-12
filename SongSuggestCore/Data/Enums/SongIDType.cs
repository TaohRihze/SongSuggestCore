namespace SongLibraryNS
{
    //Different songIDs based on active sources
    public enum SongIDType
    {
        Internal,       //Internal ID created by hash, difc, characteristic
        ScoreSaber,     //ScoreSaberID
        BeatLeader,     //BeatLeaderID
    }
}