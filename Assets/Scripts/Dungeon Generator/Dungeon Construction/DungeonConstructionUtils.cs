using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction.PlaceholderUtilities;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.TileMaps;



namespace ProceduralDungeon.DungeonGeneration.DungeonConstruction
{
    public static class DungeonConstructionUtils
    {
        private static Quaternion[] _Rotations = { Quaternion.Euler(0, 0, 0f),
                                                   Quaternion.Euler(0, 0, 90f),
                                                   Quaternion.Euler(0, 0, 180f),
                                                   Quaternion.Euler(0, 0, 270f) };



        public static Directions CalculateRoomRotationFromDoorRotation(Directions currentDoorDirection, Directions targetDoorDirection)
        {
            int result = (int)targetDoorDirection - (int)currentDoorDirection;
            int west = (int)Directions.West;

            if (result < 0)
                result = west + result;

            if (result > west)
                result -= west;

            //Debug.Log($"CurDir {currentDoorDirection}    TarDir {targetDoorDirection}    Result {(Directions)result}");
            return (Directions)result;
        }

        public static void PlaceRoomTiles(DungeonTilemapManager manager, DungeonGraphNode node)
        {
            //Debug.Log("DRAW FLOOR:");
            CopyTilesIntoDungeonMap(node.RoomBlueprint.FloorTiles,
                                    manager.DungeonMap.FloorsMap,
                                    node.RoomPosition,
                                    node.RoomDirection);

            //Debug.Log("DRAW WALLS:");
            CopyTilesIntoDungeonMap(node.RoomBlueprint.WallTiles,
                                    manager.DungeonMap.WallsMap,
                                    node.RoomPosition,
                                    node.RoomDirection);


            if (!Application.isPlaying) // Only include the placeholders if the dungeon generator is running in Unity's edit mode.
            {
                //Debug.Log("DRAW PLACEHOLDERS!");
                CopyTilesIntoDungeonMap(node.RoomBlueprint.Placeholders_General_Tiles,
                                        manager.DungeonMap.Placeholders_General_Map,
                                        node.RoomPosition,
                                        node.RoomDirection);

                CopyTilesIntoDungeonMap(node.RoomBlueprint.Placeholders_Item_Tiles,
                                        manager.DungeonMap.Placeholders_Items_Map,
                                        node.RoomPosition,
                                        node.RoomDirection);

                CopyTilesIntoDungeonMap(node.RoomBlueprint.Placeholders_Enemy_Tiles,
                                        manager.DungeonMap.Placeholders_Enemies_Map,
                                        node.RoomPosition,
                                        node.RoomDirection);
            }

        }

        /// <summary>
        /// Creates a new room using the specified RoomData (blueprint) and connects it to the specified door on a previous room.
        /// </summary>
        /// <param name="previousRoom">The room to connect the new room to.</param>
        /// <param name="previousRoomDoor">The door on this room to connect the new room to.</param>
        /// <param name="roomToConnect">A RoomData object containing the blueprint of the new room.</param>
        /// <param name="room2Door">The door on the new room to connect to the specified door on the previous room.</param>
        /// <returns>A DungeonGraphNode object for the newly generated room.</returns>
        public static DungeonGraphNode GenerateNewRoomAndConnectToPrevious(DungeonGraphNode previousRoom, DoorData previousRoomDoor, RoomData roomToConnect, DoorData room2Door)
        {
            // Get the direction of the door on room 1 and adjust it to take into account that room's rotation direction.
            Directions room1Door_AdjustedDirection = MiscellaneousUtils.AddRotationDirectionsTogether(previousRoomDoor.DoorDirection, previousRoom.RoomDirection);

            // Get the coordinates of both tiles of the previous room's door and adjust them to take into account the room's rotation direction.
            Vector3Int room1Door_Tile1AdjustedLocalPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(previousRoomDoor.Tile1Position, previousRoom.RoomPosition, previousRoom.RoomDirection);
            Vector3Int room1Door_Tile2AdjustedLocalPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(previousRoomDoor.Tile2Position, previousRoom.RoomPosition, previousRoom.RoomDirection);

            // Get the upper-left-most of the two adjusted tile positions.
            Vector3Int room1Door_AdjustedLocalPos = MiscellaneousUtils.GetUpperLeftMostTile(room1Door_Tile1AdjustedLocalPos, room1Door_Tile2AdjustedLocalPos);



            // Get the direction the new room's door needs to face to be able to connect to the specified door on the first room.
            Directions room2DoorTargetDirection = MiscellaneousUtils.FlipDirection(room1Door_AdjustedDirection);

            // Figure out the rotation of the new room based on the direction the door being connected needs to face to connect properly.
            Directions room2Direction = DungeonConstructionUtils.CalculateRoomRotationFromDoorRotation(room2Door.DoorDirection, room2DoorTargetDirection);



            // Get the coordinates of both tiles of the new room's door and adjust them to take into account the room's rotation direction.
            Vector3Int room2Door_Tile1AdjustedLocalPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(room2Door.Tile1Position, Vector3Int.zero, room2Direction); // We use Vector3Int.zero here since we just want to adjust the door position with no translation since we don't know the second room's position yet.
            Vector3Int room2Door_Tile2AdjustedLocalPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(room2Door.Tile2Position, Vector3Int.zero, room2Direction);

            // Get the upper-left-most of the two adjusted tile positions.
            Vector3Int room2Door_AdjustedLocalPos = MiscellaneousUtils.GetUpperLeftMostTile(room2Door_Tile1AdjustedLocalPos, room2Door_Tile2AdjustedLocalPos);



            // Calculate the position of the new room's door based on the position of the door it is connecting to.
            Vector3Int room2Door_WorldPos = PlaceholderUtils_Doors.CalculateDoorPositionFromConnectedDoor(room1Door_AdjustedLocalPos + previousRoom.RoomPosition, room1Door_AdjustedDirection);

            // Calculate the position of the new room based on the 1st room's door.
            Vector3Int room2Pos = room2Door_WorldPos + -room2Door_AdjustedLocalPos;



            // Create a DungeonGraphNode for the new room and add it to the dungeon graph.
            //DungeonGraphNode newNode = new DungeonGraphNode(roomToConnect, room2Pos, room2Direction, previousRoom.DistanceFromStart + 1);
            //AddNode(newNode, previousRoom);


            // Return the new node to the calling code.
            return null;

        }

        private static void CopyTilesIntoDungeonMap(Dictionary<Vector3Int, SavedTile> src, Tilemap dst, Vector3Int roomPos, Directions roomDirection)
        {
            Vector3Int pos = Vector3Int.zero;
            Quaternion rot = new Quaternion();


            foreach (KeyValuePair<Vector3Int, SavedTile> pair in src)
            {
                SavedTile sTile = pair.Value;


                pos = AdjustTileCoordsForRoomPositionAndRotation(sTile.Position, roomPos, roomDirection);


                if ((!sTile.Tile.RotateWithRoom) || roomDirection == Directions.North)
                {
                    rot = sTile.Rotation;
                }
                else // Rotation direction is East, South, or West
                {
                    float z = 0;
                    if (sTile.Rotation.eulerAngles.y == 180f) // Check if the tile has been rotated 180 degrees on the Y-axis (which mirrors the tile from its normal appearance since you're looking at the opposite side).
                        z = Mathf.Round(sTile.Rotation.eulerAngles.z + _Rotations[(int)roomDirection].eulerAngles.z);
                    else
                        z = Mathf.Round(sTile.Rotation.eulerAngles.z - _Rotations[(int)roomDirection].eulerAngles.z);


                    rot = Quaternion.Euler(sTile.Rotation.eulerAngles.x,
                                           sTile.Rotation.eulerAngles.y,
                                           z);

                    //Debug.Log($"Tile Pos: {sTile.Position}        New Pos: {pos}        Tile Rotation: {sTile.Rotation.eulerAngles}        Room Rotation: {_Rotations[(int)roomDirection].eulerAngles}        Sum: {rot.eulerAngles})");
                }

                //pos = sTile.Position;
                dst.SetTile(pos, sTile.Tile);


                Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, // We set the position parameter to Vector3.zero, as we don't want to add any offset to the tile's position.
                                                 rot,
                                                 Vector3.one);

                dst.SetTransformMatrix(pos, matrix);


            } // end foreach

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


    }

}