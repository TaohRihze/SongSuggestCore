using System;

namespace SongSuggestNS
{
    [Flags]
    public enum SongModifier
    {
        SS = 1,        //Slower Song
        FS = 2,        //Faster Song
        SF = 4,        //Super Fast

        IF = 32,       //Insta Fail/1 Life
        BE = 64,       //Battery Energy/4 Life
        NF = 128,      //No Fail/Infinite Life

        PM = 1024,     //Pro Mode
        SA = 2048,     //Strict Angles
        SC = 4096,     //Small Cubes

        DA = 32768,    //Disappearing Arrows
        GN = 65536,    //Ghost Notes

        NO = 524288,   //No Obstacles (Walls) 
        NB = 1048576,  //No Bombs
        NA = 2097152,  //No Arrows

        //Beat Leader Specific.
        OD = 16777216, //Old Dots
        OP = 33554432, //Over Platform
    }
}
