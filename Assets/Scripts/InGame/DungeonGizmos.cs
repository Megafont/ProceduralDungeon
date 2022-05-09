using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.TileMaps;
using ProceduralDungeon.DungeonGeneration.Utilities;
using ProceduralDungeon.DungeonGeneration.Utilities.PlaceholderUtilities;


using SavedTileDictionary = System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, ProceduralDungeon.TileMaps.SavedTile>;


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
            Debug.Log("DOORS: " + node.RoomBlueprint.DoorsList.Count);
            foreach (DoorData doorData in node.RoomBlueprint.DoorsList)
            {
                SavedTile doorTile1 = placeholders_general_map[doorData.Tile1Position];
                SavedTile doorTile2 = placeholders_general_map[doorData.Tile2Position];

                // Get and adjust the positions of the door tiles to take into account the position and rotation of the parent room.
                Vector3Int Tile1AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomRotationAndPosition(doorTile1, node.Position, node.Direction);
                Vector3Int Tile2AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomRotationAndPosition(doorTile2, node.Position, node.Direction);


                // Get the min and max values of X and Y.

                int minX = Tile1AdjustedPos.x;
                int minY = Tile1AdjustedPos.y;
                int maxX = minX + 1;
                int maxY = minY + 1;


                //Debug.Log($"Room \"{node.RoomBlueprint.RoomName}\":  Drawing gizmo for door at {Tile1AdjustedPos}...     minX: {minX}   minY: {minY}   maxX: {maxX}   maxY: {maxY}");


                // Draw door direction indicator
                // ----------------------------------------------------------------------------------------------------
                Gizmos.color = Color.yellow;
                float length = 0.5f;
                Directions adjustedDoorDirection = PlaceholderUtils_Doors.AdjustDoorDirectionForRoomRotation(doorData.DoorDirection, node.Direction);
                //Debug.Log($"DOOR DIR: {doorData.DoorDirection}    ROOM DIR: {node.Direction}    ADJ. DIR: {adjustedDoorDirection}");
                if (adjustedDoorDirection == Directions.North)
                {
                    maxX += 1;
                    Gizmos.DrawLine(new Vector3(minX + 1, maxY, 0), new Vector3(minX + 1, maxY - length, 0));
                }
                else if (adjustedDoorDirection == Directions.East)
                {
                    minY -= 1;
                    Gizmos.DrawLine(new Vector3(maxX, maxY - 1, 0), new Vector3(maxX - length, maxY - 1, 0));
                }
                else if (adjustedDoorDirection == Directions.South)
                {
                    minX -= 1;
                    Gizmos.DrawLine(new Vector3(minX + 1, minY, 0), new Vector3(minX + 1, minY + length, 0));
                }
                else if (adjustedDoorDirection == Directions.West)
                {
                    maxY += 1;
                    Gizmos.DrawLine(new Vector3(minX, minY + 1, 0), new Vector3(minX + length, minY + 1, 0));
                }


                // Draw door outline
                // ----------------------------------------------------------------------------------------------------
                Gizmos.color = new Color32(127, 51, 0, 255); // brown
                //Gizmos.color = new Color32(0, 255, 255, 255); // cyan

                Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(maxX, minY, 0)); // Top
                Gizmos.DrawLine(new Vector3(minX, maxY, 0), new Vector3(maxX, maxY, 0)); // Bottom
                Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(minX, maxY, 0)); // Left
                Gizmos.DrawLine(new Vector3(maxX, minY, 0), new Vector3(maxX, maxY, 0)); // Right


            } // end foreach doorData

        } // end foreach rData


    }

}
