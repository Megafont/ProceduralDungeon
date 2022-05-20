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
        public Vector3 Position; // This position is used by the MissionStructureGraphGizmos class.
        public bool IsTightlyCoupled = false; // Indicates whether this node is tightly coupled to its parent. If so, it means it the player won't be able to access it until the clear the previous component of the dungeon.


        // This property stores an ID number for this node. This is used for executing grammar replacement rules in the GrammarRuleProcessor.
        // See the .PDF file linked in GenerativeGrammar.cs for details on this process.
        public uint ID { get; private set; }


        public MissionStructureGraphNode(GenerativeGrammar.Symbols symbol, bool isTightlyCoupled = false)
        {
            ChildNodes = new List<MissionStructureGraphNode>();

            GrammarSymbol = symbol;

            IsTightlyCoupled = isTightlyCoupled;

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
