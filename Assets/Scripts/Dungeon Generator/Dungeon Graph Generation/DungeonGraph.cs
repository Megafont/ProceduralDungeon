using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction.PlaceholderUtilities;


namespace ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration
{

    public class DungeonGraph
    {
        List<DungeonGraphNode> _Nodes;
        DungeonGraphNode _StartRoomNode;
        DungeonGraphNode _GoalRoomNode;



        public DungeonGraphNode GoalRoomNode { get { return _GoalRoomNode; } }
        public List<DungeonGraphNode> Nodes { get { return _Nodes; } }
        public DungeonGraphNode StartRoomNode { get { return _StartRoomNode; } }



        public DungeonGraph(DungeonGraphNode startNode)
        {
            _Nodes = new List<DungeonGraphNode>();

            _Nodes.Add(startNode);
            _StartRoomNode = startNode;
        }



        public void ClearAllNodes()
        {
            _Nodes.Clear();
        }

        public DungeonGraphNode AddNode(DungeonGraphNode newNode)
        {
            Assert.IsNotNull(newNode, "DungeonGraph.AddNode() - The new node passed in is null!");


            if (!_Nodes.Contains(newNode))
                _Nodes.Add(newNode);


            return newNode;
        }


    }

}
