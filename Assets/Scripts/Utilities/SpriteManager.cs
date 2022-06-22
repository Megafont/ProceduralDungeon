using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.Utilities
{
    public static class SpriteManager
    {
        private static Dictionary<string, Dictionary<string, Sprite>> _SpritesDictionary;



        static SpriteManager()
        {
            _SpritesDictionary = new Dictionary<string, Dictionary<string, Sprite>>();
        }



        public static Sprite GetSprite(string spriteName, RoomSets roomSet)
        {
            Sprite sprite;
            Dictionary<string, Sprite> dict;


            string roomSetName = ManagerUtils.GetRoomSetName(roomSet);

            _SpritesDictionary.TryGetValue(roomSetName, out dict);
            if (dict != null)
            {
                dict.TryGetValue(spriteName, out sprite);

                if (sprite != null)
                    return sprite;
            }
            else
            {
                dict = new Dictionary<string, Sprite>();
                _SpritesDictionary.Add(roomSetName, dict);
            }

            string spritesPath = ScriptableRoomUtilities.GetRoomSetSpritesPath(roomSetName);
            string type = ManagerUtils.GetResourceTypeFromName(spriteName);
            sprite = Resources.Load<Sprite>($"{spritesPath}/{type}/{spriteName}");

            if (dict.ContainsKey(spriteName))
                dict[spriteName] = sprite;
            else
                dict.Add(spriteName, sprite);


            return sprite;

        }


    }

}
