using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.DungeonGeneration.Utilities
{
    public enum Directions
    {
        North = 0,
        East,
        South,
        West,
    }



    public static class MiscellaneousUtils
    {
        /// <summary>
        /// This function is used to adjust a rotation direction by adding another one to it.
        /// For example, it is used to adjust the direction of a door to take into account the rotation direction of the parent room.
        /// </summary>
        /// <param name="direction1">The first rotation direction.</param>
        /// <param name="direction2">The second rotation direction.</param>
        /// <returns>The result of adding the two rotation directions together.</returns>
        public static Directions AddRotationDirectionsTogether(Directions direction1, Directions direction2)
        {
            int result = (int)direction1 + (int)direction2;

            if (result > (int)Directions.West)
                result -= (int)Directions.West + 1;

            return (Directions)result;
        }

        public static void CopyTilesListToDictionary(List<SavedTile> srcTileList, Dictionary<Vector3Int, SavedTile> dstTileDict)
        {
            foreach (SavedTile sTile in srcTileList)
            {
                dstTileDict.Add(sTile.Position, sTile);
            } // end foreach

        }

        public static Directions FlipDirection(Directions direction)
        {
            if (direction == Directions.North)
                return Directions.South;
            else if (direction == Directions.East)
                return Directions.West;
            else if (direction == Directions.South)
                return Directions.North;
            else if (direction == Directions.West)
                return Directions.East;


            throw new System.Exception("MiscellaneousUtils.FlipDirection() - Somehow the passed in direction is invalid!");
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