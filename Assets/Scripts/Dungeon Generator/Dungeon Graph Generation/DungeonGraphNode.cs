using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration
{

    public class DungeonGraphNode
    {
        private uint _DistanceFromStart; // The distance (in rooms) that this room is from the starting room.
        private MissionStructureGraphNode _MissionStructureNode = null; // A reference to the mission structure node this room was generated from (if any).
        private DungeonGraphNode _Parent; // The parent node of this node.
        private List<DungeonDoor> _DoorWays; // Keeps track of what rooms this room is connected to.
        private Vector3 _RoomCenterPoint; // This room's center point (average of min and max tile positions).
        private Vector3Int _RoomPosition; // This room's position offset relative to the origin.
        private Directions _RoomDirection; // Which way the room is rotated. North is no rotation. East is 90 degrees, and so on.

        private RoomData _RoomBlueprint; // A reference to a RoomData object that will be used to construct the actual room in the level.


        public uint DistanceFromStart { get { return _DistanceFromStart; } } // This property acts as a built-in Dijkstra map tracking how many nodes each node is away from the start node.
        public List<DungeonDoor> Doorways { get { return _DoorWays; } }
        public MissionStructureGraphNode MissionStructureNode { get { return _MissionStructureNode; } }
        public DungeonGraphNode Parent { get { return _Parent; } }
        public Vector3 RoomCenterPoint { get { return _RoomCenterPoint; } set { _RoomCenterPoint = value; } }

        public Directions RoomDirection { get { return _RoomDirection; } }
        public Vector3Int RoomPosition { get { return _RoomPosition; } }
        public RoomData RoomBlueprint { get { return _RoomBlueprint; } }



        public DungeonGraphNode(DungeonGraphNode parent, RoomData data, Vector3Int position, Directions direction, MissionStructureGraphNode missionStructureNode = null)
        {
            Assert.IsNotNull(data, "DungeonGraphNode.DungeonGraphNode() - The passed in RoomData object is null!");


            if (parent != null)
            {
                _Parent = parent;

                _DistanceFromStart = parent._DistanceFromStart + 1;
            }
            else
            {
                _DistanceFromStart = 0;
            }


            _DoorWays = new List<DungeonDoor>();
            _MissionStructureNode = missionStructureNode;
            _RoomBlueprint = data;
            _RoomDirection = direction;
            _RoomPosition = position;



            for (int i = 0; i < data.DoorsList.Count; i++)
                Doorways.Add(new DungeonDoor());

        }


        public List<DungeonDoor> GetUnconnectedDoors()
        {
            List<DungeonDoor> doors = new List<DungeonDoor>();

            foreach (DungeonDoor door in _DoorWays)
            {
                if (door.OtherRoom_Node == null)
                    doors.Add(door);
            }

            return doors;
        }

        /// <summary>
        /// Checks if this room has a unconnected doorway available for a non-tightly-coupled room.
        /// </summary>
        /// <returns>True if this room has at least one unused doorway available.</returns>
        public bool HasUnusedDoorway()
        {
            // Count tightly coupled doorway connections so far.
            int tightlyCoupledDoors = 0;
            int unusedDoorsCount = 0;
            foreach (DungeonDoor door in Doorways)
            {
                if (door.OtherRoom_Node == null)
                    unusedDoorsCount++;

                if (door.IsTightlyCoupledRoomConnection)
                    tightlyCoupledDoors++;
            }

            int totalTightlyCoupledChildren = _MissionStructureNode.GetTightlyCoupledChildNodeCount();
            int tightlyCoupledDoorsLeftToPlace = totalTightlyCoupledChildren - tightlyCoupledDoors;

            bool result = unusedDoorsCount - tightlyCoupledDoorsLeftToPlace > 1;
            //Debug.LogError($"\"{RoomBlueprint.RoomName}\"    Door Count: {Doorways.Count}    Unused Door Count: {unusedDoorsCount}    tcDoorCount: {tightlyCoupledDoors}    tcDoorsLeftCount: {tightlyCoupledDoorsLeftToPlace}    tcChildNodes: {totalTightlyCoupledChildren}    Result: {result}");
            return result;
        }


    }

}
