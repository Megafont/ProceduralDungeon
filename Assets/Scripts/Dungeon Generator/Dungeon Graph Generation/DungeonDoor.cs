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

        public bool IsTightlyCoupledRoomConnection; // Whether or not the room this door connects to is tightly coupled to the parent room of this door.
        public uint KeyID; // The ID of the key/multi-part key that unlocks this door.



        private Directions? _ThisRoom_DoorAdjustedDirection;
        private Vector3Int? _ThisRoom_DoorTile1WorldPos;
        private Vector3Int? _ThisRoom_DoorTile2WorldPos;

        private Directions? _OtherRoom_DoorAdjustedDirection;
        private Vector3Int? _OtherRoom_DoorTile1WorldPos;
        private Vector3Int? _OtherRoom_DoorTile2WorldPos;



        public static bool ListContainsDoor(List<DungeonDoor> doorsList, DungeonDoor doorToCheck)
        {
            if (doorsList == null || doorsList.Count < 1)
                return false;


            foreach (DungeonDoor door in doorsList)
            {
                if (door.ThisRoom_DoorTile1WorldPosition == doorToCheck.ThisRoom_DoorTile1WorldPosition &&
                    door.ThisRoom_DoorTile2WorldPosition == doorToCheck.ThisRoom_DoorTile2WorldPosition)
                {
                    return true;
                }

            } // end foreach door


            return false;

        }




        public Directions ThisRoom_DoorAdjustedDirection
        {
            get
            {
                return (Directions)(_ThisRoom_DoorAdjustedDirection != null ? _ThisRoom_DoorAdjustedDirection :
                                                                              _ThisRoom_DoorAdjustedDirection = CalculateAdjustedDoorDirection(ThisRoom_Node, (int)ThisRoom_DoorIndex));
            }
        }

        public Vector3Int ThisRoom_DoorTile1WorldPosition
        {
            get
            {
                return (Vector3Int)(_ThisRoom_DoorTile1WorldPos != null ? _ThisRoom_DoorTile1WorldPos :
                                                                          _ThisRoom_DoorTile1WorldPos = CalculateDoorTileWorldPosition(ThisRoom_Node, (int)ThisRoom_DoorIndex));
            }
        }

        public Vector3Int ThisRoom_DoorTile2WorldPosition
        {
            get
            {
                return (Vector3Int)(_ThisRoom_DoorTile2WorldPos != null ? _ThisRoom_DoorTile2WorldPos :
                                                                          _ThisRoom_DoorTile2WorldPos = CalculateDoorTileWorldPosition(ThisRoom_Node, (int)ThisRoom_DoorIndex, false));
            }
        }



        public Directions OtherRoom_DoorAdjustedDirection
        {
            get
            {
                return (Directions)(_OtherRoom_DoorAdjustedDirection != null ? _OtherRoom_DoorAdjustedDirection :
                                                                               _OtherRoom_DoorAdjustedDirection = CalculateAdjustedDoorDirection(OtherRoom_Node, (int)OtherRoom_DoorIndex));
            }
        }

        public Vector3Int OtherRoom_DoorTile1WorldPosition
        {
            get
            {
                return (Vector3Int)(_OtherRoom_DoorTile1WorldPos != null ? _OtherRoom_DoorTile1WorldPos :
                                                                           _OtherRoom_DoorTile1WorldPos = CalculateDoorTileWorldPosition(OtherRoom_Node, (int)OtherRoom_DoorIndex));
            }
        }

        public Vector3Int OtherRoom_DoorTile2WorldPosition
        {
            get
            {
                return (Vector3Int)(_OtherRoom_DoorTile2WorldPos != null ? _OtherRoom_DoorTile2WorldPos :
                                                                           _OtherRoom_DoorTile2WorldPos = CalculateDoorTileWorldPosition(OtherRoom_Node, (int)OtherRoom_DoorIndex, false));
            }
        }

        public Vector3 CenterPosition
        {
            get
            {
                Vector3 sum = (Vector3)(_ThisRoom_DoorTile1WorldPos + _ThisRoom_DoorTile2WorldPos);
                return (sum / 2) + new Vector3(0.5f, 0.5f);
            }
        }


        private static Directions CalculateAdjustedDoorDirection(DungeonGraphNode parentRoom, int doorIndex)
        {
            DoorData parentRoomDoor = parentRoom.RoomBlueprint.DoorsList[doorIndex];

            Directions result = parentRoomDoor.DoorDirection.AddRotationDirection(parentRoom.RoomFinalDirection);

            return result;
        }

        private static Vector3Int CalculateDoorTileWorldPosition(DungeonGraphNode parentRoom, int doorIndex, bool calculateForTile1 = true)
        {
            DoorData parentRoomDoor = parentRoom.RoomBlueprint.DoorsList[doorIndex];

            if (calculateForTile1)
                return DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(parentRoomDoor.Tile1Position, parentRoom.RoomPosition, parentRoom.RoomFinalDirection);
            else
                return DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(parentRoomDoor.Tile2Position, parentRoom.RoomPosition, parentRoom.RoomFinalDirection);

        }


    }

}
