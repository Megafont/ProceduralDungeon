using System.Collections;
using System.Collections.Generic;

using UnityEngine;




namespace ProceduralDungeon.TileMaps
{
    public static class ScriptableRoomUtilities
    {
        /// <summary>
        /// Gets the room file name and path as a relative path.
        /// </summary>
        /// <param name="roomName">The name of the room.</param>
        /// <returns>The room file name and path as a relative path starting with the Assets/ folder and all subfolders.</returns>
        public static string GetRoomFilePath(string roomName, string roomSet)
        {
            return $"Assets/Resources/Rooms/{roomSet}/{roomName}.asset";
        }

        public static string GetRoomFileLoadPath(string roomName, string roomSet)
        {
            return $"Rooms/{roomSet}/{roomName}";
        }

        public static string GetRoomSetPath(string roomSet)
        {
            return $"Assets/Resources/Rooms/{roomSet}";
        }

        public static string GetRoomSetPrefabsPath(string roomSet)
        {
            return $"Prefabs/{roomSet}";
        }

        public static string GetRoomSetSpritesPath(string roomSet)
        {
            return $"Sprites/{roomSet}";
        }

        public static string GetRoomSetTilesPath(string roomSet)
        {
            return $"Tiles/{roomSet}";
        }

    }

}

