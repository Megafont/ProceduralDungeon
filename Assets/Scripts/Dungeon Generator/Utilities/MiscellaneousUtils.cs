using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.DungeonGeneration.Utilities
{
    public static class MiscellaneousUtils
    {
        public static void CopyTilesListToDictionary(List<SavedTile> srcTileList, Dictionary<Vector3Int, SavedTile> dstTileDict)
        {
            foreach (SavedTile sTile in srcTileList)
            {
                dstTileDict.Add(sTile.Position, sTile);
            } // end foreach

        }


    }

}