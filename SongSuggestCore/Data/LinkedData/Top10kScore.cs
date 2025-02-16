﻿using Newtonsoft.Json;
using SongLibraryNS;
using System;


namespace LinkedData
{
    public class Top10kScore
    {
        public string songID { get; set; }
        public float pp { get; set; }
        public int rank { get; set; }
        [JsonIgnore]
        public Top10kPlayer parent {get;set;}
    }
}