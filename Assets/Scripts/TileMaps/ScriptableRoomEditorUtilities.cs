using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;



#if UNITY_EDITOR

namespace ProceduralDungeon.TileMaps
{
    public static class ScriptableRoomEditorUtilities
    {
        
        public static void SaveRoomAsset(ScriptableRoom room, string roomSet)
        {
            AssetDatabase.CreateAsset(room, ScriptableRoomUtilities.GetRoomFilePath(room.RoomName, roomSet));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

    }

}

#endif
