using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

using ToolboxLib_Shared.Math;

using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction.PlaceholderUtilities;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.TileMaps;
using ProceduralDungeon.TileMaps.TileTypes;



namespace ProceduralDungeon.DungeonGeneration.DungeonConstruction
{
    public static class DungeonConstructionUtils
    {
        private static bool _RecordMinAndMax;
        private static float _MinX, _MinY;
        private static float _MaxX, _MaxY;

        private static string _CurrentRoomSet;

        private static Dictionary<string, BasicDungeonTile> _WallTopCornerTiles_3Way = new Dictionary<string, BasicDungeonTile>();
        private static Dictionary<string, BasicDungeonTile> _WallTopCornerTiles_4Way = new Dictionary<string, BasicDungeonTile>();

        private static Dictionary<string, List<BasicDungeonTile>> _DoorwayReplacement_Wall_Tiles = new Dictionary<string, List<BasicDungeonTile>>();
        private static Dictionary<string, List<BasicDungeonTile>> _DoorwayReplacement_WallTop_Tiles = new Dictionary<string, List<BasicDungeonTile>>();

        private static BasicDungeonTile _Door_Bombable_Wall_Closed_Left;
        private static BasicDungeonTile _Door_Bombable_Wall_Closed_Right;
        private static BasicDungeonTile _Door_Bombable_Wall_Open_Left;
        private static BasicDungeonTile _Door_Bombable_Wall_Open_Right;
        private static BasicDungeonTile _Door_Bombable_WallTop_Left;
        private static BasicDungeonTile _Door_Bombable_WallTop_Right;



        public static Directions CalculateRoomRotationFromDoorRotation(Directions currentDoorDirection, Directions targetDoorDirection)
        {
            int result = (int)targetDoorDirection - (int)currentDoorDirection;
            int west = (int)Directions.West;

            if (result < 0)
                result += west + 1;

            if (result > west)
                result -= west;


            //Debug.Log($"CurDir {currentDoorDirection}    TarDir {targetDoorDirection}    Result {(Directions)result}");
            return (Directions)result;
        }

        /// <summary>
        /// Creates a new room using the specified RoomData (blueprint) and connects it to the specified door on a previous room.
        /// </summary>
        /// <param name="parentRoom">The room to connect the new room to.</param>
        /// <param name="parentRoomDoor">The door on this room to connect the new room to.</param>
        /// <param name="room2Data">A RoomData object containing the blueprint of the new room.</param>
        /// <param name="room2Door">The door on the new room to connect to the specified door on the previous room.</param>
        /// <param name="room2MissionStructureNode">The MissionStructureNode that room 2 is being generated from.
        /// <returns>A DungeonGraphNode object for the newly generated room.</returns>
        public static DungeonGraphNode CreateNewRoomConnectedToPrevious(DungeonGraphNode parentRoom, uint parentRoomDoorIndex, RoomData room2Data, uint room2DoorIndex, MissionStructureGraphNode room2MissionStructureNode = null)
        {
            DungeonDoor parentRoomDoor = parentRoom.Doorways[(int)parentRoomDoorIndex];
            //parentRoomDoor.ThisRoom_Node = parentRoom;
            //parentRoomDoor.ThisRoom_DoorIndex = parentRoomDoorIndex;


            // Get the upper-left-most of the two adjusted tile positions.
            Vector3Int parentRoomDoor_UpperLeftTileWorldPos = MiscellaneousUtils.GetUpperLeftMostTile(parentRoomDoor.ThisRoom_DoorTile1WorldPosition, parentRoomDoor.ThisRoom_DoorTile2WorldPosition);

            // Get the direction the new room's door needs to face to be able to connect to the specified door on the first room.
            Directions room2Door_TargetDirection = parentRoomDoor.ThisRoom_DoorAdjustedDirection.FlipDirection();



            // Get the door data from the new room's blueprint.
            DoorData room2Door = room2Data.DoorsList[(int)room2DoorIndex];

            // Figure out the rotation of the new room based on the direction the door being connected needs to face to connect properly.
            Directions room2Direction = CalculateRoomRotationFromDoorRotation(room2Door.DoorDirection, room2Door_TargetDirection);



            // Get the coordinates of both tiles of the new room's door and adjust them to take into account the room's rotation direction.
            Vector3Int room2Door_Tile1AdjustedLocalPos = AdjustTileCoordsForRoomPositionAndRotation(room2Door.Tile1Position, Vector3Int.zero, room2Direction); // We use Vector3Int.zero here since we just want to adjust the door position with no translation since we don't know the second room's position yet.
            Vector3Int room2Door_Tile2AdjustedLocalPos = AdjustTileCoordsForRoomPositionAndRotation(room2Door.Tile2Position, Vector3Int.zero, room2Direction);

            // Get the upper-left-most of the two adjusted tile positions.
            Vector3Int room2Door_UpperLeftTileAdjustedLocalPos = MiscellaneousUtils.GetUpperLeftMostTile(room2Door_Tile1AdjustedLocalPos, room2Door_Tile2AdjustedLocalPos);



            // Calculate the position of the new room's door based on the position of the door it is connecting to.
            Vector3Int room2Door_WorldPos = PlaceholderUtils_Doors.CalculateDoorPositionFromConnectedDoor(parentRoomDoor_UpperLeftTileWorldPos, parentRoomDoor.ThisRoom_DoorAdjustedDirection);

            // Calculate the position of the new room based on the 1st room's door.
            Vector3Int room2Pos = room2Door_WorldPos + -room2Door_UpperLeftTileAdjustedLocalPos;

            //Debug.Log($"Room1: \"{parentRoom.RoomBlueprint.RoomName}\"    Room1 Pos: {parentRoom.RoomPosition}     Room1 Dir: {parentRoom.RoomDirection}    Room1 Door Pos: {room1Door_AdjustedLocalPos}    Room1 Door Dir: {parentRoomDoor.DoorDirection} to {room1Door_AdjustedDirection}");
            //Debug.Log($"Room2: \"{room2Data.RoomName}\"    Room2 Pos: {room2Pos}    Room2 Direction: {room2Direction}    Room2 Door Pos: {room2Door_WorldPos}  Room2 Door Dir: {room2Door.DoorDirection} to {room2Door_TargetDirection}");

            // Create a DungeonGraphNode for the new room and add it to the dungeon graph.
            DungeonGraphNode newNode = new DungeonGraphNode(parentRoom,
                                                            room2Data,
                                                            room2Pos,
                                                            room2Direction,
                                                            room2MissionStructureNode);


            // Return the new node to the calling code.
            return newNode;

        }

        /// <summary>
        /// Checks if the current room will collide with an already constructed room in the dungeon map.
        /// </summary>
        /// <param name="roomNode">The DungeonGraphNode of the room in question.</param>
        /// <param name="roomFromTileDict">The dungeon generator's tracking dictionary that associates tiles with the rooms they belong to.</param>
        /// <returns>True if a collision was found or false otherwise.</returns>
        public static bool RoomCollidesWithExistingRoom(DungeonGraphNode roomNode, Dictionary<Vector3Int, DungeonGraphNode> roomFromTileDict)
        {
            bool result1;
            bool result2;


            result1 = CheckForTileCollisions(roomNode.RoomBlueprint.FloorTiles,
                                             roomNode,
                                             roomFromTileDict);

            result2 = CheckForTileCollisions(roomNode.RoomBlueprint.WallTiles,
                                             roomNode,
                                             roomFromTileDict);


            // If any tile collisions were found, return true.
            return (result1 || result2);

        }

        /// <summary>
        /// Copies tiles into the dungeon map.
        /// </summary>
        /// <param name="src">The source data to copy tiles from.</param>
        /// <param name="roomNode">The room node for the room we are constructing.</param>
        /// <param name="roomFromTileDict">The dungeon generator's dictionary for tracking which tiles belong to which rooms.</param>
        /// <returns>True if a collision is detected or false otherwise.</returns>
        private static bool CheckForTileCollisions(Dictionary<Vector3Int, SavedTile> src, DungeonGraphNode roomNode, Dictionary<Vector3Int, DungeonGraphNode> roomFromTileDict)
        {
            Vector3Int pos = Vector3Int.zero;


            Directions roomDirection = roomNode.RoomFinalDirection;
            Vector3Int roomPosition = roomNode.RoomPosition;

            foreach (KeyValuePair<Vector3Int, SavedTile> pair in src)
            {
                SavedTile sTile = pair.Value;


                pos = AdjustTileCoordsForRoomPositionAndRotation(sTile.Position, roomPosition, roomDirection);


                //pos = sTile.Position;
                if (roomFromTileDict.ContainsKey(pos))
                {
                    return true;
                }

            } // end foreach


            return false;
        }

        /// <summary>
        /// Places all tiles of the current room into the dungeon map to construct the room.
        /// </summary>
        /// <param name="tilemapManager">The dungeon tile map manager.</param>
        /// <param name="roomNode">The DungeonGraphNode of the room in question.</param>
        /// <param name="roomFromTileDict">The dungeon generator's tracking dictionary that associates tiles with the rooms they belong to.</param>
        public static void PlaceRoomTiles(DungeonTilemapManager tilemapManager, DungeonGraphNode roomNode, Dictionary<Vector3Int, DungeonGraphNode> roomFromTileDict)
        {
            _CurrentRoomSet = Enum.GetName(typeof(RoomSets), tilemapManager.RoomSet);

            _MinX = _MinY = float.MaxValue;
            _MaxX = _MaxY = float.MinValue;

            _RecordMinAndMax = true;

            //Debug.Log("DRAW FLOOR:");
            CopyTilesIntoDungeonMap(roomNode.RoomBlueprint.FloorTiles,
                                    tilemapManager.DungeonMap.FloorsMap,
                                    roomNode,
                                    roomFromTileDict);

            //Debug.Log("DRAW WALLS:");
            CopyTilesIntoDungeonMap(roomNode.RoomBlueprint.WallTiles,
                                    tilemapManager.DungeonMap.WallsMap,
                                    roomNode,
                                    roomFromTileDict);

            _RecordMinAndMax = false;



            // If we are in play mode, then disable the renderers on the placeholder layers.
            // If edit mode, these can be hidden in the editor individually using the eye icon.
            bool state = true;
            if (Application.isPlaying) // Only include the placeholders if the dungeon generator is running in Unity's edit mode.
                state = false;
            tilemapManager.DungeonMap.Placeholders_Objects_Map.GetComponent<TilemapRenderer>().enabled = state;
            tilemapManager.DungeonMap.Placeholders_Items_Map.GetComponent<TilemapRenderer>().enabled = state;
            tilemapManager.DungeonMap.Placeholders_Enemies_Map.GetComponent<TilemapRenderer>().enabled = state;
          


            //Debug.Log("DRAW PLACEHOLDERS!");
            CopyTilesIntoDungeonMap(roomNode.RoomBlueprint.Placeholders_Object_Tiles,
                                    tilemapManager.DungeonMap.Placeholders_Objects_Map,
                                    roomNode,
                                    roomFromTileDict);

            CopyTilesIntoDungeonMap(roomNode.RoomBlueprint.Placeholders_Item_Tiles,
                                    tilemapManager.DungeonMap.Placeholders_Items_Map,
                                    roomNode,
                                    roomFromTileDict);

            CopyTilesIntoDungeonMap(roomNode.RoomBlueprint.Placeholders_Enemy_Tiles,
                                    tilemapManager.DungeonMap.Placeholders_Enemies_Map,
                                    roomNode,
                                    roomFromTileDict);

        }

        /// <summary>
        /// Copies tiles into the dungeon map.
        /// </summary>
        /// <param name="src">The source data to copy tiles from.</param>
        /// <param name="dst">The Tilemap to copy the tiles into.</param>
        /// <param name="roomNode">The room node for the room we are constructing.</param>
        /// <param name="roomFromTileDict">The dungeon generator's dictionary for tracking which tiles belong to which rooms.</param>
        private static void CopyTilesIntoDungeonMap(Dictionary<Vector3Int, SavedTile> src, Tilemap dst, DungeonGraphNode roomNode, Dictionary<Vector3Int, DungeonGraphNode> roomFromTileDict)
        {
            Vector3Int pos;
            Quaternion rot;

            Directions roomDirection = roomNode.RoomFinalDirection;
            Vector3Int roomPosition = roomNode.RoomPosition;

            List<Vector3Int> wallTopCornerPositions = new List<Vector3Int>();



            foreach (KeyValuePair<Vector3Int, SavedTile> pair in src)
            {
                SavedTile sTile = pair.Value;

                pos = AdjustTileCoordsForRoomPositionAndRotation(sTile.Position, roomPosition, roomDirection);


                if (sTile.Tile.TileType == DungeonTileTypes.Walls_Top_Corner)
                    wallTopCornerPositions.Add(pos);


                if ((!sTile.Tile.RotateWithRoom) || roomDirection == Directions.North)
                {
                    rot = sTile.Rotation;
                }
                else // Rotation direction is East, South, or West
                {
                    rot = GetNewTileRotation(sTile.Rotation, roomDirection.DirectionToRotation());

                    //Debug.Log($"Tile Pos: {sTile.Position}        New Pos: {pos}        Tile Rotation: {sTile.Rotation.eulerAngles}        Room Rotation: {_Rotations[(int)roomDirection].eulerAngles}        Sum: {rot.eulerAngles})");
                }

                //pos = sTile.Position;
                dst.SetTile(pos, sTile.Tile);


                Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, // We set the position parameter to Vector3.zero, as we don't want to add any offset to the tile's position.
                                                 rot,
                                                 Vector3.one);

                dst.SetTransformMatrix(pos, matrix);


                // Register this tile in the dungeon generator's dictionary to associate it with the room it now belongs to.
                // Wall top tiles are excluded from counting since the wall top tiles of rooms are allowed to overlap because its fine for them to get overwritten.
                if (!roomFromTileDict.ContainsKey(pos) &&
                    (sTile.Tile.TileType != DungeonTileTypes.Walls_Doorway_Top && 
                     sTile.Tile.TileType != DungeonTileTypes.Walls_Top && 
                     sTile.Tile.TileType != DungeonTileTypes.Walls_Top_Corner))
                {
                    roomFromTileDict.Add(pos, roomNode);
                }


                if (_RecordMinAndMax)
                {
                    if (pos.x < _MinX)
                        _MinX = pos.x;
                    else if (pos.x > _MaxX)
                        _MaxX = pos.x;

                    if (pos.y < _MinY)
                        _MinY = pos.y;
                    else if (pos.y > _MaxY)
                        _MaxY = pos.y;
                }

            } // end foreach


            roomNode.RoomCenterPoint = new Vector3((_MaxX + _MinX) / 2 + 0.5f, // We add 0.5f just to offset the coordinate since tile coordinates are always the lower left corner of the tile.
                                                   (_MaxY + _MinY) / 2 + 0.5f);

            if (wallTopCornerPositions.Count > 0)
                FixUpWallTopCornerTiles(dst, wallTopCornerPositions);

            //Debug.Log($"Room: \"{roomNode.RoomBlueprint.RoomName}\"    Center: {roomNode.RoomCenterPoint}");
        }

        private static void FixUpWallTopCornerTiles(Tilemap wallsMap, List<Vector3Int> wallTopCornerPositions)
        {
            BasicDungeonTile north, south, east, west;

            foreach (Vector3Int pos in wallTopCornerPositions)
            {
                north = (BasicDungeonTile) wallsMap.GetTile(pos + Vector3Int.up);
                east = (BasicDungeonTile)wallsMap.GetTile(pos + Vector3Int.right);
                south = (BasicDungeonTile)wallsMap.GetTile(pos + Vector3Int.down);
                west = (BasicDungeonTile)wallsMap.GetTile(pos + Vector3Int.left);


                int count = 0;
                if (north != null && north.TileType == DungeonTileTypes.Walls_Top)
                    count++;
                if (east != null && east.TileType == DungeonTileTypes.Walls_Top)
                    count++;
                if (south != null && south.TileType == DungeonTileTypes.Walls_Top)
                    count++;
                if (west != null && west.TileType == DungeonTileTypes.Walls_Top)
                    count++;


                if (count == 4)
                {
                    wallsMap.SetTile(pos, GetWallTopCorner4WayTile());
                }


                if (count == 3)
                {
                    wallsMap.SetTile(pos, GetWallTopCorner3WayTile());
                    
                    Directions rotationDirection = Directions.North; // North means no rotation.
                    if (west == null || west.TileType != DungeonTileTypes.Walls_Top)
                        rotationDirection = Directions.North;
                    else if (north == null || north.TileType != DungeonTileTypes.Walls_Top)
                        rotationDirection = Directions.West;
                    else if (east == null || east.TileType != DungeonTileTypes.Walls_Top)
                        rotationDirection = Directions.South;
                    else if (south == null || south.TileType != DungeonTileTypes.Walls_Top)
                        rotationDirection = Directions.East;

                    Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, // We set the position parameter to Vector3.zero, as we don't want to add any offset to the tile's position.
                                                     rotationDirection.DirectionToRotation(),
                                                     Vector3.one);

                    wallsMap.SetTransformMatrix(pos, matrix);

                }

            } // end foreach pos

        }

        /// <summary>
        /// Adjusts a tile's position to take into account the position and rotation direction of its parent room.
        /// </summary>
        /// <param name="tilePos">The position of the tile within its parent room.</param>
        /// <param name="roomPos">The position of the tile's parent room.</param>
        /// <param name="roomDirection">The rotation direction of the tile's parent room.</param>
        /// <returns>The adjust coordinates.</returns>
        public static Vector3Int AdjustTileCoordsForRoomPositionAndRotation(Vector3Int tilePos, Vector3Int roomPos, Directions roomDirection)
        {
            Vector3Int pos = Vector3Int.zero;


            if (roomDirection == Directions.North)
                pos = tilePos;
            else if (roomDirection == Directions.East)
                pos = new Vector3Int(tilePos.y, -tilePos.x);
            else if (roomDirection == Directions.South)
                pos = new Vector3Int(-tilePos.x, -tilePos.y);
            else if (roomDirection == Directions.West)
                pos = new Vector3Int(-tilePos.y, tilePos.x);


            pos += roomPos;

            return pos;

        }

        public static Quaternion GetNewTileRotation(Quaternion tileRotation, Quaternion rotationAmount)
        {
            float z = 0;
            if (tileRotation.eulerAngles.y == 180f) // Check if the tile has been rotated 180 degrees on the Y-axis (which mirrors the tile from its normal appearance since you're looking at the opposite side).
                z = Mathf.Round(tileRotation.eulerAngles.z + rotationAmount.eulerAngles.z);
            else
                z = Mathf.Round(tileRotation.eulerAngles.z - rotationAmount.eulerAngles.z);


            return Quaternion.Euler(tileRotation.eulerAngles.x,
                                    tileRotation.eulerAngles.y,
                                    z);
        }

        /// <summary>
        /// This function registers a fake, 2-wide strip of tiles outside the specified door. These fake tiles are registered as belonging to the specified room via the room from tile dictionary.
        /// This is used as a simple way to prevent another room from generating outside the dungeon's entrance or exit doors by tricking it into thinking it had a collision with an existing room.
        /// </summary>
        /// <param name="roomNode">The parent room of the door in question.</param>
        /// <param name="door">The door to register tiles outside of.</param>
        /// <param name="roomFromTileDict">The dungeon generator's room from tile dictionary.</param>
        /// <param name="fakeRoomTiles">A list that is used to keep track of the fake tiles generated by this function.</param>
        /// <param name="distanceToAddTilesFromDoor">The number of tiles long the strip is. This defaults to a strip extending 10 tiles from the outside of the door.</param>
        public static void RegisterFakeTilesOutsideDoor(DungeonGraphNode roomNode, DungeonDoor door, Dictionary<Vector3Int, DungeonGraphNode> roomFromTileDict, List<Vector3Int> fakeRoomTiles, int distanceToAddTilesFromDoor = 10)
        {
            //DoorData door = roomNode.RoomBlueprint.DoorsList[(int)entranceDoor.ThisRoom_DoorIndex];

            // Get the direction of the door on room 1 and adjust it to take into account that room's rotation direction.
            Vector3Int scanVector = door.ThisRoom_DoorAdjustedDirection.DirectionToNormalizedVector();


            Vector3Int tile1PlacePos, tile2PlacePos;
            for (int i = 1; i < distanceToAddTilesFromDoor; i++)
            {
                // Get the position for the next tile to scan in front of each of the door's tiles.
                tile1PlacePos = door.ThisRoom_DoorTile1WorldPosition + (scanVector * i);
                tile2PlacePos = door.ThisRoom_DoorTile2WorldPosition + (scanVector * i);

                roomFromTileDict.Add(tile1PlacePos, roomNode);
                roomFromTileDict.Add(tile2PlacePos, roomNode);

                fakeRoomTiles.Add(tile1PlacePos);
                fakeRoomTiles.Add(tile2PlacePos);

            } // end for i

        }

        public static void PositionPlayer(DungeonTilemapManager tilemapManager, DungeonGraphNode startRoom, DungeonDoor entranceDoor)
        {
            int index = (int)entranceDoor.ThisRoom_DoorIndex;


            // Get the position of the upper-left-most of the two tiles.
            Vector3Int playerPos = MiscellaneousUtils.GetUpperLeftMostTile(entranceDoor.ThisRoom_DoorTile1WorldPosition,
                                                                           entranceDoor.ThisRoom_DoorTile2WorldPosition);


            // Get the direction of the entrance door.
            Directions doorDirection = entranceDoor.ThisRoom_DoorAdjustedDirection;


            // Move the player character next to the entrance door.
            tilemapManager.PositionPlayerByStartDoor(playerPos, doorDirection);
        }

        public static void SealOffBlockedDoors(DungeonTilemapManager tilemapManager, List<DungeonDoor> blockedDoors, NoiseRNG rng)
        {
            _CurrentRoomSet = Enum.GetName(typeof(RoomSets), tilemapManager.RoomSet);

            List<BasicDungeonTile> doorwayReplacement_Wall_Tiles = GetDoorwayReplacementWallTiles();
            List<BasicDungeonTile> doorwayReplacement_WallTop_Tiles = GetDoorwayReplacementWallTopTiles();

            GetBombableWallTiles();


            foreach (DungeonDoor door in blockedDoors)
            {
                DoorwayTilesPlacementInfo tilesInfo = GetDoorTilesPlacementInfo(door);

                bool isBombableWallDoor = false;
                if (door.OtherRoom_Node != null &&
                    door.OtherRoom_Node.MissionStructureNode.GrammarSymbol == GenerativeGrammar.Symbols.T_Secret_Room)
                {
                    isBombableWallDoor = true;
                }


                for (int i = 0; i < 4; i++)
                {
                    Vector3Int wallPos = tilesInfo.WallStartPos + (tilesInfo.FillDirection * i);
                    Vector3Int wallTopPos = tilesInfo.WallTopStartPos + (tilesInfo.FillDirection * i);
                    Vector3Int floorPos = tilesInfo.FloorStartPos + (tilesInfo.FillDirection * i);

                    // Create a transform matrix for setting the tile's rotation.
                    Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, // We set the position parameter to Vector3.zero, as we don't want to add any offset to the tile's position.
                                                     tilesInfo.AdjustedDoorDirection.DirectionToRotation(),
                                                     Vector3.one);


                    // Replace the doorway wall tiles.
                    BasicDungeonTile tile = doorwayReplacement_Wall_Tiles[rng.RollRandomIntInRange(0, doorwayReplacement_Wall_Tiles.Count - 1)];
                    tilemapManager.DungeonMap.WallsMap.SetTile(wallPos, tile);
                    tilemapManager.DungeonMap.WallsMap.SetTransformMatrix(wallPos, matrix);


                    // Replace the doorway wall top tiles.
                    tile = doorwayReplacement_WallTop_Tiles[rng.RollRandomIntInRange(0, doorwayReplacement_WallTop_Tiles.Count - 1)];
                    tilemapManager.DungeonMap.WallsMap.SetTile(wallTopPos, tile);
                    tilemapManager.DungeonMap.WallsMap.SetTransformMatrix(wallTopPos, matrix);


                    // Remove the protruding bits at the base of the door frame.
                    tilemapManager.DungeonMap.WallsMap.SetTile(floorPos, null);


                    // If this door is a cracked, bombable wall, then replace the two center doorway wall tiles with cracked wall tiles.
                    if (isBombableWallDoor)
                        PlaceBombableWallDoor(tilemapManager, tilesInfo, matrix);


                } // end for i

            } // end foreach door


            blockedDoors.Clear();

        }


        class DoorwayTilesPlacementInfo
        {
            public Directions AdjustedDoorDirection; // Door direction adjusted for placing doorway tiles.
            public Vector3Int FillDirection = Vector3Int.right; // The fill direction for drawing the door tiles (always down or right).
            public Vector3Int FloorStartPos; // Used to remove the bottom protruding bits of the door frame.
            public Vector3Int WallTopStartPos; // Position of top or leftmost of the 4-wide row or column of doorway tiles.
            public Vector3Int WallStartPos; // Position of the top or leftmost of the 4-wide row or column of doorway wall top tiles.
            public Vector3Int UpperLeftMostTile; // Of the two tiles that make up the door, this is the position of the upperleftmost one.
        }
        private static DoorwayTilesPlacementInfo GetDoorTilesPlacementInfo(DungeonDoor door)
        {
            DoorwayTilesPlacementInfo tilesInfo = new DoorwayTilesPlacementInfo();


            // Place a wall tile at the first tile position.
            tilesInfo.UpperLeftMostTile = MiscellaneousUtils.GetUpperLeftMostTile(door.ThisRoom_DoorTile1WorldPosition, door.ThisRoom_DoorTile2WorldPosition);


            if (door.ThisRoom_DoorAdjustedDirection == Directions.North)
            {
                tilesInfo.WallStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.left;
                tilesInfo.WallTopStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.up + Vector3Int.left;
                tilesInfo.FloorStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.down + Vector3Int.left;
                tilesInfo.FillDirection = Vector3Int.right;
            }
            else if (door.ThisRoom_DoorAdjustedDirection == Directions.South)
            {
                tilesInfo.WallStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.left;
                tilesInfo.WallTopStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.down + Vector3Int.left;
                tilesInfo.FloorStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.up + Vector3Int.left;
                tilesInfo.FillDirection = Vector3Int.right;
            }
            else if (door.ThisRoom_DoorAdjustedDirection == Directions.East)
            {
                tilesInfo.WallStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.up;
                tilesInfo.WallTopStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.right + Vector3Int.up;
                tilesInfo.FloorStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.left + Vector3Int.up;
                tilesInfo.FillDirection = Vector3Int.down;
            }
            else if (door.ThisRoom_DoorAdjustedDirection == Directions.West)
            {
                tilesInfo.WallStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.up;
                tilesInfo.WallTopStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.left + Vector3Int.up;
                tilesInfo.FloorStartPos = tilesInfo.UpperLeftMostTile + Vector3Int.right + Vector3Int.up;
                tilesInfo.FillDirection = Vector3Int.down;
            }


            tilesInfo.AdjustedDoorDirection = door.ThisRoom_DoorAdjustedDirection;
            if (tilesInfo.AdjustedDoorDirection == Directions.East ||
                tilesInfo.AdjustedDoorDirection == Directions.West)
            {
                tilesInfo.AdjustedDoorDirection = tilesInfo.AdjustedDoorDirection.FlipDirection();
            }


            return tilesInfo;
        }

        public static void PlaceBombableWallDoor(DungeonTilemapManager tilemapManager, DungeonDoor doorway, bool placeOpenTiles = false)
        {            
            DoorwayTilesPlacementInfo tilesInfo = GetDoorTilesPlacementInfo(doorway);

            // Create a transform matrix for setting the tile's rotation.
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, // We set the position parameter to Vector3.zero, as we don't want to add any offset to the tile's position.
                                             tilesInfo.AdjustedDoorDirection.DirectionToRotation(),
                                             Vector3.one);

            PlaceBombableWallDoor(tilemapManager, tilesInfo, matrix, placeOpenTiles);
        }

        private static void PlaceBombableWallDoor(DungeonTilemapManager tilemapManager, DoorwayTilesPlacementInfo tilesInfo, Matrix4x4 rotationMatrix, bool placeOpenTiles = false)
        {
            BasicDungeonTile wallTile1, wallTile2, wallTopTile1, wallTopTile2;
            BasicDungeonTile tileToPlace;

            wallTopTile1 = _Door_Bombable_WallTop_Left;
            wallTopTile2 = _Door_Bombable_WallTop_Right;

            if (!placeOpenTiles)
            {
                wallTile1 = _Door_Bombable_Wall_Closed_Left;
                wallTile2 = _Door_Bombable_Wall_Closed_Right;
            }
            else
            {
                wallTile1 = _Door_Bombable_Wall_Open_Left;
                wallTile2 = _Door_Bombable_Wall_Open_Right;
            }


            // First, place the two cracked wall tiles.
            tileToPlace = (tilesInfo.AdjustedDoorDirection == Directions.North || tilesInfo.AdjustedDoorDirection == Directions.West) ? wallTile1 : wallTile2;

            tilemapManager.DungeonMap.WallsMap.SetTile(tilesInfo.WallStartPos + tilesInfo.FillDirection, tileToPlace);
            tilemapManager.DungeonMap.WallsMap.SetTransformMatrix(tilesInfo.WallStartPos + tilesInfo.FillDirection, rotationMatrix);


            tileToPlace = (tilesInfo.AdjustedDoorDirection == Directions.North || tilesInfo.AdjustedDoorDirection == Directions.West) ? wallTile2 : wallTile1;

            tilemapManager.DungeonMap.WallsMap.SetTile(tilesInfo.WallStartPos + tilesInfo.FillDirection * 2, tileToPlace);
            tilemapManager.DungeonMap.WallsMap.SetTransformMatrix(tilesInfo.WallStartPos + tilesInfo.FillDirection * 2, rotationMatrix);



            // Next, place the wall top tiles for bombable doors. These have different colliders so the player can walk through the wall.
            // First, place the two cracked wall tiles.
            tileToPlace = (tilesInfo.AdjustedDoorDirection == Directions.North || tilesInfo.AdjustedDoorDirection == Directions.West) ? wallTopTile1 : wallTopTile2;

            tilemapManager.DungeonMap.WallsMap.SetTile(tilesInfo.WallTopStartPos + tilesInfo.FillDirection, tileToPlace);
            tilemapManager.DungeonMap.WallsMap.SetTransformMatrix(tilesInfo.WallTopStartPos + tilesInfo.FillDirection, rotationMatrix);


            tileToPlace = (tilesInfo.AdjustedDoorDirection == Directions.North || tilesInfo.AdjustedDoorDirection == Directions.West) ? wallTopTile2 : wallTopTile1;

            tilemapManager.DungeonMap.WallsMap.SetTile(tilesInfo.WallTopStartPos + tilesInfo.FillDirection * 2, tileToPlace);
            tilemapManager.DungeonMap.WallsMap.SetTransformMatrix(tilesInfo.WallTopStartPos + tilesInfo.FillDirection * 2, rotationMatrix);
        }

        private static BasicDungeonTile GetWallTopCorner3WayTile()
        {
            if (_WallTopCornerTiles_3Way.ContainsKey(_CurrentRoomSet))
                return _WallTopCornerTiles_3Way[_CurrentRoomSet];


            string tilesPath = ScriptableRoomUtilities.GetRoomSetTilesPath(_CurrentRoomSet);
            
            BasicDungeonTile tile = (BasicDungeonTile)Resources.Load(tilesPath + "/WallTop_Corner_3Way");

            _WallTopCornerTiles_3Way[_CurrentRoomSet] = tile;

            return tile;
        }

        private static BasicDungeonTile GetWallTopCorner4WayTile()
        {
            if (_WallTopCornerTiles_4Way.ContainsKey(_CurrentRoomSet))
                return _WallTopCornerTiles_4Way[_CurrentRoomSet];


            string tilesPath = ScriptableRoomUtilities.GetRoomSetTilesPath(_CurrentRoomSet);

            BasicDungeonTile tile = (BasicDungeonTile)Resources.Load(tilesPath + "/WallTop_Corner_4Way");

            _WallTopCornerTiles_4Way[_CurrentRoomSet] = tile;

            return tile;
        }

        private static List<BasicDungeonTile> GetDoorwayReplacementWallTiles()
        {
            if (_DoorwayReplacement_Wall_Tiles.ContainsKey(_CurrentRoomSet))
                return _DoorwayReplacement_Wall_Tiles[_CurrentRoomSet];


            string tilesPath = ScriptableRoomUtilities.GetRoomSetTilesPath(_CurrentRoomSet);

            List<BasicDungeonTile> list = new List<BasicDungeonTile>();
            list.Add((BasicDungeonTile) Resources.Load(tilesPath + "/Wall_Straight_01"));
            list.Add((BasicDungeonTile) Resources.Load(tilesPath + "/Wall_Straight_02"));
            list.Add((BasicDungeonTile) Resources.Load(tilesPath + "/Wall_Straight_03"));

            _DoorwayReplacement_Wall_Tiles[_CurrentRoomSet] = list;

            return list;
        }

        private static List<BasicDungeonTile> GetDoorwayReplacementWallTopTiles()
        {
            if (_DoorwayReplacement_WallTop_Tiles.ContainsKey(_CurrentRoomSet))
                return _DoorwayReplacement_WallTop_Tiles[_CurrentRoomSet];


            string tilesPath = ScriptableRoomUtilities.GetRoomSetTilesPath(_CurrentRoomSet);

            List<BasicDungeonTile> list = new List<BasicDungeonTile>();
            list.Add((BasicDungeonTile) Resources.Load(tilesPath + "/WallTop_Cracks_01"));
            list.Add((BasicDungeonTile) Resources.Load(tilesPath + "/WallTop_Cracks_02"));
            list.Add((BasicDungeonTile) Resources.Load(tilesPath + "/WallTop_Cracks_03"));
            list.Add((BasicDungeonTile) Resources.Load(tilesPath + "/WallTop_Cracks_04"));
            list.Add((BasicDungeonTile) Resources.Load(tilesPath + "/WallTop_Cracks_05"));

            _DoorwayReplacement_WallTop_Tiles[_CurrentRoomSet] = list;

            return list;
        }

        private static void GetBombableWallTiles()
        {
            if (_Door_Bombable_Wall_Closed_Left != null && _Door_Bombable_Wall_Closed_Right != null &&
                _Door_Bombable_Wall_Open_Left != null && _Door_Bombable_Wall_Open_Right != null &&
                _Door_Bombable_WallTop_Left != null && _Door_Bombable_WallTop_Right != null)
            {
                return;
            }


            string tilesPath = ScriptableRoomUtilities.GetRoomSetTilesPath(_CurrentRoomSet);

            _Door_Bombable_Wall_Closed_Left = (BasicDungeonTile)Resources.Load(tilesPath + "/Wall_Bombable_Closed_Left");
            _Door_Bombable_Wall_Closed_Right = (BasicDungeonTile)Resources.Load(tilesPath + "/Wall_Bombable_Closed_Right");

            _Door_Bombable_Wall_Open_Left = (BasicDungeonTile)Resources.Load(tilesPath + "/Wall_Bombable_Open_Left");
            _Door_Bombable_Wall_Open_Right = (BasicDungeonTile)Resources.Load(tilesPath + "/Wall_Bombable_Open_Right");

            _Door_Bombable_WallTop_Left = (BasicDungeonTile)Resources.Load(tilesPath + "/WallTop_Bombable_Left");
            _Door_Bombable_WallTop_Right = (BasicDungeonTile)Resources.Load(tilesPath + "/WallTop_Bombable_Right");

        }

    }

}
