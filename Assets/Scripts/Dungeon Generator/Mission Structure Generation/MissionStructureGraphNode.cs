using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using MSCNData = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.MissionStructureChildNodeData;


namespace ProceduralDungeon.DungeonGeneration.MissionStructureGeneration
{

    public class MissionStructureGraphNode
    {
        public readonly List<MSCNData> ChildNodesData; // The list of this node's childe nodes.
        public DungeonGraphNode DungeonRoomNode; // A reference to the dungeon room node generated from this mission structure node.
        public GenerativeGrammar.Symbols GrammarSymbol; // The dungeon grammar symbol assigned to this room. It defines the type of room within the procedurally generated dungeon design, like a boss room).
        public uint LockCount; // The number of locks up to this point in the dungeon. This property is essentially a built-in Dijkstra map. It allows us to ensure we don't connect parts of the dungeon that would bypass locked doors.
        public Vector3 Position; // This position is used by the MissionStructureGraphGizmos class.


        // This property stores an ID number for this node. This is used for executing grammar replacement rules in the GrammarRuleProcessor.
        // See the .PDF file linked in GenerativeGrammar.cs for details on this process.
        public uint ID { get; private set; }


        public MissionStructureGraphNode(GenerativeGrammar.Symbols symbol)
        {
            ChildNodesData = new List<MSCNData>();

            GrammarSymbol = symbol;

            LockCount = 0;
        }

        public MissionStructureGraphNode(GenerativeGrammar.Symbols symbol, uint id = 0)
            : this(symbol)
        {
            ID = id;
        }

        public bool ContainsChild(MissionStructureGraphNode childNode)
        {
            foreach (MSCNData childNodeData in ChildNodesData)
            {
                if (childNodeData.ChildNode == childNode)
                    return true;
            }

            return false;

        }

        /// <summary>
        /// Checks if the specified node is a child of this node and tightly coupled to it.
        /// </summary>
        /// <param name="childNode">The node to check.</param>
        /// <returns>True if the specified node is a child of this one and tightly coupled to it.</returns>
        public bool ContainsTightlyCoupledChild(MissionStructureGraphNode childNode)
        {
            foreach (MSCNData childData in ChildNodesData)
            {
                if (childData.ChildNode == childNode &&
                    childData.IsTightlyCoupled)
                {
                    return true;
                }

            } // end foreach childData

            return false;
        }

        public MSCNData GetChildNodeData(MissionStructureGraphNode childNode)
        {
            foreach (MSCNData childNodeData in ChildNodesData)
            {
                if (childNodeData.ChildNode == childNode)
                    return childNodeData;
            }

            return null;
        }

        /// <summary>
        /// Returns the child nodes list arranged with tightly coupled nodes all moved ahead of ones that aren't.
        /// </summary>
        /// <returns>The child nodes list arranged with all tightly coupled nodes coming before all nodes that are not.</returns>
        public List<MSCNData> GetPrioritizedChildNodeList()
        {
            List<MSCNData> childList = new List<MSCNData>();


            int lastTightlyCoupledIndex = 0;
            foreach (MSCNData childNodeData in ChildNodesData)
            {
                if (childNodeData.IsTightlyCoupled)
                {
                    childList.Insert(lastTightlyCoupledIndex, childNodeData);
                    lastTightlyCoupledIndex++;
                }
                else if (childNodeData.ChildNode.GetTightlyCoupledChildNodeCount() > 0)
                {
                    childList.Insert(lastTightlyCoupledIndex, childNodeData);
                }
                else
                {
                    childList.Add(childNodeData);
                }

            } // end foreach


            return childList;

        }

        public int GetTightlyCoupledChildNodeCount()
        {
            int count = 0;

            foreach (MSCNData childNodeData in ChildNodesData)
            {
                if (childNodeData.IsTightlyCoupled)
                    count++;
            }

            return count;
        }


    }

}
