using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;


namespace ProceduralDungeon.TileMaps.TileTypes
{
    /// <summary>
    /// This simple class stores data on a single tile.
    /// </summary>
    /// <remarks>
    /// This class is based on the great YouTube video (https://www.youtube.com/watch?v=TeEWLC-QKjw),
    /// which also covers saving and loading in two ways. The second is better for games with built-in
    /// level editors. 
    /// </remarks>
    [Serializable]
    [CreateAssetMenu(fileName = "New Dungeon Tile", menuName = "2D/Tiles/Dungeon Tiles/Basic Dungeon Tile")]
    public class BasicDungeonTile : Tile
    {

        // *********************************************************************************************************************************************
        // * NOTE: Properties added to this class must also be added to the BasicDungeonTile_Editor class to get them to show up in the Unity Inspector!        *
        // *********************************************************************************************************************************************

        [SerializeField]
        public bool RotateWithRoom = true;

        [SerializeField]
        public DungeonTileTypes TileType;

    }

}