using System;

namespace Data
{

    public class FilesMeta
    {
        public string top10kVersion { get; set; } = "0.0";
        public string songLibraryVersion { get; set; } = "0.0";
        public DateTime top10kUpdated { get; set; }
        public long beatLeaderLeaderboardUpdated { get; set; } = 0;
        public long beatLeaderSongsUpdated { get; set; } = 0;

        //Deprecated, use the String version and later rework old checks to new when playerData is updated.
        public String GetLargeVersion()
        {
            return top10kVersion.Split('.')[0];
        }

        //Get the major version of a string.
        public String Major(FilesMetaType type)
        {
            if (type == FilesMetaType.Top10kVersion) return GetMajorVersion(top10kVersion);
            if (type == FilesMetaType.SongLibraryVersion) return GetMajorVersion(songLibraryVersion);
            return null;
        }

        //Get the minor version of a string.
        public String Minor(FilesMetaType type)
        {
            if (type == FilesMetaType.Top10kVersion) return GetMinorVersion(top10kVersion);
            if (type == FilesMetaType.SongLibraryVersion) return GetMinorVersion(songLibraryVersion);
            return null;
        }

        public void UpdateMajor(FilesMetaType type)
        {
            string major = "";
            if (type == FilesMetaType.Top10kVersion) major = GetMajorVersion(top10kVersion);
            if (type == FilesMetaType.SongLibraryVersion) major =  GetMajorVersion(songLibraryVersion);

            string newVersion = $"{int.Parse(major)+1}.0";
            
            if (type == FilesMetaType.Top10kVersion) top10kVersion = top10kVersion = newVersion;
            if (type == FilesMetaType.SongLibraryVersion) songLibraryVersion = songLibraryVersion = newVersion;
        }

        public void UpdateMinor(FilesMetaType type)
        {
            string major = "";
            string minor = "";
            if (type == FilesMetaType.Top10kVersion)
            {
                major = GetMajorVersion(top10kVersion);
                minor = GetMinorVersion(top10kVersion); 
            }

            if (type == FilesMetaType.SongLibraryVersion)
            {
                major = GetMajorVersion(songLibraryVersion);
                minor = GetMinorVersion(songLibraryVersion);
            }

            string newVersion = $"{major}.{int.Parse(minor) + 1}";

            if (type == FilesMetaType.Top10kVersion) top10kVersion = top10kVersion = newVersion;
            if (type == FilesMetaType.SongLibraryVersion) songLibraryVersion = songLibraryVersion = newVersion;
        }



        private String GetMajorVersion(String version)
        {
            return version.Split('.')[0]; ;
        }

        private String GetMinorVersion(String version)
        {
            return version.Split('.')[1]; ;
        }
    }

    public enum FilesMetaType
    {
        Top10kVersion = 1,
        SongLibraryVersion = 2,
    }
}