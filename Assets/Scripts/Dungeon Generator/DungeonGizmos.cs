using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.TileMaps;
using ProceduralDungeon.DungeonGeneration.Utilities;
using ProceduralDungeon.DungeonGeneration.Utilities.PlaceholderUtilities;


using SavedTileDictionary = System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, ProceduralDungeon.TileMaps.SavedTile>;



namespace ProceduralDungeon.DungeonGeneration
{
    public static class DungeonGizmos
    {
        // This overrides all other constants below.
        public const bool ENABLE_DUNGEON_GIZMOS = true;

        // Enable and disable individual types of dungeon gizmos.    
        public const bool ENABLE_DOOR_GIZMOS = true;



        public static void DrawDungeonGizmos()
        {
            if (ENABLE_DUNGEON_GIZMOS)
            {
                if (ENABLE_DOOR_GIZMOS)
                    DrawDoorGizmos();
            }

        }

        private static void DrawDoorGizmos()
        {
            foreach (DungeonGraphNode node in DungeonGenerator.DungeonGraph.Nodes)
            {
                SavedTileDictionary placeholders_general_map = node.RoomBlueprint.Placeholders_General_Tiles;
                foreach (DoorData doorData in node.RoomBlueprint.DoorsList)
                {
                    // Get and adjust the positions of the door tiles to take into account the position and rotation of the parent room.
                    Vector3Int tile1AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(doorData.Tile1Position, node.RoomPosition, node.RoomDirection);
                    Vector3Int tile2AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(doorData.Tile2Position, node.RoomPosition, node.RoomDirection);

                    // Get the upper-left-most of the two tiles of the door.
                    Vector3Int doorPosition = MiscellaneousUtils.GetUpperLeftMostTile(tile1AdjustedPos, tile2AdjustedPos);


                    // Get the min and max values of X and Y.
                    int minX = doorPosition.x;
                    int minY = doorPosition.y;
                    int maxX = minX + 1;
                    int maxY = minY + 1;


                    //Debug.Log($"Room \"{node.RoomBlueprint.RoomName}\":  Drawing gizmo for door at {Tile1AdjustedPos}...     minX: {minX}   minY: {minY}   maxX: {maxX}   maxY: {maxY}");


                    // Draw door direction indicator
                    // ----------------------------------------------------------------------------------------------------
                    Gizmos.color = Color.yellow;
                    float length = 0.5f;

                    // Adjust the door direction to take into account the parent room's rotation direction.
                    Directions adjustedDoorDirection = MiscellaneousUtils.AddRotationDirectionsTogether(doorData.DoorDirection, node.RoomDirection);

                    //Debug.Log($"DOOR DIR: {doorData.DoorDirection}    ROOM DIR: {node.RoomDirection}    ADJ. DIR: {adjustedDoorDirection}");
                    if (adjustedDoorDirection == Directions.North)
                    {
                        maxX += 1; // These lines use this same if block to prepare for drawing the door placeholder outline in the next section below.
                        Gizmos.DrawLine(new Vector3(minX + 1, maxY, 0), new Vector3(minX + 1, maxY - length, 0));
                    }
                    else if (adjustedDoorDirection == Directions.East)
                    {
                        minY -= 1;
                        Gizmos.DrawLine(new Vector3(maxX, maxY - 1, 0), new Vector3(maxX - length, maxY - 1, 0));
                    }
                    else if (adjustedDoorDirection == Directions.South)
                    {
                        maxX += 1;
                        Gizmos.DrawLine(new Vector3(minX + 1, minY, 0), new Vector3(minX + 1, minY + length, 0));
                    }
                    else if (adjustedDoorDirection == Directions.West)
                    {
                        minY -= 1;
                        Gizmos.DrawLine(new Vector3(minX, minY + 1, 0), new Vector3(minX + length, minY + 1, 0));
                    }


                    // Draw door outline
                    // ----------------------------------------------------------------------------------------------------
                    //Gizmos.color = new Color32(127, 51, 0, 255); // brown
                    Gizmos.color = new Color32(0, 255, 255, 255); // cyan

                    Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(maxX, minY, 0)); // Top
                    Gizmos.DrawLine(new Vector3(minX, maxY, 0), new Vector3(maxX, maxY, 0)); // Bottom
                    Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(minX, maxY, 0)); // Left
                    Gizmos.DrawLine(new Vector3(maxX, minY, 0), new Vector3(maxX, maxY, 0)); // Right


                    /*
                    bool on = true;
                    if (on)
                    {
                        Gizmos.color = Color.red;

                        Vector3Int frontCenterPt = PlaceholderUtils_Doors.CalculateDoorPositionFromConnectedDoor(doorPosition, adjustedDoorDirection);

                        Gizmos.DrawLine(new Vector3Int(frontCenterPt.x, frontCenterPt.y), new Vector3Int(frontCenterPt.x + 1, frontCenterPt.y));
                        Gizmos.DrawLine(new Vector3Int(frontCenterPt.x, frontCenterPt.y - 1), new Vector3Int(frontCenterPt.x + 1, frontCenterPt.y - 1));
                        Gizmos.DrawLine(new Vector3Int(frontCenterPt.x, frontCenterPt.y), new Vector3Int(frontCenterPt.x, frontCenterPt.y - 1));
                        Gizmos.DrawLine(new Vector3Int(frontCenterPt.x + 1, frontCenterPt.y), new Vector3Int(frontCenterPt.x + 1, frontCenterPt.y - 1));
                    }                
                    */
                }

            } // end foreach doorData

        } // end foreach rData


    }

}