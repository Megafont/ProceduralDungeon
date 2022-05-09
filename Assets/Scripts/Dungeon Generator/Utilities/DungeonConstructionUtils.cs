using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration.Utilities;
using ProceduralDungeon.InGame;
using ProceduralDungeon.TileMaps;



namespace ProceduralDungeon.DungeonGeneration.Utilities
{
    public static class DungeonConstructionUtils
    {
        private static Quaternion[] _Rotations = { Quaternion.Euler(0, 0, 0f),
                                                   Quaternion.Euler(0, 0, 90f),
                                                   Quaternion.Euler(0, 0, 180f),
                                                   Quaternion.Euler(0, 0, 270f) };

        public static void PlaceRoom(DungeonTilemapManager manager, DungeonGraphNode node)
        {
            Debug.Log("DRAW FLOOR:");
            CopyTilesIntoDungeonMap(node.RoomBlueprint.FloorTiles,
                                    manager.DungeonMap.FloorsMap,
                                    node.Position,
                                    node.Direction);
            Debug.Log("DRAW PLACEHOLDERS:");
            CopyTilesIntoDungeonMap(node.RoomBlueprint.Placeholders_General_Tiles,
                                    manager.DungeonMap.Placeholders_General_Map,
                                    node.Position,
                                    node.Direction);

            Debug.Log("DRAW WALLS:");
            CopyTilesIntoDungeonMap(node.RoomBlueprint.WallTiles,
                                    manager.DungeonMap.WallsMap,
                                    node.Position,
                                    node.Direction);
        }



        private static void CopyTilesIntoDungeonMap(Dictionary<Vector3Int, SavedTile> src, Tilemap dst, Vector3Int roomPos, Directions roomDirection)
        {
            Vector3Int pos = Vector3Int.zero;
            Quaternion rot = new Quaternion();

            Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);


            foreach (KeyValuePair<Vector3Int, SavedTile> pair in src)
            {
                SavedTile sTile = pair.Value;


                if (sTile.Position.x < min.x)
                    min = new Vector3Int(sTile.Position.x, min.y, 0);
                if (sTile.Position.y < min.y)
                    min = new Vector3Int(min.x, sTile.Position.y, 0);
                if (sTile.Position.x > max.x)
                    max = new Vector3Int(sTile.Position.x, max.y, 0);
                if (sTile.Position.y > max.y)
                    max = new Vector3Int(max.x, sTile.Position.y, 0);


                pos = AdjustTileCoordsForRoomRotationAndPosition(sTile.Position, roomPos, roomDirection);


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


                    //rot = sTile.Rotation * _Rotations[(int)direction];
                    rot = Quaternion.Euler(sTile.Rotation.eulerAngles.x,
                                           sTile.Rotation.eulerAngles.y,
                                           z); //Mathf.Round(sTile.Rotation.eulerAngles.z + _Rotations[(int)direction].eulerAngles.z));

                    Debug.Log($"Tile Pos: {sTile.Position}        New Pos: {pos}        Tile Rotation: {sTile.Rotation.eulerAngles}        Room Rotation: {_Rotations[(int)roomDirection].eulerAngles}        Sum: {rot.eulerAngles})");

                }

                //pos = sTile.Position;
                dst.SetTile(pos, sTile.Tile);


                Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, // We set the position parameter to Vector3.zero, as we don't want to add any offset to the tile's position.
                                                 rot,
                                                 Vector3.one);

                dst.SetTransformMatrix(pos, matrix);


            } // end foreach


            Debug.Log($"MinXYZ: {min}    MaxXYZ: {max}");


        }

        /// <summary>
        /// Adjusts a tile's position to take into account the position and rotation direction of its parent room.
        /// </summary>
        /// <param name="tilePos">The position of the tile within its parent room.</param>
        /// <param name="roomPos">The position of the tile's parent room.</param>
        /// <param name="roomDirection">The rotation direction of the tile's parent room.</param>
        /// <returns>The adjust coordinates.</returns>
        public static Vector3Int AdjustTileCoordsForRoomRotationAndPosition(Vector3Int tilePos, Vector3Int roomPos, Directions roomDirection)
        {
            Vector3Int pos = Vector3Int.zero;


            if (roomDirection == Directions.North)
                pos = tilePos;
            else if (roomDirection == Directions.East)
                pos = new Vector3Int(tilePos.y, -tilePos.x, 0);
            else if (roomDirection == Directions.South)
                pos = new Vector3Int(-tilePos.x, -tilePos.y, 0);
            else if (roomDirection == Directions.West)
                pos = new Vector3Int(-tilePos.y, tilePos.x, 0);


            pos += roomPos;

            return pos;

        }


    }

}