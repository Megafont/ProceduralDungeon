using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.DungeonGeneration
{

    public class DungeonGraph
    {
        List<DungeonGraphNode> _Nodes;
        DungeonGraphNode _StartRoom;
        DungeonGraphNode _GoalRoom;



        public DungeonGraphNode GoalNode { get { return _GoalRoom; } }
        public List<DungeonGraphNode> Nodes { get { return _Nodes; } }
        public DungeonGraphNode StartNode { get { return _StartRoom; } }



        public DungeonGraph(DungeonGraphNode startNode)
        {
            _Nodes = new List<DungeonGraphNode>();

            _Nodes.Add(startNode);
            _StartRoom = startNode;
        }



        public void ClearAllNodes()
        {
            _Nodes.Clear();
        }

        public DungeonGraphNode AddNode(DungeonGraphNode newNode, DungeonGraphNode parent)
        {
            _Nodes.Add(newNode);

            parent.AddNeighbor(newNode);
            newNode.AddNeighbor(parent);

            return newNode;
        }


    }

}
