using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;


namespace ProceduralDungeon.TileMaps
{

    /// <summary>
    /// This data class holds references to the Tilemaps that make up a dungeon map or room map.
    /// </summary>
    public class DungeonMap
    {
        public Tilemap FloorsMap { get { return _FloorsMap; } }
        public Tilemap WallsMap { get { return _WallsMap; } }
        public Tilemap Placeholders_General_Map { get { return _Placeholders_General_Map; } }
        public Tilemap Placeholders_Items_Map { get { return _Placeholders_Items_Map; } }
        public Tilemap Placeholders_Enemies_Map { get { return _Placeholders_Enemies_Map; } }



        private Tilemap _FloorsMap;
        private Tilemap _WallsMap;
        private Tilemap _Placeholders_Enemies_Map;
        private Tilemap _Placeholders_Items_Map;
        private Tilemap _Placeholders_General_Map;



        public DungeonMap(Tilemap floors, Tilemap walls, Tilemap placeholders_General, Tilemap placeholders_Items, Tilemap placeholders_Enemies)
        {
            Assert.IsNotNull(floors, "DungeonMap: The floors map field is null!");
            Assert.IsNotNull(walls, "DungeonMap: The walls map field is null!");
            Assert.IsNotNull(placeholders_General, "DungeonMap: The placeholders map field is null!");
            Assert.IsNotNull(placeholders_Items, "DungeonMap: The items map field is null!");
            Assert.IsNotNull(placeholders_Enemies, "DungeonMap: The enemies map field is null!");

            _FloorsMap = floors;
            _WallsMap = walls;
            _Placeholders_General_Map = placeholders_General;
            _Placeholders_Items_Map = placeholders_Items;
            _Placeholders_Enemies_Map = placeholders_Enemies;
        }


        public void ClearTileMap(TileMapTypes tileMapToClear)
        {
            switch (tileMapToClear)
            {
                case TileMapTypes.Floors:
                    _FloorsMap.ClearAllTiles();
                    break;
                case TileMapTypes.Walls:
                    _WallsMap.ClearAllTiles();
                    break;
                case TileMapTypes.Placeholders_General:
                    _Placeholders_General_Map.ClearAllTiles();
                    break;
                case TileMapTypes.Placeholders_Items:
                    _Placeholders_Items_Map.ClearAllTiles();
                    break;
                case TileMapTypes.Placeholders_Enemies:
                    _Placeholders_Enemies_Map.ClearAllTiles();
                    break;
            }
        }

        public void ClearAllTileMaps()
        {
            _FloorsMap.ClearAllTiles();
            _WallsMap.ClearAllTiles();
            _Placeholders_General_Map.ClearAllTiles();
            _Placeholders_Items_Map.ClearAllTiles();
            _Placeholders_Enemies_Map.ClearAllTiles();
        }

        public bool FillTileMapsWithTileData(ScriptableRoom loadedRoom)
        {
            ClearAllTileMaps();

            bool hadError = false;


            if (!CopyTileDataIntoTileMap(loadedRoom.FloorTiles, _FloorsMap, TileMapTypes.Floors))
                hadError = true;

            if (!CopyTileDataIntoTileMap(loadedRoom.WallTiles, _WallsMap, TileMapTypes.Walls))
                hadError = true;

            if (!CopyTileDataIntoTileMap(loadedRoom.Placeholders_General_Tiles, _Placeholders_General_Map, TileMapTypes.Placeholders_General))
                hadError = true;

            if (!CopyTileDataIntoTileMap(loadedRoom.Placeholders_Item_Tiles, _Placeholders_Items_Map, TileMapTypes.Placeholders_Items))
                hadError = true;

            if (!CopyTileDataIntoTileMap(loadedRoom.Placeholders_Enemy_Tiles, _Placeholders_Enemies_Map, TileMapTypes.Placeholders_Enemies))
                hadError = true;


            return !hadError;
        }


        public bool GetSaveDataFromTileMaps(ScriptableRoom roomData)
        {
            bool hadError = false;
            bool temp = false;


            roomData.FloorTiles = GetTileDataFromMap(TileMapTypes.Floors, out temp);
            if (temp) { hadError = true; }

            roomData.WallTiles = GetTileDataFromMap(TileMapTypes.Walls, out temp);
            if (temp) { hadError = true; }

            roomData.Placeholders_General_Tiles = GetTileDataFromMap(TileMapTypes.Placeholders_General, out temp);
            if (temp) { hadError = true; }

            roomData.Placeholders_Item_Tiles = GetTileDataFromMap(TileMapTypes.Placeholders_Items, out temp);
            if (temp) { hadError = true; }

            roomData.Placeholders_Enemy_Tiles = GetTileDataFromMap(TileMapTypes.Placeholders_Enemies, out temp);
            if (temp) { hadError = true; }


            return !hadError;
        }

        public void CompressBoundsOfAllTileMaps()
        {
            _FloorsMap.CompressBounds();
            _WallsMap.CompressBounds();
            _Placeholders_General_Map.CompressBounds();
            _Placeholders_Items_Map.CompressBounds();
            _Placeholders_Enemies_Map.CompressBounds();
        }



        private bool CopyTileDataIntoTileMap(List<SavedTile> tileData, Tilemap tileMap, TileMapTypes tileMapType)
        {
            bool hadError = false;
            bool tileError = false;


            foreach (SavedTile sTile in tileData)
            {
                tileError = false;

                int tileType = (int)sTile.Tile.TileType;


                switch (tileMapType)
                {
                    case TileMapTypes.Floors:
                        if ((tileType < (int)RoomTileCategoryRanges.FLOORS_START || tileType > (int)RoomTileCategoryRanges.FLOORS_END))
                        {
                            Debug.LogError(String.Format("DungeonMap.CopyTileDataIntoTileMap() - Encountered invalid floor tile type \"{0}\" at position {1} while copying loaded tile data into the tilemap! This tile was ignored.", Enum.GetName(typeof(RoomTileTypes), sTile.Tile.TileType), sTile.Position));
                            tileError = true;
                        }
                        break;

                    case TileMapTypes.Walls:
                        if ((tileType < (int)RoomTileCategoryRanges.WALLS_START || tileType > (int)RoomTileCategoryRanges.WALLS_END))
                        {
                            Debug.LogError(String.Format("DungeonMap.CopyTileDataIntoTileMap() - Encountered invalid wall tile type \"{0}\" at position {1} while copying loaded tile data into the tilemap! This tile was ignored.", Enum.GetName(typeof(RoomTileTypes), sTile.Tile.TileType), sTile.Position));
                            tileError = true;
                        }
                        break;

                    case TileMapTypes.Placeholders_General:
                        if ((tileType < (int)RoomTileCategoryRanges.PLACEHOLDERS_GENERAL_START || tileType > (int)RoomTileCategoryRanges.PLACEHOLDERS_GENERAL_END))
                        {
                            Debug.LogError(String.Format("DungeonMap.CopyTileDataIntoTileMap() - Encountered invalid placeholder tile type \"{0}\" at position {1} while copying loaded tile data into the tilemap! This tile was ignored.", Enum.GetName(typeof(RoomTileTypes), sTile.Tile.TileType), sTile.Position));
                            tileError = true;
                        }
                        break;

                    case TileMapTypes.Placeholders_Items:
                        if ((tileType < (int)RoomTileCategoryRanges.PLACEHOLDERS_ITEMS_START || tileType > (int)RoomTileCategoryRanges.PLACEHOLDERS_ITEMS_END))
                        {
                            Debug.LogError(String.Format("DungeonMap.CopyTileDataIntoTileMap() - Encountered invalid item placeholder tile type \"{0}\" at position {1} while copying loaded tile data into the tilemap! This tile was ignored.", Enum.GetName(typeof(RoomTileTypes), sTile.Tile.TileType), sTile.Position));
                            tileError = true;
                        }
                        break;

                    case TileMapTypes.Placeholders_Enemies:
                        if (tileType < (int)RoomTileCategoryRanges.PLACEHOLDERS_ENEMIES_START || tileType > (int)RoomTileCategoryRanges.PLACEHOLDERS_ENEMIES_END)
                        {
                            Debug.LogError(String.Format("DungeonMap.CopyTileDataIntoTileMap() - Encountered invalid enemy placeholder tile type \"{0}\" at position {1} while copying loaded tile data into the tilemap! This tile was ignored.", Enum.GetName(typeof(RoomTileTypes), sTile.Tile.TileType), sTile.Position));
                            tileError = true;
                        }
                        break;

                } // end switch


                if (!tileError)
                {
                    tileMap.SetTile(sTile.Position, sTile.Tile);

                    tileMap.SetTransformMatrix(sTile.Position,
                         Matrix4x4.TRS(Vector3.zero, sTile.Rotation, Vector3.one)); // We set the position parameter to Vector3.zero, as we don't want to add any offset to the tile's position.
                }
                else
                {
                    hadError = true;
                }

            } // end foreach


            return !hadError;

        }

        private List<SavedTile> GetTileDataFromMap(TileMapTypes tileMapToGetDataFrom, out bool hadError)
        {
            List<SavedTile> tileData = new List<SavedTile>();

            hadError = false;

            bool tileError = false;

            Tilemap map = GetMap(tileMapToGetDataFrom);

            foreach (Vector3Int pos in map.cellBounds.allPositionsWithin)
            {
                tileError = false;

                if (map.HasTile(pos))
                {
                    RoomTile tile = map.GetTile<RoomTile>(pos);
                    int type = (int)tile.TileType;


                    if (map == _FloorsMap &&
                        (type < (int)RoomTileCategoryRanges.FLOORS_START || type > (int)RoomTileCategoryRanges.FLOORS_END))
                    {
                        Debug.LogError(String.Format("DungeonMap.GetTileDataFromMap() - Encountered invalid floor tile type \"{0}\" at position {1} while getting data from tile map! This tile was ignored.", Enum.GetName(typeof(RoomTileTypes), tile.TileType), pos));
                        tileError = true;
                    }
                    else if (map == _WallsMap &&
                        (type < (int)RoomTileCategoryRanges.WALLS_START || type > (int)RoomTileCategoryRanges.WALLS_END))
                    {
                        Debug.LogError(String.Format("DungeonMap.GetTileDataFromMap() - Encountered invalid wall tile type \"{0}\" at position {1} getting data from tile map! This tile was ignored.", Enum.GetName(typeof(RoomTileTypes), tile.TileType), pos));
                        tileError = true;
                    }
                    else if (map == _Placeholders_General_Map &&
                        (type < (int)RoomTileCategoryRanges.PLACEHOLDERS_GENERAL_START || type > (int)RoomTileCategoryRanges.PLACEHOLDERS_GENERAL_END))
                    {
                        Debug.LogError(String.Format("DungeonMap.GetTileDataFromMap() - Encountered invalid placeholder tile type \"{0}\" at position {1} while getting data from tile map! This tile was ignored.", Enum.GetName(typeof(RoomTileTypes), tile.TileType), pos));
                        tileError = true;
                    }
                    else if (map == _Placeholders_Items_Map &&
                        (type < (int)RoomTileCategoryRanges.PLACEHOLDERS_ITEMS_START || type > (int)RoomTileCategoryRanges.PLACEHOLDERS_ITEMS_END))
                    {
                        Debug.LogError(String.Format("DungeonMap.GetTileDataFromMap() - Encountered invalid item tile type \"{0}\" a position {1} while getting data from tile map! This tile was ignored.", Enum.GetName(typeof(RoomTileTypes), tile.TileType), pos));
                        tileError = true;
                    }
                    else if (map == _Placeholders_Enemies_Map &&
                        (type < (int)RoomTileCategoryRanges.PLACEHOLDERS_ENEMIES_START || type > (int)RoomTileCategoryRanges.PLACEHOLDERS_ENEMIES_END))
                    {
                        Debug.LogError(String.Format("DungeonMap.GetTileDataFromMap() - Encountered invalid enemy tile type \"{0}\" at position {1} while getting data from tile map! This tile was ignored.", Enum.GetName(typeof(RoomTileTypes), tile.TileType), pos));
                        tileError = true;
                    }


                    if (!tileError)
                    {
                        SavedTile sTile = new SavedTile(tile, pos, map.GetTransformMatrix(pos).rotation);
                        tileData.Add(sTile);
                    }
                    else
                    {
                        hadError = true;
                    }
                }

            } // end foreach


            return tileData;
        }


        private Tilemap GetMap(TileMapTypes mapToGet)
        {
            switch (mapToGet)
            {
                case TileMapTypes.Floors:
                    return _FloorsMap;
                case TileMapTypes.Walls:
                    return _WallsMap;
                case TileMapTypes.Placeholders_General:
                    return _Placeholders_General_Map;
                case TileMapTypes.Placeholders_Items:
                    return _Placeholders_Items_Map;
                case TileMapTypes.Placeholders_Enemies:
                    return _Placeholders_Enemies_Map;

                default:
                    return null;

            } // end switch

        }


    }

}
