using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Assertions;

using ToolboxLib_Shared.Math;

using ProceduralDungeon.DungeonGeneration;


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;
using MSCNData = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.MissionStructureChildNodeData;


namespace ProceduralDungeon.DungeonGeneration.MissionStructureGeneration
{
    /// <summary>
    /// This class processes the generative grammar replacement rules defined in GrammarReplacementRules.cs
    /// to produce mission structure graphs for our randomly generated dungeons.
    /// </summary>
    public static class GrammarRuleProcessor
    {
        private static Dictionary<string, List<GrammarReplacementRule>> _GrammarReplacementRuleSet;
        private static MissionStructureGraph _MissionStructureGraph;
        private static NoiseRNG _RNG_MissionStructureGen;



        public static MissionStructureGraph GenerateMissionStructureGraph(MissionStructureGraph graph, List<GrammarReplacementRule> ruleSet, NoiseRNG rng)
        {
            Assert.IsNotNull(ruleSet, "The passed in list of generative grammar replacement rules is null!");
            Assert.IsFalse(ruleSet.Count < 1, "The passed in list of generative grammar replacement rules is empty!");
            Assert.IsNotNull(rng, "The passed in random number generator is null!");


            // Organize the passed in grammar replacement rule set.
            OrganizeGrammarReplacementRuleSet(ruleSet);
            //DEBUG_OutputRulesList();

            _MissionStructureGraph = graph;
            _RNG_MissionStructureGen = rng;


            MissionStructureGraphNode curNode;
            Queue<MissionStructureGraphNode> nodeQueue = new Queue<MissionStructureGraphNode>();


            // Do a breadth first scan of the graph.
            while (true)
            {
                int nonTerminalNodeCount = 0;
                int ruleCheckFails = 0;

                nodeQueue.Enqueue(_MissionStructureGraph.StartNode);

                while (nodeQueue.Count > 0)
                {
                    // Get the next node.
                    curNode = nodeQueue.Dequeue();


                    //Debug.Log($"DEQUEUE: {curNode.GrammarSymbol}");


                    // Add the current node's children to the queue.
                    MiscellaneousUtils.AddChildNodesToQueue(nodeQueue, curNode);


                    // Check if this is a terminal node.
                    if (GenerativeGrammar.IsTerminalSymbol(curNode.GrammarSymbol))
                        continue;
                    else
                        nonTerminalNodeCount++;

                    // This node is a non-terminal node, so find the rule type that applies to it and execute it.
                    if (!DoRuleCheck(curNode))
                        ruleCheckFails++;

                } // while nodeQueue is not empty


                // If no non-terminal nodes remain, or if we failed to apply a grammar replacement rule to all
                // remaining non-terminal nodes, then exit this while loop.
                if (nonTerminalNodeCount < 1 ||
                    ruleCheckFails == nonTerminalNodeCount)
                {
                    break;
                }



            } // end while


            // Calculate the lock count for each node (number of locks between it and the start room).
            EvaluateNodeLockCounts();

            // Set preliminary position values for all nodes so the MissionStructureGraphGizmos class will draw them correctly if enabled.
            SetPositions();


            return _MissionStructureGraph;
        }

        private static bool DoRuleCheck(MissionStructureGraphNode nodeToCheckAt)
        {
            Assert.IsNotNull(nodeToCheckAt, "GrammarRuleProcessor.DoRuleCheck() - The passed in node is null!");


            // Holds all matching rule categories we find.
            List<String> matching_1Node_RuleCategories = new List<string>();
            List<String> matching_2Node_RuleCategories = new List<string>();

            // Holds the child node of nodeToCheckAt that matched the corresponding rule in matching_2Node_RuleCategories.
            List<MSCNData> matchingChildNodes = new List<MSCNData>();


            //Debug.Log($"RULE CHECK ON {nodeToCheckAt.GrammarSymbol}");


            // Check each rule type to find one that can apply to the passed in non-terminal node.
            foreach (string ruleCategory in _GrammarReplacementRuleSet.Keys)
            {
                List<GrammarReplacementRule> ruleList = _GrammarReplacementRuleSet[ruleCategory];

                if (ruleList[0].LeftSide.StartNode.GrammarSymbol != nodeToCheckAt.GrammarSymbol)
                    continue;


                // Does this rule have one node on the left side?
                if (ruleList[0].LeftSide.Nodes.Count == 1)
                {
                    // This rule matches, so cache it.
                    matching_1Node_RuleCategories.Add(ruleCategory);

                    // We don't continue here, because we want the next if statement to also run.
                    // This is because there may be a rule with two nodes on the left side that starts with the same type of node.
                }

                // Does this rule have two nodes on the left side?
                if (ruleList[0].LeftSide.Nodes.Count == 2)
                {
                    // We only check the first child since having more than two nodes total is not supported for the left side of rules.
                    // If the structure on the left side of the rule is bigger than two nodes, it becomes much harder to process the grammar rules.
                    MSCNData ruleStartNodeChildData = ruleList[0].LeftSide.StartNode.ChildNodesData[0];

                    foreach (MSCNData childNodeData in nodeToCheckAt.ChildNodesData)
                    {
                        if (childNodeData.ChildNode.GrammarSymbol == ruleStartNodeChildData.ChildNode.GrammarSymbol &&
                            childNodeData.IsTightlyCoupled == ruleStartNodeChildData.IsTightlyCoupled)
                        {
                            // This rule matches, so cache it.
                            matching_2Node_RuleCategories.Add(ruleCategory);
                            matchingChildNodes.Add(childNodeData);
                        }

                    } // end foreach childNode

                } // end if


            } // end foreach graph


            // If there were no matching rules, return false.
            if (matching_1Node_RuleCategories.Count < 1 && matching_2Node_RuleCategories.Count < 1)
                return false;

            // If there were any matching rules with two nodes, then use that list.
            // We want these rules to have precedence so that all edge rules get applied to edges between nodes
            // before we execute rules that replace the parent node, as single node rules generally always do.
            List<string> listToUse;
            List<MSCNData> childNodeListToUse;
            bool isTwoNodeRule = matching_2Node_RuleCategories.Count > 0;
            if (isTwoNodeRule)
            {
                listToUse = matching_2Node_RuleCategories;
                childNodeListToUse = matchingChildNodes;
            }
            else
            {
                listToUse = matching_1Node_RuleCategories;
                childNodeListToUse = null;
            }

            // We found one or more matching rules, so select one at random.
            int index = _RNG_MissionStructureGen.RollRandomIntInRange(0, listToUse.Count - 1);

            // Execute the selected grammar replacement rule.
            ExecuteGrammarRule(nodeToCheckAt,
                               isTwoNodeRule ? matchingChildNodes[index] : null,
                               GetRandomGrammarRuleFromKey(listToUse[index]));

            return true;

        }

        /// <summary>
        /// This function executes the specified grammar replacement rule on the specified pair of linked nodes.
        /// </summary>
        /// <param name="node1">The first node.</param>
        /// <param name="childNodeData">The second node.</param>
        /// <param name="rule">The grammar replacement rule to apply.</param>
        private static void ExecuteGrammarRule(MissionStructureGraphNode node1, MSCNData childNodeData, GrammarReplacementRule rule)
        {
            Assert.IsNotNull(node1, "GrammarRuleProcessor.ExecuteGrammarRule() - The first passed in node is null!");
            // The node2 parameter will be null if the rule has only one node on the left side, so we don't assert on it here.
            Assert.IsNotNull(rule, "GrammarRuleProcessor.ExecuteGrammarRule() - The passed in grammar replacement rule is null!");

            if (childNodeData != null)
                Assert.IsTrue(node1.ChildNodesData.Contains(childNodeData), "node2 is not a child of node1!");


            // Keeps track of nodes from the right side of the rule that have already been created.
            Dictionary<uint, MissionStructureGraphNode> rightSideNodesCreated = new Dictionary<uint, MissionStructureGraphNode>();

            // Keeps track of nodes that were added by the grammar replacement rule we are executing.
            List<MissionStructureGraphNode> newNodes = new List<MissionStructureGraphNode>();


            //Debug.Log($"EXECUTE: \"{rule.Name}\"    Node1: {node1.GrammarSymbol}    Node2: {(childNodeData == null ? "null" : childNodeData.ChildNode.GrammarSymbol)}");


            // Remove the connection between the two nodes.
            node1.ChildNodesData.Remove(childNodeData);

            // "Replace" the first node by changing its type.
            node1.GrammarSymbol = rule.RightSide.StartNode.GrammarSymbol;


            MissionStructureGraphNode curRuleNode = null; // These are set to null to stop the compiler complaining about the if statement just below the while loop.
            MissionStructureGraphNode curNode = null;
            Queue<MissionStructureGraphNode> ruleNodeQueue = new Queue<MissionStructureGraphNode>(); // Holds nodes from the grammar replacement rule.
            Queue<MissionStructureGraphNode> nodeQueue = new Queue<MissionStructureGraphNode>(); // Holds nodes from or being created in our mission structure graph.
            ruleNodeQueue.Enqueue(rule.RightSide.StartNode);
            nodeQueue.Enqueue(node1);
            newNodes.Add(node1); // We have to add node1 to this list since it is always part of the operation carried out by a grammar replacement rule. Otherwise the loop will crash when it tries to dequeue the first node.


            while (ruleNodeQueue.Count > 0)
            {
                // Get the next node from the rule's right side, and then get the next node that's part of our mission structure graph (or being added to it during this grammar rule execution).
                curRuleNode = ruleNodeQueue.Dequeue();

                // Skip pre-existing nodes so we find the correct new child node that was added by the grammar replacement rule we are executing.
                curNode = nodeQueue.Dequeue();
                while (!newNodes.Contains(curNode))
                    curNode = nodeQueue.Dequeue();


                int childNodeIndex = 0;
                // Duplicate each child node and add it to the corresponding node in our mission structure graph.
                foreach (MSCNData ruleChildNodeData in curRuleNode.ChildNodesData)
                {
                    // Is this the node that corresponds to node2?
                    if (childNodeData != null && ruleChildNodeData.ChildNode.ID == 2)
                    {
                        // This node in the rule has ID 2, meaning it is the one that corresponds to node2. node2 may not exist if the rule is only replacing one node instead of two, in which case the else clause will run instead.
                        childNodeData.ChildNode.GrammarSymbol = ruleChildNodeData.ChildNode.GrammarSymbol;
                        childNodeData.IsTightlyCoupled = ruleChildNodeData.IsTightlyCoupled;
                        
                        if (!curNode.ContainsChild(childNodeData.ChildNode))
                            curNode.ChildNodesData.Add(childNodeData);

                        // Add it to the new nodes list so we can tell the difference between ones the rule created, and ones that already existed.
                        newNodes.Add(childNodeData.ChildNode);
                    }
                    else
                    {
                        uint id = ruleChildNodeData.ChildNode.ID;
                        MissionStructureGraphNode newNode;
                        if (!rightSideNodesCreated.ContainsKey(id))
                        {
                            newNode = new MissionStructureGraphNode(ruleChildNodeData.ChildNode.GrammarSymbol);

                            // Add the new node and its ID from the right side of the rule into this dictionary. That way if the same node is linked elsewhere in the rule, we can correctly link to it instead of accidentally recreating the same node.
                            rightSideNodesCreated.Add(ruleChildNodeData.ChildNode.ID, newNode);
                        }
                        else
                        {
                            newNode = rightSideNodesCreated[id];
                        }


                        if (!curNode.ContainsChild(newNode))
                        {
                            curNode.ChildNodesData.Add(new MSCNData(newNode, ruleChildNodeData.IsTightlyCoupled));

                            // Add it to the new nodes list so we can tell the difference between nodes the rule created and those that already existed.
                            newNodes.Add(newNode);
                        }

                        // Add the node to our mission structure graph as well.
                        _MissionStructureGraph.AddNode(newNode);

                        // If this is the goal node, then set it to the GoalNode field on the mission structure graph.
                        if (newNode.GrammarSymbol == GrammarSymbols.T_Goal)
                            _MissionStructureGraph.GoalNode = newNode;

                    } // end if


                    childNodeIndex++;

                } // end foreach childNode


                // Add child nodes to the queues.
                MiscellaneousUtils.AddChildNodesToQueue(ruleNodeQueue, curRuleNode);
                MiscellaneousUtils.AddChildNodesToQueue(nodeQueue, curNode);


            } // end while nodeQueue has at least one node

        }

        private static GrammarReplacementRule GetRandomGrammarRuleFromKey(string ruleCategory)
        {
            Assert.IsNotNull(ruleCategory, "GrammarRuleProcessor.GetRandomGrammarRuleFromKey() - The passed in grammar rule key is null!");


            // Get the list of grammar replacement rules associated with the passed in key.
            List<GrammarReplacementRule> ruleList = _GrammarReplacementRuleSet[ruleCategory];

            // Select a random index in the list.
            int index = _RNG_MissionStructureGen.RollRandomIntInRange(0, ruleList.Count - 1);


            // Return the randomly selected grammar rule.
            return ruleList[index];
        }


        private static void OrganizeGrammarReplacementRuleSet(List<GrammarReplacementRule> ruleSet)
        {
            _GrammarReplacementRuleSet = new Dictionary<string, List<GrammarReplacementRule>>();


            foreach (GrammarReplacementRule rule in ruleSet)
            {
                if (rule.LeftSide.Nodes.Count < 1)
                    throw new Exception($"GrammarRuleProcessor.OrganizeGrammarReplacementRuleSet() - The grammar replacement rule \"{rule.Name}\" has no nodes in the left side!");
                if (rule.LeftSide.StartNode.ChildNodesData.Count > 1)
                    throw new Exception($"GrammarRuleProcessor.OrganizeGrammarReplacementRuleSet() - The grammar replacement rule \"{rule.Name}\" has more than two nodes! This is not supported, because executing rules becomes much more complex and not so practical with larger structures in the left side of the rule.");

                if (rule.RightSide.Nodes.Count < 1)
                    throw new Exception($"GrammarRuleProcessor.OrganizeGrammarReplacementRuleSet() - The grammar replacement rule \"{rule.Name}\" has no nodes in the right side!");

                if (rule.LeftSide.StartNode == null)
                    throw new Exception($"GrammarRuleProcessor.OrganizeGrammarReplacementRuleSet() - The grammar replacement rule \"{rule.Name}\" has a null start node on the left side!");
                if (rule.RightSide.StartNode == null)
                    throw new Exception($"GrammarRuleProcessor.OrganizeGrammarReplacementRuleSet() - The grammar replacement rule \"{rule.Name}\" has a null start node on the right side!");



                // Check if we already have a list for this rule category. If not, create one.
                if (!_GrammarReplacementRuleSet.ContainsKey(rule.Category))
                    _GrammarReplacementRuleSet.Add(rule.Category, new List<GrammarReplacementRule>());

                if (RuleNameExistsInCategory(rule.Category, rule.Name))
                    throw new Exception($"GrammarRuleProcessor.OrganizeGrammarReplacementRuleSet() - A grammar replacement rule named \"{rule.Name}\" already exists in the category \"{rule.Category}\"!");


                // Add this rule into the appropriate list.
                List<GrammarReplacementRule> ruleList = _GrammarReplacementRuleSet[rule.Category];
                ruleList.Add(rule);

            } // end foreach

        }

        private static bool RuleNameExistsInCategory(string category, string ruleName)
        {
            List<GrammarReplacementRule> ruleCategory = _GrammarReplacementRuleSet[category];

            if (ruleCategory == null)
                throw new ArgumentException($"GrammarRuleProcessor.RuleNameExistsInCategory() - The passed in rule category \"{category}\" does not exist!");


            foreach (GrammarReplacementRule rule in ruleCategory)
            {
                if (rule.Name == ruleName)
                    return true;
            }


            return false;
        }

        public static void EvaluateNodeLockCounts()
        {            
            Queue<MissionStructureGraphNode> nodeQueue = new Queue<MissionStructureGraphNode>();
            List<MissionStructureGraphNode> visitedNodes = new List<MissionStructureGraphNode>();
            nodeQueue.Enqueue(_MissionStructureGraph.StartNode);
            _MissionStructureGraph.StartNode.LockCount = 0;

            while (true)
            {               
                MissionStructureGraphNode curNode = nodeQueue.Dequeue();


                foreach (MSCNData childNodeData in curNode.ChildNodesData)
                {
                    MissionStructureGraphNode childNode = childNodeData.ChildNode;

                    if (!visitedNodes.Contains(childNode))
                        visitedNodes.Add(childNode);

                    
                    // If this node is a lock room, then add one to its lock count.
                    if (childNode.GrammarSymbol == GrammarSymbols.T_Lock ||
                        childNode.GrammarSymbol == GrammarSymbols.T_Lock_Multi ||
                        childNode.GrammarSymbol == GrammarSymbols.T_Lock_Goal)
                    {
                        childNode.LockCount = Math.Max(childNode.LockCount, curNode.LockCount + 1);
                    }
                    else
                    {
                        childNode.LockCount = curNode.LockCount;
                    }


                    nodeQueue.Enqueue(childNodeData.ChildNode);
                }


                if (nodeQueue.Count < 1)
                    break;

            } // end foreach node

        }

        /// <summary>
        /// This method gives all the nodes positions so that they will draw nicely if MissionStructureGraphGizmos are neabled in MissionStructureGraphGizmos.cs.
        /// </summary>
        public static void SetPositions()
        {
            // This dictionary tracks the positions assigned to a node. If a node has multiple parents, it will have multiple
            // positions stored in its list in this dictionary. It's final position will be Vector2(maxX, averageY) of all
            // the positions in that list.
            Dictionary<MSCNData, List<Vector2>> childNodePosDict = new Dictionary<MSCNData, List<Vector2>>();

            MissionStructureGraphNode curNode = null;
            MissionStructureGraphNode prevNode = null;
            Queue<MissionStructureGraphNode> nodeQueue = new Queue<MissionStructureGraphNode>(); // = GetTrueBreadthFirstNodeQueue(_MissionStructureGraph);
            nodeQueue.Enqueue(_MissionStructureGraph.StartNode);

            // Traverse the node map in a breadth first manner.
            while (nodeQueue.Count > 0)
            {
                prevNode = curNode;

                // Get the next node from the queue.
                curNode = nodeQueue.Dequeue();

                // Calculate y-position of first child node. The will be stacked upward from this position.
                float startY = 0f;
                if (prevNode == null)
                    startY = curNode.Position.y - (curNode.ChildNodesData.Count / 2);
                else
                {
                    if (curNode.Position.y == prevNode.Position.y)
                        startY = curNode.Position.y - (curNode.ChildNodesData.Count / 2);
                    else if (curNode.Position.y > prevNode.Position.y)
                        startY = curNode.Position.y;
                    else // curNode.Position.y < prevNode.Position.y
                        startY = curNode.Position.y - (curNode.ChildNodesData.Count - 1);
                }


                CheckForNodeOverlap(curNode);

                //Debug.Log($"NODE: {curNode.GrammarSymbol}    POS: {curNode.Position}");


                int childIndex = 0;
                foreach (MSCNData childNodeData in curNode.GetPrioritizedChildNodeList())
                {
                    // If this child node is not tightly coupled to this node, then we need to check if it is tightly
                    // coupled to any other node. If so, we need to skip it here so it is added just after the parent
                    // node it is tightly coupled to. This is necessary if there are two branches in the mission structure
                    // that merge back together. This check prevents us from processing nodes after the merge twice.
                    if ((!childNodeData.IsTightlyCoupled) &&  // Check that the child is not tightly coupled to this node
                         _MissionStructureGraph.IsTightlyCoupledToAnyNode(childNodeData.ChildNode))  // Check if it is tightly coupled to any other node
                    {
                        continue;
                    }


                    float posX = curNode.Position.x + 1f;
                    float posY = startY + childIndex;

                    if (childNodeData.ChildNode.Position.x > posX)
                        posX = childNodeData.ChildNode.Position.x;

                    if (!childNodePosDict.ContainsKey(childNodeData))
                        childNodePosDict.Add(childNodeData, new List<Vector2>());

                    // Add the position calculated from the current node to the list of positions assigned to the childNode so far.
                    childNodePosDict[childNodeData].Add(new Vector2(posX, posY));


                    // Assign a position to the childNode based on all positions in its list.
                    childNodeData.ChildNode.Position = GetNodePosFromPositionsList(childNodePosDict[childNodeData]);
                    childIndex++;

                    //Debug.Log($"        CHILD NODE: {childNode.GrammarSymbol}    POS: {childNode.Position}");

                    if (!nodeQueue.Contains(childNodeData.ChildNode))
                        nodeQueue.Enqueue(childNodeData.ChildNode);

                } // end foreach childNodeData


            } // end while nodeQueue is not empty


            Debug.Log(new string('-', 256));
        }

        private static void CheckForNodeOverlap(MissionStructureGraphNode nodeToCheck)
        {
            MissionStructureGraphNode lastLargeBranchNode = null;

            foreach (MissionStructureGraphNode node in _MissionStructureGraph.Nodes)
            {
                if (node == nodeToCheck)
                    continue;

                if (node.ChildNodesData.Count >= 3)
                    lastLargeBranchNode = node;

                // Does the node to check overlap this node?
                if (nodeToCheck.Position == node.Position && nodeToCheck.Position != Vector3.zero)
                {
                    //Debug.Log($"OVERLAP:    Node1: {nodeToCheck.GrammarSymbol}    Node2: {node.GrammarSymbol}");

                    int yOffset = 0;
                    if (lastLargeBranchNode != null)
                    {
                        if (nodeToCheck.Position.y >= lastLargeBranchNode.Position.y)
                            yOffset = 1;
                        else
                            yOffset = -1;
                    }
                    else
                    {
                        if (nodeToCheck.Position.y >= 0)
                            yOffset = 1;
                        else
                            yOffset = -1;
                    }

                    nodeToCheck.Position = new Vector2(node.Position.x, node.Position.y + yOffset);

                }

            } // end foreach node

        }

        /// <summary>
        /// Calculates the position of a node based on all positions added into its list.
        /// The final result calculated is Vector2(maxX, averageY) of all the positions in that list.
        /// </summary>
        /// <param name="list">The list of positions to calculate a node position from.</param>
        /// <returns>The node position.</returns>
        private static Vector2 GetNodePosFromPositionsList(List<Vector2> list)
        {
            float posXMax = 0f;
            float posYSum = 0f;

            List<float> yValsAdded = new List<float>();

            int yCount = 0;
            foreach (Vector2 nodePos in list)
            {
                posXMax = (nodePos.x > posXMax) ? nodePos.x : posXMax;

                // Make sure we don't add the same y coordinate twice, as this will skew the calculated average y-position that we return below.
                if (yValsAdded.Contains(nodePos.y))
                    continue;

                yValsAdded.Add(nodePos.y);
                posYSum += nodePos.y;
                yCount++;

                //Debug.Log($"Added {nodePos}");

            } // end foreach nodePos


            return new Vector2(posXMax, (int)posYSum / yCount);
        }

        private static void DEBUG_OutputRulesList()
        {
            Debug.Log("");
            Debug.Log("GRAMMAR REPLACEMENT RULE SET:");
            Debug.Log(new string('-', 128));


            foreach (string ruleCategory in _GrammarReplacementRuleSet.Keys)
            {
                // Get all the grammar replacement rules in this category.
                List<GrammarReplacementRule> ruleList = _GrammarReplacementRuleSet[ruleCategory];

                if (ruleList[0].LeftSide.Nodes.Count == 1)
                {
                    Debug.Log($"CATEGORY:    \"{ruleCategory}\"    Node1={ruleList[0].LeftSide.StartNode.GrammarSymbol}");
                }
                else if (ruleList[0].LeftSide.Nodes.Count == 2)
                {
                    bool tightlyCoupled = ruleList[0].LeftSide.StartNode.ChildNodesData[0].IsTightlyCoupled;
                    string sep = tightlyCoupled ? "=====>" : "----->";
                    Debug.Log($"CATEGORY:    \"{ruleCategory}\"    Node1={ruleList[0].LeftSide.StartNode.GrammarSymbol}   {sep}   Node2={ruleList[0].LeftSide.StartNode.ChildNodesData[0].ChildNode.GrammarSymbol}");
                }


                // List them in the Unity console.
                foreach (GrammarReplacementRule rule in ruleList)
                {
                    Debug.Log($"                                {rule.Name}");

                } // end foreach rule


            } // end foreach ruleCategory


            Debug.Log(new string('-', 128));

        }



    }

}