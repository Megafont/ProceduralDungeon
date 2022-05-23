using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;


namespace ProceduralDungeon.DungeonGeneration.MissionStructureGeneration
{
    public class MissionStructureGraphNode
    {
        public readonly List<MissionStructureGraphNode> ChildNodes; // The list of this node's childe nodes.
        public DungeonGraphNode DungeonRoomNode; // A reference to the dungeon room node generated from this mission structure node.
        public GenerativeGrammar.Symbols GrammarSymbol; // The dungeon grammar symbol assigned to this room. It defines the type of room within the procedurally generated dungeon design, like a boss room).
        public bool IsTightlyCoupled = false; // Indicates whether this node is tightly coupled to its parent. If so, it means it the player won't be able to access it until the clear the previous component of the dungeon.
        public uint LockCount; // The number of locks up to this point in the dungeon. This property is essentially a built-in Dijkstra map. It allows us to ensure we don't connect parts of the dungeon that would bypass locked doors.
        public Vector3 Position; // This position is used by the MissionStructureGraphGizmos class.


        // This property stores an ID number for this node. This is used for executing grammar replacement rules in the GrammarRuleProcessor.
        // See the .PDF file linked in GenerativeGrammar.cs for details on this process.
        public uint ID { get; private set; }


        public MissionStructureGraphNode(GenerativeGrammar.Symbols symbol, bool isTightlyCoupled = false)
        {
            ChildNodes = new List<MissionStructureGraphNode>();

            GrammarSymbol = symbol;

            IsTightlyCoupled = isTightlyCoupled;

            LockCount = 0;
        }

        public MissionStructureGraphNode(GenerativeGrammar.Symbols symbol)
            : this(symbol, false)
        {

        }

        public MissionStructureGraphNode(GenerativeGrammar.Symbols symbol, uint id = 0, bool isTightlyCoupled = false)
            : this(symbol, isTightlyCoupled)
        {
            ID = id;
        }

        /// <summary>
        /// Returns the child nodes list arranged with tightly coupled nodes all moved ahead of ones that aren't.
        /// </summary>
        /// <returns>The child nodes list arranged with all tightly coupled nodes coming before all nodes that are not.</returns>
        public List<MissionStructureGraphNode> GetPrioritizedChildNodeList()
        {
            List<MissionStructureGraphNode> childList = new List<MissionStructureGraphNode>();


            int lastTightlyCoupledIndex = 0;
            foreach (MissionStructureGraphNode childNode in ChildNodes)
            {
                if (childNode.IsTightlyCoupled)
                {
                    childList.Insert(lastTightlyCoupledIndex, childNode);
                    lastTightlyCoupledIndex++;
                }
                else
                {
                    childList.Add(childNode);
                }

            } // end foreach


            return childList;

        }

        public int GetTightlyCoupledChildNodeCount()
        {
            int count = 0;

            foreach (MissionStructureGraphNode childNode in ChildNodes)
            {
                if (childNode.IsTightlyCoupled)
                    count++;
            }

            return count;
        }


    }

}
