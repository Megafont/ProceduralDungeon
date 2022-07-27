using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.InGame.Objects;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration
{

    public class DungeonGraphNode
    {
        public uint DistanceFromStart { get; private set; } // This property acts as a built-in Dijkstra map tracking how many nodes each node is away from the start node.
        public List<DungeonDoor> Doorways { get; private set; } // The distance (in rooms) that this room is from the starting room.
        public List<Object_Button> Buttons { get; private set; }

        public List<GameObject> Enemies { get; private set; }
        public List<Object_Door> PuzzleActivatedDoors { get { return _ClosedPuzzleDoors; } }
        public List<GameObject> IceBlocks { get { return _IceBlocks; } }
        public MissionStructureGraphNode MissionStructureNode { get; private set; }  // A reference to the mission structure node this room was generated from (if any).
        public DungeonGraphNode Parent { get; private set; }
        public Vector3 RoomCenterPoint { get; set; } // This room's center point (average of min and max tile positions).

        public Directions RoomFinalDirection { get; set; } // Which way the room is rotated. North is no rotation. East is 90 degrees, and so on.
        public Vector3Int RoomPosition { get; set; }
        public RoomData RoomBlueprint { get; private set; }


        private List<Object_Door> _ClosedPuzzleDoors; // Keeps track of the closed doors in the room that only open when a puzzle is solved.       
        private List<GameObject> _IceBlocks;



        public DungeonGraphNode(DungeonGraphNode parent, RoomData data, Vector3Int position, Directions direction, MissionStructureGraphNode missionStructureNode = null)
        {
            Assert.IsNotNull(data, "DungeonGraphNode.DungeonGraphNode() - The passed in RoomData object is null!");


            if (parent != null)
            {
                Parent = parent;

                DistanceFromStart = parent.DistanceFromStart + 1;
            }
            else
            {
                DistanceFromStart = 0;
            }


            Enemies = new List<GameObject>();

            Doorways = new List<DungeonDoor>();
            Buttons = new List<Object_Button>();
            _ClosedPuzzleDoors = new List<Object_Door>();
            _IceBlocks = new List<GameObject>();

            MissionStructureNode = missionStructureNode;
            RoomBlueprint = data;
            RoomFinalDirection = direction;
            RoomPosition = position;



            for (int i = 0; i < data.DoorsList.Count; i++)
                Doorways.Add(new DungeonDoor());

        }


        public List<DungeonDoor> GetUnconnectedDoors()
        {
            List<DungeonDoor> doors = new List<DungeonDoor>();

            foreach (DungeonDoor door in Doorways)
            {
                if (door.OtherRoom_Node == null)
                    doors.Add(door);
            }

            return doors;
        }

        /// <summary>
        /// Checks if this room has an unconnected doorway available for a non-tightly-coupled room.
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

            int totalTightlyCoupledChildren = MissionStructureNode.GetTightlyCoupledChildNodeCount();
            int tightlyCoupledDoorsLeftToPlace = totalTightlyCoupledChildren - tightlyCoupledDoors;

            bool result = unusedDoorsCount - tightlyCoupledDoorsLeftToPlace > 0;

            //Debug.LogError($"\"{RoomBlueprint.RoomName}\"    Door Count: {Doorways.Count}    Unused Door Count: {unusedDoorsCount}    tcDoorCount: {tightlyCoupledDoors}    tcDoorsLeftCount: {tightlyCoupledDoorsLeftToPlace}    tcChildNodes: {totalTightlyCoupledChildren}    Result: {result}");

            return result;
        }


    }

}
