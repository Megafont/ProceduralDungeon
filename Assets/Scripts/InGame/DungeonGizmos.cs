using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.Utilities.PlaceholderUtilities;


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
            foreach (DoorData doorData in node.RoomBlueprint.DoorsList)
            {
                // Draw door outline
                // ----------------------------------------------------------------------------------------------------
                Gizmos.color = new Color32(127, 51, 0, 255); // brown

                int minX = doorData.Tile1Position.x;
                int maxX = doorData.Tile2Position.x + 1;
                int minY = doorData.Tile1Position.y + 1;
                int maxY = doorData.Tile2Position.y;

                //Debug.Log($"Room \"{node.RoomBlueprint.RoomName}\":  Drawing gizmo for door at {doorData.Tile1Position}...     minX: {minX}   minY: {minY}   maxX: {maxX}   maxY: {maxY}");

                Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(maxX, minY, 0)); // Top
                Gizmos.DrawLine(new Vector3(minX, maxY, 0), new Vector3(maxX, maxY, 0)); // Bottom
                Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(minX, maxY, 0)); // Left
                Gizmos.DrawLine(new Vector3(maxX, minY, 0), new Vector3(maxX, maxY, 0)); // Right


                // Draw door direction indicator
                // ----------------------------------------------------------------------------------------------------
                Gizmos.color = Color.yellow;
                float length = 0.5f;
                if (doorData.DoorDirection == Directions.North)
                    Gizmos.DrawLine(new Vector3(minX + 1, minY, 0), new Vector3(minX + 1, minY - length, 0));
                else if (doorData.DoorDirection == Directions.South)
                    Gizmos.DrawLine(new Vector3(minX + 1, maxY, 0), new Vector3(minX + 1, maxY + length, 0));
                else if (doorData.DoorDirection == Directions.East)
                    Gizmos.DrawLine(new Vector3(maxX, minY - 1, 0), new Vector3(maxX - length, minY - 1, 0));
                else if (doorData.DoorDirection == Directions.West)
                    Gizmos.DrawLine(new Vector3(minX, minY - 1, 0), new Vector3(minX + length, minY - 1, 0));


            } // end foreach doorData

        } // end foreach rData


    }

}
