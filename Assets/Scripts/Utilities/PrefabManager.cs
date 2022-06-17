using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.Utilities
{
    public static class PrefabManager
    {
        private const string PREFABS_PATH = @"Prefabs"; // Do NOT include a trailing / on this path.

        private static Dictionary<string, Dictionary<string, GameObject>> _PrefabsDictionary;



        static PrefabManager()
        {
            _PrefabsDictionary = new Dictionary<string, Dictionary<string, GameObject>>();
        }



        public static GameObject GetItemPrefab(string prefabName, RoomSets roomSet)
        {
            return GetPrefab(prefabName,
                             GetRoomSetName(roomSet),
                             "Items");
        }

        public static GameObject GetObjectPrefab(string prefabName, RoomSets roomSet)
        {
            return GetPrefab(prefabName,
                             GetRoomSetName(roomSet),
                             "Objects");
        }

        public static GameObject GetUIPrefab(string prefabName, RoomSets roomSet)
        {
            return GetPrefab(prefabName,
                             GetRoomSetName(roomSet),
                             "UI");
        }

        private static GameObject GetPrefab(string prefabName, string roomSetName, string type)
        {
            GameObject prefab;
            Dictionary<string, GameObject> dict;


            _PrefabsDictionary.TryGetValue(roomSetName, out dict);
            if (dict != null)
            {
                dict.TryGetValue(prefabName, out prefab);

                if (prefab != null)
                    return prefab;
            }
            else
            {
                dict = new Dictionary<string, GameObject>();
                _PrefabsDictionary.Add(roomSetName, dict);
            }

            prefab = Resources.Load<GameObject>($"{PREFABS_PATH}/{type}/{prefabName}");

            if (dict.ContainsKey(prefabName))
                dict[prefabName] = prefab;
            else
                dict.Add(prefabName, prefab);


            return prefab;

        }

        private static string GetRoomSetName(RoomSets roomSet)
        {
            return Enum.GetName(typeof(RoomSets), roomSet);
        }


    }

}
