using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration;
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
            CopyTilesIntoDungeonMap(node.RoomBlueprint.PlaceholderTiles,
                                    manager.DungeonMap.Placeholders_General_Map,
                                    node.Position,
                                    node.Direction);

            Debug.Log("DRAW WALLS:");
            CopyTilesIntoDungeonMap(node.RoomBlueprint.WallTiles,
                                    manager.DungeonMap.WallsMap,
                                    node.Position,
                                    node.Direction);
        }



        private static void CopyTilesIntoDungeonMap(Dictionary<Vector3Int, SavedTile> src, Tilemap dst, Vector3Int dstOffest, Directions direction)
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


                if (direction == Directions.North)
                    pos = sTile.Position;
                else if (direction == Directions.East)
                    pos = new Vector3Int(sTile.Position.y, -sTile.Position.x, 0);
                else if (direction == Directions.South)
                    pos = new Vector3Int(-sTile.Position.x, -sTile.Position.y, 0);
                else if (direction == Directions.West)
                    pos = new Vector3Int(-sTile.Position.y, sTile.Position.x, 0);


                if ((!sTile.Tile.RotateWithRoom) || direction == Directions.North)
                {
                    rot = sTile.Rotation;
                }
                else // Rotation direction is East, South, or West
                {
                    float z = 0;
                    if (sTile.Rotation.eulerAngles.y == 180f) // Check if the tile has been rotated 180 degrees on the Y-axis (which mirrors the tile from its normal appearance since you're looking at the opposite side).
                        z = Mathf.Round(sTile.Rotation.eulerAngles.z + _Rotations[(int)direction].eulerAngles.z);
                    else
                        z = Mathf.Round(sTile.Rotation.eulerAngles.z - _Rotations[(int)direction].eulerAngles.z);


                    //rot = sTile.Rotation * _Rotations[(int)direction];
                    rot = Quaternion.Euler(sTile.Rotation.eulerAngles.x,
                                           sTile.Rotation.eulerAngles.y,
                                           z); //Mathf.Round(sTile.Rotation.eulerAngles.z + _Rotations[(int)direction].eulerAngles.z));

                    Debug.Log($"Tile Pos: {sTile.Position}        New Pos: {pos}        Tile Rotation: {sTile.Rotation.eulerAngles}        Room Rotation: {_Rotations[(int)direction].eulerAngles}        Sum: {rot.eulerAngles})");

                }

                //pos = sTile.Position;
                dst.SetTile(pos + dstOffest, sTile.Tile);


                Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, // We set the position parameter to Vector3.zero, as we don't want to add any offset to the tile's position.
                                                 rot,
                                                 Vector3.one);

                dst.SetTransformMatrix(pos, matrix);


            } // end foreach


            Debug.Log($"MinXYZ: {min}    MaxXYZ: {max}");


        }



    }

}