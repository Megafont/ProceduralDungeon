using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;
using MSCNData = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.MissionStructureChildNodeData;


namespace ProceduralDungeon.DungeonGeneration.MissionStructureGeneration
{

    /// <summary>
    /// This class defines the grammar replacement rules that allow the grammar rule processor
    /// to create mission structure graphs for our randomly generated dungeons.
    /// </summary>
    public static class GrammarReplacementRules_Default
    {
        private static List<GrammarReplacementRule> _RuleSet;



        public static List<GrammarReplacementRule> GetGrammarReplacementRuleSet()
        {
            _RuleSet = new List<GrammarReplacementRule>();

            GenerateGrammarRule_Start();
            GenerateGrammarRule_ChainFinal();

            GenerateGrammarRules_ChainToGate();
            GenerateGrammarRules_ChainLinear();
            GenerateGrammarRules_ChainLinearToChainLinear();
            GenerateGrammarRules_ChainParallelToGate();
            GenerateGrammarRules_ForkToKey();
            GenerateGrammarRules_ForkToKeyMulti();
            GenerateGrammarRules_ForkToHooks();
            GenerateGrammarRules_Hooks();

            return _RuleSet;
        }


        private static void GenerateGrammarRule_Start()
        {
            GrammarReplacementRule rule = new GrammarReplacementRule("Start", "GrammarRule_Start");


            MissionStructureGraphNode prevNode;
            MissionStructureGraphNode newNode;


            // Define the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule.LeftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Start, 1));


            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Add a T_Entrance node that will replace the original NT_Start node.
            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Entrance, 1));

            // Add a chain node.
            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));
            prevNode = newNode;

            // Add a gate node.
            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Gate, 3));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));
            prevNode = newNode;

            // Add a mini boss node.
            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Boss_Mini, 4));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;

            // Add a quest item node.
            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_MainDungeonItem, 5));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;

            // Add an item test node.
            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_MainDungeonItem, 6));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;

            // Add a final chain node.
            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain_DungeonConclusion, 7));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;

            // Add the goal node.
            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Goal, 8));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));
            prevNode = newNode;


            // Add the rule to the rule set.
            _RuleSet.Add(rule);

        }


        /// <summary>
        /// Creates a series of chain to gate rules from a min to max length.
        /// It also creates a chain to gate rule that can generate parallel chains.
        /// </summary>
        private static void GenerateGrammarRules_ChainToGate()
        {
            const int MinChainLength = 3;
            const int MaxChainLength = 5;


            GrammarReplacementRule rule;
            MissionStructureGraphNode prevNode;
            MissionStructureGraphNode newNode;


            // Define the left side of the grammar replacement rule for all rules in this category.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            MissionStructureGraph leftSide = new MissionStructureGraph();
            prevNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain, 1));

            newNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Gate, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));


            for (int length = MinChainLength; length <= MaxChainLength; length++)
            {
                // Set the left side of the grammar replacement rule.
                // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                rule = new GrammarReplacementRule("ChainToGate", "GrammarRule_Chain_Length_" + length);
                rule.LeftSide = leftSide;


                // Define the right side of the grammar replacement rule.
                // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain_Linear, 1));

                for (int i = 3; i <= length; i++)
                {
                    newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain_Linear, (uint)i));
                    prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
                    prevNode = newNode;

                } // end for i


                newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain_Linear, 2));
                prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
                prevNode = newNode;


                // Add the rule to the rule set.
                _RuleSet.Add(rule);

            } // end for length


            // Now that the chain to gate rules with min to max lengths are generated, we have one more
            // rule to create in the chain to gate category.
            // Define the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ChainToGate", "GrammarRule_ChainToGate_Parallel");

            prevNode = rule.LeftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain, 1));

            newNode = rule.LeftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Gate, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));


            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain_Parallel, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Gate, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));


            // Add the rule to the rule set.
            _RuleSet.Add(rule);

        }


        private static void GenerateGrammarRules_ChainLinear()
        {
            GrammarReplacementRule rule;
            MissionStructureGraphNode prevNode;
            MissionStructureGraphNode newNode;


            // Define the left side of the grammar replacement rule for all rules in this category.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            MissionStructureGraph leftSide = new MissionStructureGraph();
            prevNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain_Linear, 1));


            // RULE: Chainlinear_Test

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ChainLinear", "GrammarRule_ChainLinear_Test");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_PreviousItem, 1));


            // Add the rule to the rule set.
            _RuleSet.Add(rule);


            // RULE: Chainlinear_TestSecret

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ChainLinear", "GrammarRule_ChainLinear_TestSecret");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_Secret, 1));


            // Add the rule to the rule set.
            _RuleSet.Add(rule);


            // RULE: Chainlinear_Test_Test_Treasure

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ChainLinear", "GrammarRule_ChainLinear_Test_Test_Treasure");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_PreviousItem, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_PreviousItem, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Bonus, 3));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;


            // Add the rule to the rule set.
            _RuleSet.Add(rule);

        }


        /// <summary>
        /// Creates a series of chain to gate rules from a min to max length.
        /// </summary>
        private static void GenerateGrammarRules_ChainLinearToChainLinear()
        {
            GrammarReplacementRule rule;
            MissionStructureGraphNode prevNode;
            MissionStructureGraphNode newNode;


            // Define the left side of the grammar replacement rule for all rules in this category.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            MissionStructureGraph leftSide = new MissionStructureGraph();
            prevNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain_Linear, 1));

            newNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain_Linear, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));



            // RULE: ChainlinearToChainLinear_TightlyCoupled_Key_Lock

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ChainLinearToChainLinear", "GrammarRule_ChainLinearToChainLinear_TightlyCoupled_Key_Lock");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Lock, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;


            // Add the rule to the rule set.
            _RuleSet.Add(rule);


            // RULE: ChainlinearToChainLinear_TightlyCoupled_Key_Lock_ChainLinear

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ChainLinearToChainLinear", "GrammarRule_ChainLinearToChainLinear_TightlyCoupled_Key_Lock_ChainLinear");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Lock, 3));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain_Linear, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;


            // Add the rule to the rule set.
            _RuleSet.Add(rule);

        }


        private static void GenerateGrammarRules_ChainParallelToGate()
        {
            GrammarReplacementRule rule;
            MissionStructureGraphNode prevNode;
            MissionStructureGraphNode newNode;
            MissionStructureGraphNode newNode2;
            MissionStructureGraphNode newNode3;
            MissionStructureGraphNode newNode4;


            // Define the left side of the grammar replacement rule for all rules in this category.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            MissionStructureGraph leftSide = new MissionStructureGraph();
            prevNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain_Parallel, 1));

            newNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Gate, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));


            // RULE: ChainParallelToGate_Fork_MultiKey

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ChainParallelToGate", "GrammarRule_ChainParallelToGate_Fork_MultiKey");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Fork, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key_Multipart, 3));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));

            newNode2 = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key_Multipart, 4));
            prevNode.ChildNodesData.Add(new MSCNData(newNode2));

            newNode3 = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key_Multipart, 5));
            prevNode.ChildNodesData.Add(new MSCNData(newNode3));

            newNode4 = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Lock_Multi, 2));
            newNode.ChildNodesData.Add(new MSCNData(newNode4));
            newNode2.ChildNodesData.Add(new MSCNData(newNode4));
            newNode3.ChildNodesData.Add(new MSCNData(newNode4));


            // Add the rule to the rule set.
            _RuleSet.Add(rule);

        }


        private static void GenerateGrammarRules_ForkToKey()
        {
            GrammarReplacementRule rule;
            MissionStructureGraphNode prevNode;
            MissionStructureGraphNode newNode;


            // Define the left side of the grammar replacement rule for all rules in this category.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            MissionStructureGraph leftSide = new MissionStructureGraph();
            prevNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Fork, 1));

            newNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));


            // RULE: ForkToKey_Test_Key

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ForkToKey", "GrammarRule_ForkToKey_Test_Key");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Fork, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_PreviousItem, 3));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));
            prevNode = newNode;

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;


            // Add the rule to the rule set.
            _RuleSet.Add(rule);


            // RULE: ForkToKey_TestSecret_Key

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ForkToKey", "GrammarRule_ForkToKey_TestSecret_Key");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Fork, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_Secret, 3));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));
            prevNode = newNode;

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;


            // Add the rule to the rule set.
            _RuleSet.Add(rule);

        }


        private static void GenerateGrammarRules_ForkToKeyMulti()
        {
            GrammarReplacementRule rule;
            MissionStructureGraphNode prevNode;
            MissionStructureGraphNode newNode;


            // Define the left side of the grammar replacement rule for all rules in this category.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            MissionStructureGraph leftSide = new MissionStructureGraph();
            prevNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Fork, 1));

            newNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key_Multipart, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));


            // RULE: ForkToKey_Test_KeyMulti

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ForkToKeyMulti", "GrammarRule_ForkToKey_Test_KeyMulti");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Fork, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_PreviousItem, 3));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));
            prevNode = newNode;

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key_Multipart, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;


            // Add the rule to the rule set.
            _RuleSet.Add(rule);


            // RULE: ForkToKey_TestSecret_Key

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ForkToKeyMulti", "GrammarRule_ForkToKey_TestSecret_KeyMulti");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Fork, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_Secret, 3));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));
            prevNode = newNode;

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key_Multipart, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;


            // Add the rule to the rule set.
            _RuleSet.Add(rule);


            // RULE: ForkToKeyAndKeyMulti_Key_Lock_KeyMulti_Hook

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("ForkToKeyMulti", "GrammarRule_ForkToKeyAndKeyMulti_Key_Lock_KeyMulti_Hook");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Fork, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key, 3));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));
            prevNode = newNode;

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Lock, 4));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));
            prevNode = newNode;

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key_Multipart, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Hook, 5));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));


            // Add the rule to the rule set.
            _RuleSet.Add(rule);

        }


        /// <summary>
        /// Creates a series of fork to hooks rules from a min to max length.
        /// </summary>
        private static void GenerateGrammarRules_ForkToHooks()
        {
            const int MinHooks = 2;
            const int MaxHooks = 2;


            GrammarReplacementRule rule;
            MissionStructureGraphNode prevNode;
            MissionStructureGraphNode newNode;


            // Define the left side of the grammar replacement rule for all rules in this category.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            MissionStructureGraph leftSide = new MissionStructureGraph();
            prevNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Fork, 1));


            for (int hookCount = MinHooks; hookCount <= MaxHooks; hookCount++)
            {
                // Set the left side of the grammar replacement rule.
                // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                rule = new GrammarReplacementRule("ForkToHooks", "GrammarRule_ForkToHooks_" + hookCount);
                rule.LeftSide = leftSide;

                //dfhg
                // Define the right side of the grammar replacement rule.
                // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Exploration, 1));

                for (int i = 3; i <= 3 + hookCount - 1; i++)
                {
                    newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Hook, (uint)i));
                    prevNode.ChildNodesData.Add(new MSCNData(newNode, true));

                } // end for i


                // Add the rule to the rule set.
                _RuleSet.Add(rule);

            } // end for length

        }


        private static void GenerateGrammarRules_Hooks()
        {
            GrammarReplacementRule rule;
            MissionStructureGraphNode prevNode;
            MissionStructureGraphNode newNode;


            // Define the left side of the grammar replacement rule for all rules in this category.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            MissionStructureGraph leftSide = new MissionStructureGraph();
            prevNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Hook, 1));


            // RULE: Hook_Test_Exploration

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("Hooks", "GrammarRule_Hook_Test_Exploration");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Exploration, 1));


            // Add the rule to the rule set.
            _RuleSet.Add(rule);


            // RULE: Hook_Test_TreasureBonus

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("Hooks", "GrammarRule_Hook_Test_TreasureBonus");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_PreviousItem, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Bonus, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;


            // Add the rule to the rule set.
            _RuleSet.Add(rule);


            // RULE: Hook_TestSecret_TreasureBonus

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("Hooks", "GrammarRule_Hook_TestSecret_TreasureBonus");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_Secret, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Bonus, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;


            // Add the rule to the rule set.
            _RuleSet.Add(rule);


        }


        private static void GenerateGrammarRule_ChainFinal()
        {
            GrammarReplacementRule rule;
            MissionStructureGraphNode prevNode;
            MissionStructureGraphNode newNode;
            MissionStructureGraphNode node3;
            MissionStructureGraphNode node4;
            MissionStructureGraphNode node6;


            // Define the left side of the grammar replacement rule for all rules in this category.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            MissionStructureGraph leftSide = new MissionStructureGraph();
            prevNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain_DungeonConclusion, 1));

            newNode = leftSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Goal, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));


            // RULE: Chain_Final

            // Set the left side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            rule = new GrammarReplacementRule("Chain_Final", "GrammarRule_ChainFinal");
            rule.LeftSide = leftSide;

            // Define the right side of the grammar replacement rule.
            // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            prevNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Chain, 1));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Hook, 8));
            prevNode.ChildNodesData.Add(new MSCNData(newNode));

            node3 = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Gate, 3));
            prevNode.ChildNodesData.Add(new MSCNData(node3));

            node6 = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Test_PreviousItem, 6));
            prevNode.ChildNodesData.Add(new MSCNData(node6));



            // Node 3 subtree
            // -----------------

            node4 = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Lock_Goal, 4));
            node3.ChildNodesData.Add(new MSCNData(node4, true));
            prevNode = node4;

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Boss_Main, 5));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Goal, 2));
            prevNode.ChildNodesData.Add(new MSCNData(newNode, true));
            prevNode = newNode;


            // Node 6 subtree
            // -----------------
            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.T_Treasure_Key_Goal, 7));
            node6.ChildNodesData.Add(new MSCNData(newNode, true));
            newNode.ChildNodesData.Add(new MSCNData(node4));

            newNode = rule.RightSide.AddNode(new MissionStructureGraphNode(GrammarSymbols.NT_Hook, 9));
            node6.ChildNodesData.Add(new MSCNData(newNode));


            // Add the rule to the rule set.
            _RuleSet.Add(rule);

        }


    }

}
