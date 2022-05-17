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
        private DungeonGraphNode _Parent; // The parent node of this node.
        private List<DungeonDoor> _RoomConnections; // Keeps track of what rooms this room is connected to.
        private Vector3Int _RoomPosition; // This room's position offset relative to the origin.
        private Directions _RoomDirection; // Which way the room is rotated. North is no rotation. East is 90 degrees, and so on.

        private RoomData _RoomBlueprint; // A reference to a RoomData object that will be used to construct the actual room in the level.



        public uint DistanceFromStart { get { return _DistanceFromStart; } }
        public DungeonGraphNode Parent { get { return _Parent; } }
        public List<DungeonDoor> Doorways { get { return _RoomConnections; } }
        public Directions RoomDirection { get { return _RoomDirection; } }
        public Vector3Int RoomPosition { get { return _RoomPosition; } }
        public RoomData RoomBlueprint { get { return _RoomBlueprint; } }



        public DungeonGraphNode(DungeonGraphNode parent, RoomData data, Vector3Int position, Directions direction, MissionStructureGraphNode missionNode = null)
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


            _RoomBlueprint = data;
            _RoomConnections = new List<DungeonDoor>();
            _RoomDirection = direction;
            _RoomPosition = position;


            // The initial position is set like this just to space the nodes out initially so the dungeon graph gizmos aren't all drawn on top of each other.
            _RoomPosition = new Vector3Int((int)_DistanceFromStart * 5, 0, 0);
        }



        public void AddNeighbor(DungeonGraphNode newNeighbor)
        {
            throw new NotImplementedException();
        }


    }

}
