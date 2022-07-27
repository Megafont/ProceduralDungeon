using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

using ToolboxLib_Shared.Math;


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;
using MSCNData = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.MissionStructureChildNodeData;


namespace ProceduralDungeon.DungeonGeneration.MissionStructureGeneration
{
    /// <summary>
    /// This class is used to generate a structural graph that defines the mission structure of the dungeon.
    /// It will be used to build out the dungeon with various "quests" for the player to do to advance, such as
    /// finding keys, etc.
    /// </summary>
    public class MissionStructureGraph
    {
        private readonly List<MissionStructureGraphNode> _Nodes;
        public MissionStructureGraphNode StartNode;
        public MissionStructureGraphNode GoalNode;


        public List<MissionStructureGraphNode> Nodes { get { return _Nodes; } }



        public static MissionStructureGraph Generate(NoiseRNG rng)
        {
            MissionStructureGraph graph = new MissionStructureGraph();

            // Create the starting node.
            MissionStructureGraphNode startNode = new MissionStructureGraphNode(GenerativeGrammar.Symbols.NT_Start);
            graph.Nodes.Add(startNode);
            graph.StartNode = startNode;


            GrammarRuleProcessor.GenerateMissionStructureGraph(graph,
                                                               GrammarReplacementRules_Default.GetGrammarReplacementRuleSet(),
                                                               rng);


            return graph;
        }



        public MissionStructureGraph()
        {
            _Nodes = new List<MissionStructureGraphNode>();
        }



        public MissionStructureGraphNode AddNode(MissionStructureGraphNode newNode)
        {
            if (!_Nodes.Contains(newNode))
                _Nodes.Add(newNode);


            // Is the new node the first one to be added to the graph?
            if (_Nodes.Count == 1)
                StartNode = newNode;


            return newNode;
        }

        public MissionStructureGraphNode FindFirstParent(MissionStructureGraphNode structureNode)
        {
            List<MissionStructureGraphNode> parentList = GetPrioritizedParentList(structureNode);

            if (parentList.Count > 0)
                return parentList[0];
            else
                return null;
        }

        /// <summary>
        /// Gets the last parent (closest to the end of the tree) of the specified mission structure node.
        /// </summary>
        /// <param name="structureNode">The node to get the last parent of.</param>
        /// <returns>The last tightly coupled parent of the specified mission structure node if there is one, otherwise it returns the last non-tightly coupled parent node.</returns>
        public MissionStructureGraphNode FindLastParent(MissionStructureGraphNode structureNode)
        {
            List<MissionStructureGraphNode> parentList = GetPrioritizedParentList(structureNode);

            if (parentList.Count < 1)
                return null;


           
            for (int i = parentList.Count - 1; i >= 0; i--)
            {
                if (parentList[i].GetChildNodeData(structureNode).IsTightlyCoupled)
                    return parentList[i];
            }


            return parentList[parentList.Count - 1];
        }

        public List<MissionStructureGraphNode> GetPrioritizedParentList(MissionStructureGraphNode nodeToGetParentsOf)
        {
            List<MissionStructureGraphNode> tightlyCoupledParents = new List<MissionStructureGraphNode>();
            List<MissionStructureGraphNode> nonTightlyCoupledParents = new List<MissionStructureGraphNode>();


            Queue<MissionStructureGraphNode> nodeQueue = new Queue<MissionStructureGraphNode>();
            nodeQueue.Enqueue(StartNode);


            while (nodeQueue.Count > 0)
            {
                MissionStructureGraphNode curStructureNode = nodeQueue.Dequeue();

                if (curStructureNode == null)
                    throw new Exception("MissionStructureGraph.GetPrioritizedParentList() - The current mission structure node is null!");


                // Now iterate through this node's children.
                // We are getting a prioritized child node list here because we want to queue the tightly coupled nodes
                // first since they need to be connected to the parent. That way, we don't waste doors on the parent room
                // by connecting ones that aren't tightly coupled to it.
                foreach (MSCNData childNodeData in curStructureNode.ChildNodesData)
                {
                    MissionStructureGraphNode childNode = childNodeData.ChildNode;


                    if (childNode == nodeToGetParentsOf)
                    {
                        if (childNodeData.IsTightlyCoupled)
                            tightlyCoupledParents.Add(curStructureNode);
                        else
                            nonTightlyCoupledParents.Add(curStructureNode);
                    }

                    
                    // If this child node is not tightly coupled to this node, then we need to check if it is tightly
                    // coupled to any other node. If so, we need to skip it here so it is generated just after the parent
                    // node it is tightly coupled to. This is necessary if there are two branches in the mission structure,
                    // and the child node is tightly coupled to a node in the longer branch that is deeper in the tree
                    // than the current node is. This way we ensure it is always after that parent in the dungeon layout
                    // as it should be.
                    if ((!childNodeData.IsTightlyCoupled) &&    // Check that the child is not tightly coupled to this node
                         IsTightlyCoupledToAnyNode(childNode))  // Check if it is tightly coupled to any other node
                    {
                        continue;
                    }


                    nodeQueue.Enqueue(childNode);

                } // end foreach childNodeData

            } // end while (nodeQueue.Count > 0)



            tightlyCoupledParents.AddRange(nonTightlyCoupledParents);

            return tightlyCoupledParents;
        }

        public bool IsTightlyCoupledToAnyNode(MissionStructureGraphNode structureNode)
        {
            foreach (MissionStructureGraphNode node in _Nodes)
            {
                foreach (MSCNData childNodeData in node.ChildNodesData)
                {
                    if (childNodeData.ChildNode == structureNode && childNodeData.IsTightlyCoupled)
                        return true;

                } // end foreach childNodeData

            } // end foreach node


            return false;
        }

        public int GetLockRoomCount()
        {
            int lockCount = 0;

            foreach (MissionStructureGraphNode node in _Nodes)
            {
                if (node.GrammarSymbol == GrammarSymbols.T_Lock || 
                    node.GrammarSymbol == GrammarSymbols.T_Lock_Multi || 
                    node.GrammarSymbol == GrammarSymbols.T_Lock_Goal)
                {
                    lockCount++;
                }
            }

            return lockCount;
        }


    }

}
