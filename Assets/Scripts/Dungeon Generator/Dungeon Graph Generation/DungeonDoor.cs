using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction.PlaceholderUtilities;


namespace ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration
{
    public enum DungeonDoorFlags
    {
        IsEntranceDoor = 1,
        IsGoalDoor = 2,

        IsClosed,
        IsLocked,

    }


    /// <summary>
    /// This class holds data about a physical door between two rooms in a generated dungeon.
    /// </summary>
    public class DungeonDoor
    {
        public uint ThisRoom_DoorIndex; // The index of the door in the current room.
        public DungeonGraphNode ThisRoom_Node; // A reference to the DungeonGraphNode of the current room.

        public uint OtherRoom_DoorIndex; // The index of the connected door on the connected room.
        public DungeonGraphNode OtherRoom_Node; // A reference to the DungeonGraphNode of the connected room.

        public DungeonDoorFlags Flags; // Holds flags defining characterics of this room connection.

        uint KeyID; // The ID of the key/multi-part key that unlocks this door.



        public static bool ListContainsDoor(List<DungeonDoor> doorsList, DungeonDoor doorToCheck)
        {
            if (doorsList == null || doorsList.Count < 1)
                return false;


            DungeonGraphNode doorToCheckRoom = doorToCheck.ThisRoom_Node;
            DoorData doorToCheckData = doorToCheckRoom.RoomBlueprint.DoorsList[(int)doorToCheck.ThisRoom_DoorIndex];

            // Get the direction of the door on room 1 and adjust it to take into account that room's rotation direction.
            Directions doorToCheck_AdjustedDirection = MiscellaneousUtils.AddRotationDirectionsTogether(doorToCheckData.DoorDirection, doorToCheckRoom.RoomDirection);

            // Get the coordinates of both tiles of the previous room's door and adjust them to take into account the room's rotation direction.
            Vector3Int doorToCheck_Tile1AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(doorToCheckData.Tile1Position, doorToCheckRoom.RoomPosition, doorToCheckRoom.RoomDirection);
            Vector3Int doorToCheck_Tile2AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(doorToCheckData.Tile2Position, doorToCheckRoom.RoomPosition, doorToCheckRoom.RoomDirection);


            foreach (DungeonDoor door in doorsList)
            {
                DungeonGraphNode curDoorRoom = door.ThisRoom_Node;
                DoorData curDoorData = curDoorRoom.RoomBlueprint.DoorsList[(int)door.ThisRoom_DoorIndex];

                // Get the direction of the door on room 1 and adjust it to take into account that room's rotation direction.
                Directions door_AdjustedDirection = MiscellaneousUtils.AddRotationDirectionsTogether(curDoorData.DoorDirection, curDoorRoom.RoomDirection);

                // Get the coordinates of both tiles of the previous room's door and adjust them to take into account the room's rotation direction.
                Vector3Int door_Tile1AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(curDoorData.Tile1Position, curDoorRoom.RoomPosition, curDoorRoom.RoomDirection);
                Vector3Int door_Tile2AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(curDoorData.Tile2Position, curDoorRoom.RoomPosition, curDoorRoom.RoomDirection);

                if (door_Tile1AdjustedPos == doorToCheck_Tile1AdjustedPos &&
                    door_Tile2AdjustedPos == doorToCheck_Tile2AdjustedPos)
                {
                    return true;
                }

            } // end foreach door


            return false;

        }


    }

}
