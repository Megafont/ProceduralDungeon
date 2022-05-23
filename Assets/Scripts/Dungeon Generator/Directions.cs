using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.DungeonGeneration
{
    public enum Directions
    {
        North = 0,
        East,
        South,
        West,
    }



    public static class DirectionsExtensionMethods
    {
        private static Quaternion[] _Rotations = { Quaternion.Euler(0, 0, 0f),
                                                   Quaternion.Euler(0, 0, 90f),
                                                   Quaternion.Euler(0, 0, 180f),
                                                   Quaternion.Euler(0, 0, 270f) };



        public static Vector3Int DirectionToNormalizedVector(this Directions direction)
        {
            if (direction == Directions.North)
                return Vector3Int.up;
            else if (direction == Directions.East)
                return Vector3Int.right;
            else if (direction == Directions.South)
                return Vector3Int.down;
            else if (direction == Directions.West)
                return Vector3Int.left;


            throw new System.Exception("MiscellaneousUtils.DirectionToVector() - Somehow the passed in direction is invalid!");

        }

        public static Directions FlipDirection(this Directions direction)
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

        public static Quaternion DirectionToRotation(this Directions direction)
        {
            return _Rotations[(int)direction];
        }


    }

}
