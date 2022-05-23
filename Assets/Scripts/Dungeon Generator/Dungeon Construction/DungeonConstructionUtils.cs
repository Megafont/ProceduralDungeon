using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

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
        public static DungeonGraphNode CreateNewRoomConnectedToPrevious(DungeonGraphNode parentRoom, DoorData parentRoomDoor, RoomData room2Data, DoorData room2Door, MissionStructureGraphNode room2MissionStructureNode = null)
        {
            // Get the direction of the door on room 1 and adjust it to take into account that room's rotation direction.
            Directions room1Door_AdjustedDirection = MiscellaneousUtils.AddRotationDirectionsTogether(parentRoomDoor.DoorDirection, parentRoom.RoomDirection);

            // Get the coordinates of both tiles of the previous room's door and adjust them to take into account the room's rotation direction.
            Vector3Int room1Door_Tile1AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(parentRoomDoor.Tile1Position, parentRoom.RoomPosition, parentRoom.RoomDirection);
            Vector3Int room1Door_Tile2AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(parentRoomDoor.Tile2Position, parentRoom.RoomPosition, parentRoom.RoomDirection);

            // Get the upper-left-most of the two adjusted tile positions.
            Vector3Int room1Door_AdjustedLocalPos = MiscellaneousUtils.GetUpperLeftMostTile(room1Door_Tile1AdjustedPos, room1Door_Tile2AdjustedPos);



            // Get the direction the new room's door needs to face to be able to connect to the specified door on the first room.
            Directions room2Door_TargetDirection = room1Door_AdjustedDirection.FlipDirection();

            // Figure out the rotation of the new room based on the direction the door being connected needs to face to connect properly.
            Directions room2Direction = DungeonConstructionUtils.CalculateRoomRotationFromDoorRotation(room2Door.DoorDirection, room2Door_TargetDirection);



            // Get the coordinates of both tiles of the new room's door and adjust them to take into account the room's rotation direction.
            Vector3Int room2Door_Tile1AdjustedLocalPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(room2Door.Tile1Position, Vector3Int.zero, room2Direction); // We use Vector3Int.zero here since we just want to adjust the door position with no translation since we don't know the second room's position yet.
            Vector3Int room2Door_Tile2AdjustedLocalPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(room2Door.Tile2Position, Vector3Int.zero, room2Direction);

            // Get the upper-left-most of the two adjusted tile positions.
            Vector3Int room2Door_AdjustedLocalPos = MiscellaneousUtils.GetUpperLeftMostTile(room2Door_Tile1AdjustedLocalPos, room2Door_Tile2AdjustedLocalPos);



            // Calculate the position of the new room's door based on the position of the door it is connecting to.
            Vector3Int room2Door_WorldPos = PlaceholderUtils_Doors.CalculateDoorPositionFromConnectedDoor(room1Door_AdjustedLocalPos, room1Door_AdjustedDirection);

            // Calculate the position of the new room based on the 1st room's door.
            Vector3Int room2Pos = room2Door_WorldPos + -room2Door_AdjustedLocalPos;

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
        /// <param name="tilemapManager">The dungeon tile map manager.</param>
        /// <param name="roomNode">The DungeonGraphNode of the room in question.</param>
        /// <param name="roomFromTileDict">The dungeon generator's tracking dictionary that associates tiles with the rooms they belong to.</param>
        /// <returns>True if a collision was found or false otherwise.</returns>
        public static bool RoomCollidesWithExistingRoom(DungeonTilemapManager tilemapManager, DungeonGraphNode roomNode, Dictionary<Vector3Int, DungeonGraphNode> roomFromTileDict)
        {
            bool result1;
            bool result2;


            result1 = CheckForTileCollisions(roomNode.RoomBlueprint.FloorTiles,
                                             tilemapManager.DungeonMap.FloorsMap,
                                             roomNode,
                                             roomFromTileDict);

            result2 = CheckForTileCollisions(roomNode.RoomBlueprint.WallTiles,
                                             tilemapManager.DungeonMap.WallsMap,
                                             roomNode,
                                             roomFromTileDict);


            // If any tile collisions were found, return true.
            return (result1 || result2);

        }

        /// <summary>
        /// Copies tiles into the dungeon map.
        /// </summary>
        /// <param name="src">The source data to copy tiles from.</param>
        /// <param name="dst">The Tilemap to copy the tiles into.</param>
        /// <param name="roomNode">The room node for the room we are constructing.</param>
        /// <param name="roomFromTileDict">The dungeon generator's dictionary for tracking which tiles belong to which rooms.</param>
        /// <returns>True if a collision is detected or false otherwise.</returns>
        private static bool CheckForTileCollisions(Dictionary<Vector3Int, SavedTile> src, Tilemap dst, DungeonGraphNode roomNode, Dictionary<Vector3Int, DungeonGraphNode> roomFromTileDict)
        {
            Vector3Int pos = Vector3Int.zero;


            Directions roomDirection = roomNode.RoomDirection;
            Vector3Int roomPosition = roomNode.RoomPosition;

            foreach (KeyValuePair<Vector3Int, SavedTile> pair in src)
            {
                SavedTile sTile = pair.Value;


                pos = AdjustTileCoordsForRoomPositionAndRotation(sTile.Position, roomPosition, roomDirection);


                //pos = sTile.Position;
                if (dst.GetTile(pos) != null)
                    return true;

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


            if (!Application.isPlaying) // Only include the placeholders if the dungeon generator is running in Unity's edit mode.
            {
                //Debug.Log("DRAW PLACEHOLDERS!");
                CopyTilesIntoDungeonMap(roomNode.RoomBlueprint.Placeholders_General_Tiles,
                                        tilemapManager.DungeonMap.Placeholders_General_Map,
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
            Vector3Int pos = Vector3Int.zero;
            Quaternion rot = new Quaternion();


            Directions roomDirection = roomNode.RoomDirection;
            Vector3Int roomPosition = roomNode.RoomPosition;

            foreach (KeyValuePair<Vector3Int, SavedTile> pair in src)
            {
                SavedTile sTile = pair.Value;


                pos = AdjustTileCoordsForRoomPositionAndRotation(sTile.Position, roomPosition, roomDirection);


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
                if (!roomFromTileDict.ContainsKey(pos))
                    roomFromTileDict.Add(pos, roomNode);


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

            //Debug.Log($"Room: \"{roomNode.RoomBlueprint.RoomName}\"    Center: {roomNode.RoomCenterPoint}");
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

        public static void PositionPlayer(DungeonTilemapManager tilemapManager, DungeonGraphNode startRoom, DungeonDoor entranceDoor)
        {
            int index = (int)entranceDoor.ThisRoom_DoorIndex;

            // Get the position of both tiles of the entrance door.
            Vector3Int doorTile1Pos = entranceDoor.ThisRoom_Node.RoomBlueprint.DoorsList[index].Tile1Position;
            Vector3Int doorTile2Pos = entranceDoor.ThisRoom_Node.RoomBlueprint.DoorsList[index].Tile2Position;

            // Adjust the tile positions to take into account the position and rotation direction of the room.
            doorTile1Pos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(doorTile1Pos, startRoom.RoomPosition, startRoom.RoomDirection);
            doorTile2Pos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(doorTile2Pos, startRoom.RoomPosition, startRoom.RoomDirection);

            // Get the position of the upper-left-most of the two tiles.
            Vector3Int playerPos = MiscellaneousUtils.GetUpperLeftMostTile(doorTile1Pos, doorTile2Pos);


            // Get the direction of the entrance door.
            Directions doorDirection = entranceDoor.ThisRoom_Node.RoomBlueprint.DoorsList[index].DoorDirection;

            // Adjust the door direction to take into account the room's rotation direction.
            doorDirection = MiscellaneousUtils.AddRotationDirectionsTogether(doorDirection, startRoom.RoomDirection);


            // Move the player character next to the entrance door.
            tilemapManager.PositionPlayerByStartDoor(playerPos, doorDirection);
        }

        public static void SealOffBlockedDoors(DungeonTilemapManager tilemapManager, List<DungeonDoor> blockedDoors)
        {
            foreach (DungeonDoor door in blockedDoors)
            {
                DungeonGraphNode parentRoom = door.ThisRoom_Node;
                DoorData parentRoomDoor = parentRoom.RoomBlueprint.DoorsList[(int)door.ThisRoom_DoorIndex];

                // Get the direction of the door on room 1 and adjust it to take into account that room's rotation direction.
                Directions door_AdjustedDirection = MiscellaneousUtils.AddRotationDirectionsTogether(parentRoomDoor.DoorDirection, parentRoom.RoomDirection);

                // Get the coordinates of both tiles of the previous room's door and adjust them to take into account the room's rotation direction.
                Vector3Int door_Tile1AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(parentRoomDoor.Tile1Position, parentRoom.RoomPosition, parentRoom.RoomDirection);
                Vector3Int door_Tile2AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(parentRoomDoor.Tile2Position, parentRoom.RoomPosition, parentRoom.RoomDirection);



                Tilemap wallsMap = tilemapManager.DungeonMap.WallsMap;

                // Place a wall tile at the first tile position.
                BasicDungeonTile tile1 = (BasicDungeonTile)wallsMap.GetTile(door_Tile1AdjustedPos);

                if (tile1.TileType != DungeonTileTypes.Walls_DoorFrame_Left &&
                    tile1.TileType != DungeonTileTypes.Walls_DoorFrame_Right)
                {
                    Debug.LogError($"DungeonConstructionUtils.SealOffBlockedDoors() - Cannot seal off door[{door.ThisRoom_DoorIndex}] tile 1 in room \"{parentRoom.RoomBlueprint.RoomName}\", because the wall tile at {door_Tile1AdjustedPos} is not a door frame tile!");
                }
                else
                {
                    wallsMap.SetTile(door_Tile1AdjustedPos, (tile1 as DoorwayTile).ReplacementWallTile);

                    // Create a transform matrix for setting the tile rotation.
                    Matrix4x4 transformMatrix1 = Matrix4x4.TRS(Vector3.zero, // We set the position parameter to Vector3.zero, as we don't want to add any offset to the tile's position.
                                                              GetNewTileRotation(Quaternion.identity, door_AdjustedDirection.DirectionToRotation()),
                                                              Vector3.one);

                    wallsMap.SetTransformMatrix(door_Tile1AdjustedPos, transformMatrix1);
                }

                // Place a wall tile at the second tile position.
                BasicDungeonTile tile2 = (BasicDungeonTile)wallsMap.GetTile(door_Tile2AdjustedPos);

                if (tile2.TileType != DungeonTileTypes.Walls_DoorFrame_Left &&
                    tile2.TileType != DungeonTileTypes.Walls_DoorFrame_Right)
                {
                    Debug.LogError($"DungeonConstructionUtils.SealOffBlockedDoors() - Cannot seal off door[{door.ThisRoom_DoorIndex}] tile 2 in room \"{parentRoom.RoomBlueprint.RoomName}\", because the wall tile at {door_Tile2AdjustedPos} is not a door frame tile!");
                }
                else
                {
                    wallsMap.SetTile(door_Tile2AdjustedPos, (tile2 as DoorwayTile).ReplacementWallTile);

                    // Create a transform matrix for setting the tile rotation.
                    Matrix4x4 transformMatrix2 = Matrix4x4.TRS(Vector3.zero, // We set the position parameter to Vector3.zero, as we don't want to add any offset to the tile's position.
                                                              GetNewTileRotation(Quaternion.identity, door_AdjustedDirection.DirectionToRotation()),
                                                              Vector3.one);


                    wallsMap.SetTransformMatrix(door_Tile2AdjustedPos, transformMatrix2);
                }

            } // end foreach door


        }


    }

}