using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.TileMaps;


using MSCNData = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.MissionStructureChildNodeData;


namespace ProceduralDungeon.DungeonGeneration
{

    public static class DungeonGeneratorUtils
    {
        public static void AddChildNodesToQueue(Queue<MissionStructureGraphNode> queue, MissionStructureGraphNode node)
        {
            foreach (MSCNData childNodeData in node.ChildNodesData)
            {
                // Check that the node is not already in the queue. If the same node gets in the queue
                // multiple times it can cause us to get stuck in an infinite loop and lock up the Unity Editor.
                if (!queue.Contains(childNodeData.ChildNode))
                    queue.Enqueue(childNodeData.ChildNode);
            }
        }

        public static Queue<MissionStructureGraphNode> DetermineRoomGenerationOrder(MissionStructureGraph missionStructureGraph)
        {
            if (missionStructureGraph.GoalNode == null)
                throw new System.Exception("DungeonGeneratorUtils.DetermineRoomGenerationOrder() - The mission structure graph has no goal node!");
            if (missionStructureGraph.StartNode == null)
                throw new System.Exception("DungeonGeneratorUtils.DetermineRoomGenerationOrder() - The mission structure graph has no start node!");



            List<MissionStructureGraphNode> generationOrder = new List<MissionStructureGraphNode>();

            // First, start with the goal node and work our way back to the start node.
            // We want to find just the main path through the dungeon, ignoring all other nodes.
            // This way the main path is generated first. This way we should avoid more generation errors,
            // like situations where it fails because the goal room can't be placed since an adjacent room
            // is making the required location of the room be an invalid placement option.
            MissionStructureGraphNode curStructureNode = missionStructureGraph.GoalNode;
            generationOrder.Insert(0, curStructureNode);
            while (true)
            {
                // Find the parent of the current node and make it the new current node (starting from the end of the list since we're working backwards here).
                curStructureNode = missionStructureGraph.FindLastParent(curStructureNode);
                

                if (curStructureNode == null)
                    throw new System.Exception("DungeonGeneratorUtils.DetermineRoomGenerationOrder() - Could not reach the mission structure start node, because the current node has no parent!");


                generationOrder.Insert(0, curStructureNode);

                // Did we reach the start of the list?
                if (curStructureNode.GrammarSymbol == GenerativeGrammar.Symbols.T_Entrance)
                    break;

            } // end while



            // Now go back and add in all the rest of the nodes that aren't part of the main path.
            Queue<MissionStructureGraphNode> nodeQueue = new Queue<MissionStructureGraphNode>();
            nodeQueue.Enqueue(missionStructureGraph.StartNode);

            while (nodeQueue.Count > 0)
            {
                curStructureNode = nodeQueue.Dequeue();

                if (curStructureNode == null)
                    throw new System.Exception("DungeonGeneratorUtils.DetermineRoomGenerationOrder() - The current mission structure node is null!");


                // If this node is not already in the generation order list, then add it.
                if (!generationOrder.Contains(curStructureNode))
                    generationOrder.Add(curStructureNode);


                // Now iterate through this node's children.
                // We are getting a prioritized child node list here because we want to queue the tightly coupled nodes
                // first since they need to be connected to the parent. That way, we don't waste doors on the parent room
                // by connecting ones that aren't tightly coupled to it.
                foreach (MSCNData childNodeData in curStructureNode.GetPrioritizedChildNodeList())
                {
                    MissionStructureGraphNode childNode = childNodeData.ChildNode;

                    // If this node is not already in the generation order list, then add it.
                    if (!generationOrder.Contains(childNode))
                        generationOrder.Add(childNode);


                    // If this child node is not tightly coupled to this node, then we need to check if it is tightly
                    // coupled to any other node. If so, we need to skip it here so it is generated just after the parent
                    // node it is tightly coupled to. This is necessary if there are two branches in the mission structure,
                    // and the child node is tightly coupled to a node in the longer branch that is deeper in the tree
                    // than the current node is. This way we ensure it is always after that parent in the dungeon layout
                    // as it should be.
                    if ((!childNodeData.IsTightlyCoupled) &&                          // Check that the current child is not tightly coupled to this node
                         missionStructureGraph.IsTightlyCoupledToAnyNode(childNode))  // Check if it is tightly coupled to any other node
                    {
                        continue;
                    }


                    nodeQueue.Enqueue(childNode);

                } // end foreach childNode

            } // end while (nodeQueue.Count > 0)


            return new Queue<MissionStructureGraphNode>(generationOrder);
        }

        public static void CopyTilesListToDictionary(List<SavedTile> srcTileList, Dictionary<Vector3Int, SavedTile> dstTileDict)
        {
            foreach (SavedTile sTile in srcTileList)
                dstTileDict.Add(sTile.Position, sTile);


        }

        /// <summary>
        /// This simply utility function corrects an object rotation direction to rectify the fact that many objects like
        /// chests and doors are facing south when they are not rotated. In other words, the face south when their rotation
        /// direction is north. This means they end up facing the wrong way when rotated 90 degrees, for example.
        /// </summary>
        /// <param name="original">The object's previous rotation direction.</param>
        /// <param name="newDirection">The object's new rotation direction.</param>
        /// <param name="roomFinalDirection">The final rotation direction of the object's parent room after rotation.</param>
        /// <returns>The corrected rotation direction.</returns>
        public static Directions CorrectObjectRotationDirection(Directions original, Directions newDirection, Directions roomFinalDirection)
        {
            Directions result = newDirection;

            
            if (roomFinalDirection == Directions.East || roomFinalDirection == Directions.West)
            {
                result = newDirection.FlipDirection();
            }
            
            
            return result;
        }

        public static Vector3Int GetUpperLeftMostTile(Vector3Int tile1Position, Vector3Int tile2Position)
        {
            if (tile1Position.x < tile2Position.x || tile1Position.y > tile2Position.y)
                return tile1Position;
            else
                return tile2Position;
        }



    }

}