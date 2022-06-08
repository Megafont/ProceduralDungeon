using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.TileMaps;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction.PlaceholderUtilities;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;


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
            if (!DungeonGenerator.IsInitialized || DungeonGenerator.DungeonGraph == null || DungeonGenerator.DungeonGraph.Nodes.Count < 1)
                return;


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
                if (node.RoomBlueprint == null)
                    continue;

                SavedTileDictionary placeholders_object_map = node.RoomBlueprint.Placeholders_Object_Tiles;


                foreach (DungeonDoor door in node.Doorways)
                {
                    // Get the upper-left-most of the two tiles of the door.
                    Vector3Int doorPosition = MiscellaneousUtils.GetUpperLeftMostTile(door.ThisRoom_DoorTile1WorldPosition, door.ThisRoom_DoorTile2WorldPosition);


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

                    //Debug.Log($"DOOR DIR: {doorData.DoorDirection}    ROOM DIR: {node.RoomDirection}    ADJ. DIR: {adjustedDoorDirection}");
                    if (door.ThisRoom_DoorAdjustedDirection == Directions.North)
                    {
                        maxX += 1; // These lines use this same if block to prepare for drawing the door placeholder outline in the next section below.
                        Gizmos.DrawLine(new Vector3(minX + 1, maxY, 0), new Vector3(minX + 1, maxY - length, 0));
                    }
                    else if (door.ThisRoom_DoorAdjustedDirection == Directions.East)
                    {
                        minY -= 1;
                        Gizmos.DrawLine(new Vector3(maxX, maxY - 1, 0), new Vector3(maxX - length, maxY - 1, 0));
                    }
                    else if (door.ThisRoom_DoorAdjustedDirection == Directions.South)
                    {
                        maxX += 1;
                        Gizmos.DrawLine(new Vector3(minX + 1, minY, 0), new Vector3(minX + 1, minY + length, 0));
                    }
                    else if (door.ThisRoom_DoorAdjustedDirection == Directions.West)
                    {
                        minY -= 1;
                        Gizmos.DrawLine(new Vector3(minX, minY + 1, 0), new Vector3(minX + length, minY + 1, 0));
                    }


                    // Draw door outline
                    // ----------------------------------------------------------------------------------------------------
                    Gizmos.color = new Color32(127, 51, 0, 255); // brown
                    //Gizmos.color = new Color32(0, 255, 255, 255); // cyan

                    // Draw a box around the two tiles of the door.
                    Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(maxX, minY, 0)); // Top
                    Gizmos.DrawLine(new Vector3(minX, maxY, 0), new Vector3(maxX, maxY, 0)); // Bottom
                    Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(minX, maxY, 0)); // Left
                    Gizmos.DrawLine(new Vector3(maxX, minY, 0), new Vector3(maxX, maxY, 0)); // Right


                    /* Debug code that draws a red box around the center point of the door.
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