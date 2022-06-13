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

    public static class MiscellaneousUtils
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
        /// <returns>The corrected rotation direction.</returns>
        public static Directions CorrectObjectRotationDirection(Directions original, Directions newDirection)
        {
            Directions result = newDirection;

            if ((original == Directions.North || original == Directions.South) &&
                (newDirection == Directions.East || newDirection == Directions.West))
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