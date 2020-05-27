﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRrhythmLeague.LeagueAPI
{
    /// <summary>
    /// This is a patch of the method <see cref="PlaylistsViewController.SetData(IAnnotatedBeatmapLevelCollection[], int, bool)"/>
    /// TODO: Remove this or replace it with your own.
    /// </summary>
    [HarmonyPatch(typeof(AnnotatedBeatmapLevelCollectionsViewController), "SetData",
        new Type[] { // Specify the types of SetDataFromLevelAsync's parameters here.
        typeof(IAnnotatedBeatmapLevelCollection[]), typeof(int), typeof(bool)})]
    class PlaylistCollectionOverride
    {
        private static IAnnotatedBeatmapLevelCollection[] loadedPlaylists;
        /// <summary>
        /// Adds this plugin's name to the beginning of the author text in the song list view.
        /// </summary>
        static void Prefix(ref IAnnotatedBeatmapLevelCollection[] annotatedBeatmapLevelCollections)
        {
            if (annotatedBeatmapLevelCollections[0].GetType().Equals(typeof(UserFavoritesPlaylistSO))) //Checks if this is the playlists view
            {
                if (loadedPlaylists == null)
                    refreshPlaylists();
                IAnnotatedBeatmapLevelCollection[] tempplaylists = new IAnnotatedBeatmapLevelCollection[loadedPlaylists.Length];

                int j = 0;
                for (int i = 0; i < tempplaylists.Length; i++)
                {
                    tempplaylists[i] = loadedPlaylists[j++];
                }
                annotatedBeatmapLevelCollections = tempplaylists;
            }
        }

        public static void refreshPlaylists()
        {
            LeagueRequestHandler.Instance.getPlaylist();
        }
    }
}
