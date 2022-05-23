using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.TileMaps.TileTypes;


namespace ProceduralDungeon.TileMaps
{
    /// <summary>
    /// This struct is used for saving tile data.
    /// </summary>
    [Serializable]
    public struct SavedTile
    {
        public SavedTile(BasicDungeonTile tile, Vector3Int position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
            Tile = tile;
        }


        public Vector3Int Position;
        public Quaternion Rotation;
        public BasicDungeonTile Tile;
    }

}