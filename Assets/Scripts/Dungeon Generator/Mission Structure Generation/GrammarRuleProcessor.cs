using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Assertions;

using ToolboxLib_Shared.Math;

using ProceduralDungeon.DungeonGeneration;


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;


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
            DEBUG_OutputRulesList();

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


            // Set preliminary position values for all nodes so the MissionStructureGraphGizmos class will draw them correctly if enabled.
            SetPositions2();


            return _MissionStructureGraph;
        }

        private static bool DoRuleCheck(MissionStructureGraphNode nodeToCheckAt)
        {
            Assert.IsNotNull(nodeToCheckAt, "GrammarRuleProcessor.DoRuleCheck() - The passed in node is null!");


            // Holds all matching rule categories we find.
            List<String> matching_1Node_RuleCategories = new List<string>();
            List<String> matching_2Node_RuleCategories = new List<string>();

            // Holds the child node of nodeToCheckAt that matched the corresponding rule in matching_2Node_RuleCategories.
            List<MissionStructureGraphNode> matchingChildNodes = new List<MissionStructureGraphNode>();


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
                    MissionStructureGraphNode ruleStartNodeChild = ruleList[0].LeftSide.StartNode.ChildNodes[0];

                    foreach (MissionStructureGraphNode childNode in nodeToCheckAt.ChildNodes)
                    {
                        if (childNode.GrammarSymbol == ruleStartNodeChild.GrammarSymbol &&
                            childNode.IsTightlyCoupled == ruleStartNodeChild.IsTightlyCoupled)
                        {
                            // This rule matches, so cache it.
                            matching_2Node_RuleCategories.Add(ruleCategory);
                            matchingChildNodes.Add(childNode);
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
            List<MissionStructureGraphNode> childNodeListToUse;
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
        /// <param name="node2">The second node.</param>
        /// <param name="rule">The grammar replacement rule to apply.</param>
        private static void ExecuteGrammarRule(MissionStructureGraphNode node1, MissionStructureGraphNode node2, GrammarReplacementRule rule)
        {
            Assert.IsNotNull(node1, "GrammarRuleProcessor.ExecuteGrammarRule() - The first passed in node is null!");
            // The node2 parameter will be null if the rule has only one node on the left side, so we don't assert on it here.
            Assert.IsNotNull(rule, "GrammarRuleProcessor.ExecuteGrammarRule() - The passed in grammar replacement rule is null!");

            if (node2 != null)
                Assert.IsTrue(node1.ChildNodes.Contains(node2), "node2 is not a child of node1!");


            // Keeps track of nodes from the left side of the rule that have already been created.
            Dictionary<uint, MissionStructureGraphNode> leftSideNodesCreated = new Dictionary<uint, MissionStructureGraphNode>();

            // Keeps track of nodes that were added by the grammar replacement rule we are executing.
            List<MissionStructureGraphNode> newNodes = new List<MissionStructureGraphNode>();


            Debug.Log($"EXECUTE: \"{rule.Name}\"    Node1: {node1.GrammarSymbol}    Node2: {(node2 == null ? "null" : node2.GrammarSymbol)}");


            // Remove the connection between the two nodes.
            node1.ChildNodes.Remove(node2);

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
                foreach (MissionStructureGraphNode childNode in curRuleNode.ChildNodes)
                {
                    // Is this the node that corresponds to node2?
                    if (node2 != null && childNode.ID == 2)
                    {
                        // This node in the rule has ID 2, meaning it is the one that corresponds to node2. node2 may not exist if the rule is only replacing one node instead of two, in which case the else clause will run instead.
                        node2.GrammarSymbol = childNode.GrammarSymbol;
                        node2.IsTightlyCoupled = childNode.IsTightlyCoupled;
                        curNode.ChildNodes.Add(node2);

                        // Add it to the new nodes list so we can tell the difference between ones the rule created, and ones that already existed.
                        newNodes.Add(node2);

                        // Flag that node2 has been inserted.
                    }
                    else
                    {
                        uint id = childNode.ID;
                        MissionStructureGraphNode newNode;
                        if (!leftSideNodesCreated.ContainsKey(id))
                        {
                            newNode = new MissionStructureGraphNode(childNode.GrammarSymbol, childNode.IsTightlyCoupled);

                            newNode.LockCount = curNode.LockCount; // Set this node's lock count equal to that of its parent.
                            // If this node is a lock room, then add one to its lock count.
                            if (newNode.GrammarSymbol == GrammarSymbols.T_Lock ||
                                newNode.GrammarSymbol == GrammarSymbols.T_Lock_Multi ||
                                newNode.GrammarSymbol == GrammarSymbols.T_Lock_Goal)
                            {
                                newNode.LockCount++;
                            }

                            // Add the new node and its ID from the left side of the rule into this dictionary. That way if the same node is linked elsewhere in the rule, we can correctly link to it instead of accidentally recreating the same node.
                            leftSideNodesCreated.Add(childNode.ID, newNode);
                        }
                        else
                        {
                            newNode = leftSideNodesCreated[id];
                        }

                        curNode.ChildNodes.Add(newNode);

                        // Add it to the new nodes list so we can tell the difference between nodes the rule created and those that already existed.
                        newNodes.Add(newNode);

                        // Add the node to our mission structure graph as well.
                        _MissionStructureGraph.Nodes.Add(newNode);

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
                if (rule.LeftSide.StartNode.ChildNodes.Count > 1)
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

        /// <summary>
        /// This method gives all the nodes positions so that they will draw nicely if MissionStructureGraphGizmos are neabled in MissionStructureGraphGizmos.cs.
        /// </summary>
        private static void SetPositions()
        {
            // This dictionary tracks the positions assigned to a node. If a node has multiple parents, it will have multiple
            // positions stored in its list in this dictionary. It's final position will be Vector2(maxX, averageY) of all
            // the positions in that list.
            Dictionary<MissionStructureGraphNode, List<Vector2>> childNodePosDict = new Dictionary<MissionStructureGraphNode, List<Vector2>>();

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
                    startY = curNode.Position.y - (curNode.ChildNodes.Count / 2);
                else
                {
                    if (curNode.Position.y == prevNode.Position.y)
                        startY = curNode.Position.y - (curNode.ChildNodes.Count / 2);
                    else if (curNode.Position.y > prevNode.Position.y)
                        startY = curNode.Position.y;
                    else // curNode.Position.y < prevNode.Position.y
                        startY = curNode.Position.y - (curNode.ChildNodes.Count - 1);
                }


                CheckForNodeOverlap(curNode);

                //Debug.Log($"NODE: {curNode.GrammarSymbol}    POS: {curNode.Position}");

                int childIndex = 0;
                foreach (MissionStructureGraphNode childNode in curNode.ChildNodes)
                {
                    float posX = curNode.Position.x + 1f;
                    float posY = startY + childIndex;

                    if (childNode.Position.x > posX)
                        posX = childNode.Position.x;

                    if (!childNodePosDict.ContainsKey(childNode))
                        childNodePosDict.Add(childNode, new List<Vector2>());

                    // Add the position calculated from the current node to the list of positions assigned to the childNode so far.
                    childNodePosDict[childNode].Add(new Vector2(posX, posY));


                    // Assign a position to the childNode based on all positions in its list.
                    childNode.Position = GetNodePosFromPositionsList(childNodePosDict[childNode]);//new Vector3(posX, posY);
                    childIndex++;

                    //Debug.Log($"        CHILD NODE: {childNode.GrammarSymbol}    POS: {childNode.Position}");

                }

                MiscellaneousUtils.AddChildNodesToQueue(nodeQueue, curNode);

            } // end while nodeQueue is not empty


            Debug.Log(new string('-', 256));
        }

        /// <summary>
        /// This is an alternate version of SetPositions() that uses a depth first scan rather than a breadth first one.
        /// It seems to work a little bit better, whereas the original has an issue where sometimes you'll get some nodes
        /// placed on top of each other when the graph is drawn by the MissionStructureGraphGizmos class without the
        /// graph nodes snapped to generated rooms.
        /// </summary>
        private static void SetPositions2()
        {
            // This dictionary tracks the positions assigned to a node. If a node has multiple parents, it will have multiple
            // positions stored in its list in this dictionary. It's final position will be Vector2(maxX, averageY) of all
            // the positions in that list.
            Dictionary<MissionStructureGraphNode, List<Vector2>> childNodePosDict = new Dictionary<MissionStructureGraphNode, List<Vector2>>();

            MissionStructureGraphNode curNode = null;
            MissionStructureGraphNode prevNode = null;
            Stack<MissionStructureGraphNode> nodeStack = new Stack<MissionStructureGraphNode>();
            nodeStack.Push(_MissionStructureGraph.StartNode);

            // Traverse the node map in a breadth first manner.
            while (nodeStack.Count > 0)
            {
                prevNode = curNode;

                // Get the next node from the queue.
                curNode = nodeStack.Pop();

                // Calculate y-position of first child node. The will be stacked upward from this position.
                float startY = 0f;
                if (prevNode == null)
                    startY = curNode.Position.y - (int)(curNode.ChildNodes.Count / 2);
                else
                {
                    if (curNode.Position.y == prevNode.Position.y)
                        startY = curNode.Position.y - (int)(curNode.ChildNodes.Count / 2);
                    else if (curNode.Position.y > prevNode.Position.y)
                        startY = curNode.Position.y;
                    else // curNode.Position.y < prevNode.Position.y
                        startY = curNode.Position.y - (int)(curNode.ChildNodes.Count - 1);
                }


                CheckForNodeOverlap(curNode);

                //Debug.Log($"NODE: {curNode.GrammarSymbol}    POS: {curNode.Position}");

                int childIndex = 0;
                foreach (MissionStructureGraphNode childNode in curNode.ChildNodes)
                {
                    float posX = curNode.Position.x + 1f;
                    float posY = startY + childIndex;

                    if (childNode.Position.x > posX)
                        posX = childNode.Position.x;

                    if (!childNodePosDict.ContainsKey(childNode))
                        childNodePosDict.Add(childNode, new List<Vector2>());

                    // Add the position calculated from the current node to the list of positions assigned to the childNode so far.
                    childNodePosDict[childNode].Add(new Vector2(posX, posY));


                    // Assign a position to the childNode based on all positions in its list.
                    childNode.Position = GetNodePosFromPositionsList(childNodePosDict[childNode]);//new Vector3(posX, posY);
                    childIndex++;

                    //Debug.Log($"        CHILD NODE: {childNode.GrammarSymbol}    POS: {childNode.Position}");

                }

                foreach (MissionStructureGraphNode childNode in curNode.ChildNodes)
                    nodeStack.Push(childNode);

            } // end while nodeQueue is not empty

        }

        private static void CheckForNodeOverlap(MissionStructureGraphNode nodeToCheck)
        {
            MissionStructureGraphNode lastLargeBranchNode = null;

            foreach (MissionStructureGraphNode node in _MissionStructureGraph.Nodes)
            {
                if (node == nodeToCheck)
                    continue;

                if (node.ChildNodes.Count >= 3)
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

                    //nodeToCheck.Position = new Vector2(node.Position.x + 1, node.Position.y);

                    //foreach (MissionStructureGraphNode childNode in node.ChildNodes)
                    //    childNode.Position = new Vector2(childNode.Position.x + 1, childNode.Position.y);
                }
            }

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

        /// <summary>
        /// This function is needed because our mission structure graphs can have multiple branches merge back together because of multipart keys.
        /// When this happens, if one branch is shorter than the others, it short circuits the processing and causes later nodes to get processed
        /// before some of the branches before them are done. This function gets us a proper queue with the nodes in the correct order.
        /// </summary>
        /// <param name="graph">The mission structure graph to make a queue for.</param>
        /// <returns>A queue with all nodes in the proper order from start node to end node.</returns>
        private static Queue<MissionStructureGraphNode> GetTrueBreadthFirstNodeQueue(MissionStructureGraph graph)
        {
            List<MissionStructureGraphNode> nodeList = new List<MissionStructureGraphNode>();
            MissionStructureGraphNode curNode;
            Queue<MissionStructureGraphNode> nodeQueue = new Queue<MissionStructureGraphNode>();

            nodeQueue.Enqueue(_MissionStructureGraph.StartNode);
            nodeList.Add(graph.StartNode);

            // Traverse the node map in a breadth first manner.
            while (nodeQueue.Count > 0)
            {
                // Get the next node from the queue.
                curNode = nodeQueue.Dequeue();

                foreach (MissionStructureGraphNode childNode in curNode.ChildNodes)
                {
                    // If the node list already contains this child node, then remove it and add it again so it is at the end of the list.
                    // This is how we ensure that the nodes are in the right order to solve the short circuiting issue mentioned in the
                    // documentation of this function.
                    if (nodeList.Contains(childNode))
                        nodeList.Remove(childNode);

                    nodeList.Add(childNode);
                }

                MiscellaneousUtils.AddChildNodesToQueue(nodeQueue, curNode);

            } // end while


            nodeQueue = new Queue<MissionStructureGraphNode>();
            Debug.Log(new string('*', 256));
            foreach (MissionStructureGraphNode node in nodeList)
            {
                Debug.Log(node.GrammarSymbol);
                nodeQueue.Enqueue(node);
            }
            Debug.Log(new string('*', 256));

            return nodeQueue;
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
                    bool tightlyCoupled = ruleList[0].LeftSide.StartNode.ChildNodes[0].IsTightlyCoupled;
                    string sep = tightlyCoupled ? "=====>" : "----->";
                    Debug.Log($"CATEGORY:    \"{ruleCategory}\"    Node1={ruleList[0].LeftSide.StartNode.GrammarSymbol}   {sep}   Node2={ruleList[0].LeftSide.StartNode.ChildNodes[0].GrammarSymbol}");
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