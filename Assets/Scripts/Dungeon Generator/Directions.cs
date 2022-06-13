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



        /// <summary>
        /// This function is used to adjust a rotation direction by adding another one to it.
        /// For example, it is used to adjust the direction of a door to take into account the rotation direction of the parent room.
        /// As an example, North + East = South (0 + 90 = 180 degrees)
        /// </summary>
        /// <param name="direction1">The first rotation direction.</param>
        /// <param name="direction2">The second rotation direction.</param>
        /// <returns>The result of adding the two rotation directions together.</returns>
        public static Directions AddRotationDirection(this Directions direction1, Directions direction2)
        {
            int result = (int)direction1 + (int)direction2;

            if (result > (int)Directions.West)
                result -= (int)Directions.West + 1;

            return (Directions) result;
        }

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

        public static Directions DirectionFromRotation(this Directions direction, Quaternion rotation)
        {
            Directions result = Directions.North;

            float y = Mathf.Round(rotation.eulerAngles.y);
            float z = Mathf.Round(rotation.eulerAngles.z);


            // Check if the placeholder tile for the object being spawned is mirrored. If so, this changes some of the directions.
            if (y == 180f)
            {
                if (z == 0f)
                    result = Directions.North;
                else if (z == 90f)
                    result = Directions.West;
                else if (z == 180f)
                    result = Directions.South;
                else if (z == 270f)
                    result = Directions.East;
            }
            else // The placeholder tile for the object being spawned is not mirrored.
            {
                if (z == 0f)
                    result = Directions.North;
                else if (z == 90f)
                    result = Directions.East;
                else if (z == 180f)
                    result = Directions.South;
                else if (z == 270f)
                    result = Directions.West;
            }

            //Debug.Log("DIRECTION: " + result + "    ROTATION: " + rotation.eulerAngles);

            return result;
        }


    }

}
