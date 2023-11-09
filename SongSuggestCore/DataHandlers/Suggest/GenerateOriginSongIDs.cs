using System;
using System.Collections.Generic;
using System.Linq;

namespace Actions
{
    public static class GenerateOriginSongIDs
    {
        internal static void Execute(RankedSongSuggest.DTO dto, out List<string> originSongIDs)
        {
            originSongIDs = new List<string>();
            //Add Liked songs.
            dto.log?.WriteLine($"Use Liked Songs: {dto.useLikedSongs}");

            if (dto.useLikedSongs) originSongIDs.AddRange(dto.suggestSM.LikedSongs());
            int targetCount = originSongIDs.Count();

            dto.log?.WriteLine($"Liked Songs in list: {originSongIDs.Count()}");

            //Add the standard origin songs if either normal mode of filler is activated
            if (!dto.useLikedSongs || dto.fillLikedSongs)
            {
                //update targetsongs to either originSongsCount, or liked songs total, whichever is larger
                targetCount = Math.Max(dto.originSongsCount, targetCount);

                originSongIDs.AddRange(dto.playedOriginSongs);

                originSongIDs = originSongIDs
                    .Distinct()         //Remove Duplicates
                    .ToList();

                dto.log?.WriteLine("Liked + Played Songs in list: " + originSongIDs.Count());

                //If there is no originSongs found (no played in the group, or all permabanned) we some default songs to give suggestions from.
                if (originSongIDs.Count == 0)
                {
                    //Too few source songs, so we set a warning, and get the filler songs (which activates the Limit Breaker).
                    dto.log?.WriteLine($"No played songs found, so Filler Songs Activated along with Limit Break.");

                    //Add the filler songs to the currently found
                    originSongIDs.AddRange(dto.fillerSongs);

                    //Remove any duplicates and reduce the list to target filler count.
                    originSongIDs = originSongIDs
                        .Distinct()         //Remove Duplicates
                        .Take(dto.targetFillers)
                        .ToList();

                    dto.log?.WriteLine("Liked + Played + Filler Songs in list: " + originSongIDs.Count());
                }

                originSongIDs = originSongIDs
                    .Take(targetCount)  //Try and get originSongsCount or all liked whichever is larger
                    .ToList();
                dto.log?.WriteLine("Final Songs in list: " + originSongIDs.Count());
            }
        }
    }
}