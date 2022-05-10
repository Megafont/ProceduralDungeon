using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

using ProceduralDungeon.DungeonGeneration.Utilities;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.DungeonGeneration
{
    public class DungeonGraphNode
    {
        private uint _DistanceFromStart;
        private uint _DistanceFromGoal;
        private Vector3Int _RoomPosition; // This room's position offset relative to the origin.
        private Directions _RoomDirection; // Which way the room is rotated. North is no rotation. East is 90 degrees, and so on.

        private RoomData _RoomBlueprint;
        private List<DungeonGraphNode> _NeighboringRooms;



        public uint DistanceFromGoal { get { return _DistanceFromGoal; } }
        public uint DistanceFromStart { get { return _DistanceFromStart; } }
        public Directions RoomDirection { get { return _RoomDirection; } }
        public Vector3Int RoomPosition { get { return _RoomPosition; } }
        public RoomData RoomBlueprint { get { return _RoomBlueprint; } }



        public DungeonGraphNode(RoomData roomData, Vector3Int position, Directions direction, uint distanceFromStart)
        {
            Assert.IsNotNull(roomData, "DungeonGraphNode.DungeonGraphNode() - The passed in RoomData object is null!");

            _NeighboringRooms = new List<DungeonGraphNode>();

            _RoomDirection = direction;
            _DistanceFromStart = distanceFromStart;
            _RoomPosition = position;
            _RoomBlueprint = roomData;
        }



        public void AddNeighbor(DungeonGraphNode newNeighbor)
        {
            Assert.IsNotNull(newNeighbor, "DungeonGraphNode.AddNeighbor() - The passed in neighbor node is null!");

            if (_NeighboringRooms.Count == _RoomBlueprint.DoorsList.Count)
                throw new System.Exception("DungeonGraphNode.AddNeighbor() - This node already has its maximum number of possible neighbors!");

            _NeighboringRooms.Add(newNeighbor);
        }


    }

}
