using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction.PlaceholderUtilities;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.DungeonGeneration.DungeonConstruction
{
    /// <summary>
    /// This subclass of scriptable room is used within the dungeon generator.
    /// It adds storage for extended room data.
    /// </summary>
    public class RoomData
    {
        public Dictionary<Vector3Int, SavedTile> FloorTiles;
        public Dictionary<Vector3Int, SavedTile> WallTiles;
        public Dictionary<Vector3Int, SavedTile> Placeholders_General_Tiles;
        public Dictionary<Vector3Int, SavedTile> Placeholders_Item_Tiles;
        public Dictionary<Vector3Int, SavedTile> Placeholders_Enemy_Tiles;


        public string RoomName;
        public RoomLevels RoomLevel = RoomLevels.Level_1stFloor;
        public RoomTypeFlags RoomTypeFlags = 0;


        public List<DoorData> DoorsList;



        public RoomData(ScriptableRoom loadedRoom)
        {
            Assert.IsNotNull(loadedRoom, "RoomData.RoomData() - The passed in room data is null!");


            DoorsList = new List<DoorData>();


            FloorTiles = new Dictionary<Vector3Int, SavedTile>();
            WallTiles = new Dictionary<Vector3Int, SavedTile>();
            Placeholders_General_Tiles = new Dictionary<Vector3Int, SavedTile>();
            Placeholders_Item_Tiles = new Dictionary<Vector3Int, SavedTile>();
            Placeholders_Enemy_Tiles = new Dictionary<Vector3Int, SavedTile>();

            MiscellaneousUtils.CopyTilesListToDictionary(loadedRoom.FloorTiles, FloorTiles);
            MiscellaneousUtils.CopyTilesListToDictionary(loadedRoom.WallTiles, WallTiles);
            MiscellaneousUtils.CopyTilesListToDictionary(loadedRoom.Placeholders_General_Tiles, Placeholders_General_Tiles);
            MiscellaneousUtils.CopyTilesListToDictionary(loadedRoom.Placeholders_Item_Tiles, Placeholders_Item_Tiles);
            MiscellaneousUtils.CopyTilesListToDictionary(loadedRoom.Placeholders_Enemy_Tiles, Placeholders_Enemy_Tiles);


            RoomName = loadedRoom.RoomName;
            RoomLevel = loadedRoom.RoomLevel;
            RoomTypeFlags = loadedRoom.RoomTypeFlags;

        }



        public static List<SavedTile> GetTilesOfType(Dictionary<Vector3Int, SavedTile> srcTilesDict, List<RoomTileTypes> typesToGet)
        {
            List<SavedTile> sTiles = new List<SavedTile>();

            foreach (KeyValuePair<Vector3Int, SavedTile> pair in srcTilesDict)
            {
                SavedTile srcTile = pair.Value;

                if (typesToGet.Contains(srcTile.Tile.TileType))
                    sTiles.Add(srcTile);
            }

            return sTiles;
        }

        public static List<SavedTile> GetNeighborsOfTile(List<SavedTile> srcTilesList, SavedTile tileToGetNeighborsOf)
        {
            List<SavedTile> sTiles = new List<SavedTile>();

            foreach (SavedTile srcTile in srcTilesList)
            {
                Vector3Int pos = srcTile.Position;


                if ((srcTile.Position == tileToGetNeighborsOf.Position + Vector3Int.up) || // Is this tile the north neighbor?
                     (srcTile.Position == tileToGetNeighborsOf.Position + Vector3Int.down) || // Is this tile the south neighbor?
                     (srcTile.Position == tileToGetNeighborsOf.Position + Vector3Int.right) || // Is this tile the east neighbor?
                     (srcTile.Position == tileToGetNeighborsOf.Position + Vector3Int.left)) // Is this tile the west neighbor?
                {
                    sTiles.Add(srcTile);
                }
            }

            return sTiles;
        }


    }

}

