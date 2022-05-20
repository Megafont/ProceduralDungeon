
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
    /// This class holds data about a connection (door) between too rooms.
    /// </summary>
    public class DungeonDoor
    {
        public uint ThisRoom_DoorIndex; // The index of the door in the current room.
        public DungeonGraphNode ThisRoom_Node; // A reference to the DungeonGraphNode of the current room.

        public uint OtherRoom_DoorIndex; // The index of the connected door on the connected room.
        public DungeonGraphNode OtherRoom_Node; // A reference to the DungeonGraphNode of the connected room.

        public DungeonDoorFlags Flags; // Holds flags defining characterics of this room connection.

        uint KeyID; // The ID of the key/multi-part key that unlocks this door.
    }

}
