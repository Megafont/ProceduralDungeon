using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;


namespace ProceduralDungeon.TileMaps.TileTypes
{
    /// <summary>
    /// This simple class stores data on a doorway tile.
    /// </summary>
    [CreateAssetMenu(fileName = "New Doorway Tile", menuName = "2D/Tiles/Dungeon Tiles/Doorway Tile")]
    public class DoorwayTile : BasicDungeonTile
    {

        // *********************************************************************************************************************************************
        // * NOTE: Properties added to this class must also be added to the DungeTile_DoorwayEditor class to get them to show up in the Unity Inspector!        *
        // *********************************************************************************************************************************************

        [SerializeField]
        public Tile ReplacementWallTile; // The wall tile the dungeon generator will use to fill in removed doorways with.

    }

}