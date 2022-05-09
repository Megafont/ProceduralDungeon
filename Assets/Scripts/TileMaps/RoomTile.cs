using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;


namespace ProceduralDungeon.TileMaps
{
    /// <summary>
    /// This simple class stores data on a single tile, and inherits from Unity's Tile class.
    /// </summary>
    /// <remarks>
    /// This class is based on the great YouTube video (https://www.youtube.com/watch?v=TeEWLC-QKjw),
    /// which also covers saving and loading in two ways. The second is better for games with built-in
    /// level editors. 
    /// </remarks>
    [CreateAssetMenu(fileName = "New Room Tile", menuName = "2D/Tiles/Room Tile")]
    public class RoomTile : Tile
    {
        [SerializeField]
        public bool RotateWithRoom = true;

        [SerializeField]
        public RoomTileTypes TileType;

    }

}