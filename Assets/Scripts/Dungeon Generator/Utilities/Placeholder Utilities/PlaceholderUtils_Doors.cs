using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration.Utilities;
using ProceduralDungeon.TileMaps;

using SavedTileDictionary = System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, ProceduralDungeon.TileMaps.SavedTile>;


namespace ProceduralDungeon.DungeonGeneration.Utilities.PlaceholderUtilities
{
    public struct DoorData
    {
        public Directions DoorDirection; // The direction this door is facing.
        public RoomLevels DoorLevel; // The level this door is on.


        // The positions of the two tiles of this door. The first one is always the upper-left-most of the two tiles.
        public Vector3Int Tile1Position;
        public Vector3Int Tile2Position;

    }



    public static class PlaceholderUtils_Doors
    {
        private static RoomData _CurrentRoomData;

        private static bool _ErrorOccurred;



        static List<RoomTileTypes> _DoorTypes = new List<RoomTileTypes>()
        {
            RoomTileTypes.Placeholders_Doors_Basement,
            RoomTileTypes.Placeholders_Doors_1stFloor,
            RoomTileTypes.Placeholders_Doors_2ndFloor,
        };

        static List<RoomTileTypes> _FloorTypes_Basement = new List<RoomTileTypes>()
        {
            RoomTileTypes.Floors_Basement,
        };

        static List<RoomTileTypes> _FloorTypes_1stFloor = new List<RoomTileTypes>()
        {
            RoomTileTypes.Floors_1stFloor,
        };

        static List<RoomTileTypes> _FloorTypes_2ndFloor = new List<RoomTileTypes>()
        {
            RoomTileTypes.Floors_2ndFloor,
        };



        public static void DetectDoorLocations(List<RoomData> roomsList)
        {
            Assert.IsNotNull(roomsList, "The passed in rooms list is null!");


            foreach (RoomData room in roomsList)
            {
                _CurrentRoomData = room;
                FindRoomDoors(room.PlaceholderTiles, room.FloorTiles);

            } // end foreach

        }


        /// <summary>
        /// This public overload is used by the room editor to validate the positions of door placeholders.
        /// </summary>
        /// <param name="placeholderTiles">A list of the room's placeholder tiles.</param>
        /// <param name="floorTiles">A list of the room's floor tiles.</param>
        public static bool FindAndValidateRoomDoors(List<SavedTile> placeholderTiles, List<SavedTile> floorTiles)
        {
            _ErrorOccurred = false;


            Assert.IsNotNull(placeholderTiles, "The passed in list of placeholder tiles is null!");
            Assert.IsNotNull(floorTiles, "The passed in list of floor tiles is null!");

            Assert.IsFalse(placeholderTiles.Count == 0, "The passed in list of placeholder tiles is empty!");
            Assert.IsFalse(floorTiles.Count == 0, "The passed in list of floor tiles is empty!");


            SavedTileDictionary placeholderTilesDict = new SavedTileDictionary();
            SavedTileDictionary floorTilesDict = new SavedTileDictionary();

            MiscellaneousUtils.CopyTilesListToDictionary(placeholderTiles, placeholderTilesDict);
            MiscellaneousUtils.CopyTilesListToDictionary(floorTiles, floorTilesDict);


            // We pass in true for the final parameter to tell this function that it has been invoked from the room editor.
            FindRoomDoors(placeholderTilesDict, floorTilesDict, true);


            return !_ErrorOccurred;
        }

        private static void FindRoomDoors(SavedTileDictionary placeholderTiles, SavedTileDictionary floorTiles, bool calledFromRoomEditor = false)
        {
            List<Vector3Int> visitedDoorTiles = new List<Vector3Int>();

            List<SavedTile> doorPlaceholderTiles = RoomData.GetTilesOfType(placeholderTiles, _DoorTypes);


            foreach (SavedTile sTile in doorPlaceholderTiles)
            {
                RoomTileTypes type = sTile.Tile.TileType;

                // Check that we didn't already visit this tile.
                if (visitedDoorTiles.Contains(sTile.Position))
                    continue;


                DoorData door = GetDoorData(floorTiles, doorPlaceholderTiles, sTile);
                if (!calledFromRoomEditor) // We don't need to add the door if we were called from the room editor, as this can cause a null reference exception.
                    _CurrentRoomData.DoorsList.Add(door);


                visitedDoorTiles.Add(door.Tile1Position);
                visitedDoorTiles.Add(door.Tile2Position);


            } // end foreach

        }

        private static DoorData GetDoorData(SavedTileDictionary floorTiles, List<SavedTile> doorPlaceholderTiles, SavedTile sTile)
        {
            List<SavedTile> neighbors = RoomData.GetNeighborsOfTile(doorPlaceholderTiles, sTile);


            // Debug output:
            /*
            Debug.Log("DOOR PLACEHOLDER POS: " + sTile.Position);
            foreach (SavedTile s in neighbors)
                Debug.Log("    NEIGHBOR POS: " + s.Position);
            */

            DoorData door = new DoorData();


            if (neighbors.Count > 1)
            {
                Debug.LogError($"PlaceholderUtility_Doors.GetDoorData() - The door placeholder tile at {sTile.Position} has more than one neighboring door placeholder tile! Each door should be made of two placeholder tiles.");
                _ErrorOccurred = true;
                return door;
            }
            else if (neighbors.Count < 1)
            {
                Debug.LogError($"PlaceholderUtility_Doors.GetDoorData() - The door placeholder tile at {sTile.Position} has no neighboring door placeholder tile! Each door should be made of two placeholder tiles.");
                _ErrorOccurred = true;
                return door;
            }
            else if (neighbors[0].Tile.TileType != sTile.Tile.TileType)
            {
                Debug.LogError($"PlaceholderUtility_Doors.GetDoorData() - The door placeholder tile at {sTile.Position} has one neighboring door placeholder tile, but it is not the same door type! Each door should be made of two placeholder tiles of the same type.");
                _ErrorOccurred = true;
                return door;
            }


            // Which tile is the upper-left-most of the two? We want this one always stored in the first position field of the DoorData struct.
            if (sTile.Position.x < neighbors[0].Position.x ||
                sTile.Position.y > neighbors[0].Position.y)
            {
                door.Tile1Position = sTile.Position;
                door.Tile2Position = neighbors[0].Position;
            }
            else
            {
                door.Tile1Position = neighbors[0].Position;
                door.Tile2Position = sTile.Position;
            }


            // Get the direction and level of the door.
            door.DoorDirection = GetDoorDirectionFromFloorLayer(door, floorTiles);


            if (sTile.Tile.TileType == RoomTileTypes.Placeholders_Doors_Basement)
                door.DoorLevel = RoomLevels.Level_Basement;
            else if (sTile.Tile.TileType == RoomTileTypes.Placeholders_Doors_1stFloor)
                door.DoorLevel = RoomLevels.Level_1stFloor;
            else if (sTile.Tile.TileType == RoomTileTypes.Floors_2ndFloor)
                door.DoorLevel = RoomLevels.Level_2ndFloor;


            CheckFloorLevelAtDoor(door, floorTiles);


            return door;
        }

        private static void CheckFloorLevelAtDoor(DoorData door, SavedTileDictionary floorTiles)
        {
            // These two variables are initialized only to keep the compiler from complaining about the use of a possibly unassigned value when we assign values to the two floor tile type variables below.
            SavedTile floorTile1 = new SavedTile();
            SavedTile floorTile2 = new SavedTile();
            if (!(floorTiles.TryGetValue(door.Tile1Position, out floorTile1) &&
                  floorTiles.TryGetValue(door.Tile2Position, out floorTile2)))
            {
                Debug.LogError($"PlaceholderUtility_Doors.CheckFloorLevelAtDoor() - The door placeholder at {door.Tile1Position} has one or more null floor tiles under it");
                _ErrorOccurred = true;
                return;
            }


            RoomTileTypes floorTile1Type = floorTile1.Tile.TileType;
            RoomTileTypes floorTile2Type = floorTile2.Tile.TileType;


            if (!(_FloorTypes_Basement.Contains(floorTile1Type) ||
                   _FloorTypes_1stFloor.Contains(floorTile1Type) ||
                   _FloorTypes_2ndFloor.Contains(floorTile1Type)))
            {
                Debug.LogError($"PlaceholderUtility_Doors.CheckFloorLevelAtDoor() - The door placeholder at {door.Tile1Position} is on floor tiles that are not associated with a particular level!");
                _ErrorOccurred = true;
            }


            RoomLevels floorTile1Level = RoomLevels.Level_1stFloor;
            RoomLevels floorTile2Level = RoomLevels.Level_1stFloor;

            if (_FloorTypes_Basement.Contains(floorTile1Type))
                floorTile1Level = RoomLevels.Level_Basement;
            else if (_FloorTypes_1stFloor.Contains(floorTile1Type))
                floorTile1Level = RoomLevels.Level_1stFloor;
            else if (_FloorTypes_2ndFloor.Contains(floorTile1Type))
                floorTile1Level = RoomLevels.Level_2ndFloor;

            if (_FloorTypes_Basement.Contains(floorTile2Type))
                floorTile2Level = RoomLevels.Level_Basement;
            else if (_FloorTypes_1stFloor.Contains(floorTile2Type))
                floorTile2Level = RoomLevels.Level_1stFloor;
            else if (_FloorTypes_2ndFloor.Contains(floorTile2Type))
                floorTile2Level = RoomLevels.Level_2ndFloor;

            if (floorTile1Level != door.DoorLevel || floorTile2Level != door.DoorLevel)
            {
                Debug.LogError($"PlaceholderUtility_Doors.CheckFloorLevelAtDoor() - The door placeholder at {door.Tile1Position} is on one or more floor tiles that are not associated with the same level as the door!");
                _ErrorOccurred = true;
            }
        }

        private static Directions GetDoorDirectionFromFloorLayer(DoorData door, SavedTileDictionary floorTiles)
        {
            Directions result = Directions.North;


            // Is the door horizontal?
            if (door.Tile2Position == door.Tile1Position + Vector3Int.right)
            {
                bool northResult = CheckFloorTilesAreNull(door.Tile1Position,
                                                          door.Tile1Position + Vector3Int.up,
                                                          door.Tile2Position + Vector3Int.up, floorTiles);
                bool southResult = CheckFloorTilesAreNull(door.Tile1Position,
                                                          door.Tile1Position + Vector3Int.down,
                                                          door.Tile2Position + Vector3Int.down, floorTiles);

                if (!northResult && !southResult)
                {
                    Debug.LogError($"PlaceholderUtility_Doors.GetDoorDirectionFromFloorLayer() - The door placeholder at {door.Tile1Position} has null floor tiles on both the north and south sides! Cannot determine the door's direction.");
                    _ErrorOccurred = true;
                }

                if (northResult)
                    result = Directions.North;
                else
                    result = Directions.South;
            }
            else // The door is vertical
            {
                bool eastResult = CheckFloorTilesAreNull(door.Tile1Position,
                                                         door.Tile1Position + Vector3Int.right,
                                                         door.Tile2Position + Vector3Int.right, floorTiles);
                bool westResult = CheckFloorTilesAreNull(door.Tile1Position,
                                                         door.Tile1Position + Vector3Int.left,
                                                         door.Tile2Position + Vector3Int.left, floorTiles);

                if (!eastResult && !westResult)
                {
                    Debug.LogError($"PlaceholderUtility_Doors.GetDoorDirectionFromFloorLayer() - The door placeholder at {door.Tile1Position} has null floor tiles on both the east and west sides! Cannot determine the door's direction.");
                    _ErrorOccurred = true;
                }

                if (eastResult)
                    result = Directions.East;
                else
                    result = Directions.West;

            }


            return result;
        }


        private static bool CheckFloorTilesAreNull(Vector3Int doorPosition, Vector3Int tile1Position, Vector3Int tile2Position, SavedTileDictionary floorTiles)
        {
            bool result = false;

            SavedTile value;


            bool result1 = floorTiles.TryGetValue(tile1Position, out value);
            bool result2 = floorTiles.TryGetValue(tile2Position, out value);

            if (result1 != result2)
            {
                Debug.LogError($"PlaceholderUtility_Doors.CheckFloorTilesAreNull() - The door placeholder at {doorPosition} has floor tiles on one side with one null and one that is not! Cannot determine the door's direction.");
                _ErrorOccurred = true;
            }


            // Check if both tiles were null.
            if (!(result1 && result2))
                result = true;


            return result;
        }


    }

}