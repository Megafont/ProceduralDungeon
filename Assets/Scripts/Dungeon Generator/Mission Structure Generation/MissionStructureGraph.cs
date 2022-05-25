using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ToolboxLib_Shared.Math;


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;


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



        public List<MissionStructureGraphNode> Nodes { get { return _Nodes; } }



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

        public bool IsTightlyCoupledToAnyNode(MissionStructureGraphNode structureNode)
        {
            foreach (MissionStructureGraphNode node in _Nodes)
            {
                foreach (MissionStructureChildNodeData childNodeData in node.ChildNodesData)
                {
                    if (childNodeData.ChildNode == structureNode && childNodeData.IsTightlyCoupled)
                        return true;

                } // end foreach childNodeData

            } // end foreach node


            return false;
        }


    }

}