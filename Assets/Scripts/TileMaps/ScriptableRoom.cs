using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;


namespace ProceduralDungeon.TileMaps
{
    /// <summary>
    /// Enumerates the tile maps that make up a dungeon room.
    /// </summary>
    public enum TileMapTypes
    {
        Floors,
        Walls,
        Placeholders_Objects,
        Placeholders_Items,
        Placeholders_Enemies,
    }



    /// <summary>
    /// This scriptable object is used for storing a room tilemap as a resource.
    /// </summary>
    public class ScriptableRoom : ScriptableObject
    {
        public List<SavedTile> FloorTiles;
        public List<SavedTile> WallTiles;
        public List<SavedTile> Placeholders_Object_Tiles;
        public List<SavedTile> Placeholders_Item_Tiles;
        public List<SavedTile> Placeholders_Enemy_Tiles;

        public string RoomName;
        public RoomLevels RoomLevel = RoomLevels.Level_1stFloor;
        public RoomTypeFlags RoomTypeFlags = 0;



        public BoundsInt GetBoundsFromTileList(List<SavedTile> tiles)
        {
            BoundsInt bounds = new BoundsInt();

            foreach (SavedTile sTile in tiles)
            {
                int x = sTile.Position.x;
                int y = sTile.Position.y;

                if (x < bounds.xMin)
                    bounds.xMin = x;
                if (x > bounds.xMax)
                    bounds.xMax = x;

                if (y < bounds.yMin)
                    bounds.yMin = y;
                if (y > bounds.yMax)
                    bounds.yMax = y;

            } // end foreach


            return bounds;
        }


    }


}