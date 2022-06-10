using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.InGame
{
    public static class SpriteLoader
    {
        private static Dictionary<string, Dictionary<string, Sprite>> _SpritesDictionary;



        static SpriteLoader()
        {
            _SpritesDictionary = new Dictionary<string, Dictionary<string, Sprite>>();
        }



        public static Sprite GetItemSprite(string spriteName, RoomSets roomSet)
        {
            return GetSprite(spriteName, 
                             GetRoomSetName(roomSet), 
                             "Items");
        }

        public static Sprite GetObjectSprite(string spriteName, RoomSets roomSet)
        {
            return GetSprite(spriteName, 
                             GetRoomSetName(roomSet), 
                             "Objects");
        }

        private static Sprite GetSprite(string spriteName, string roomSetName, string type)
        {
            Sprite sprite;
            Dictionary<string, Sprite> dict;


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
            sprite = Resources.Load<Sprite>($"{spritesPath}/{type}/{spriteName}");
            dict.Add(spriteName, sprite);

            return sprite;
        }

        private static string GetRoomSetName(RoomSets roomSet)
        {
            return Enum.GetName(typeof(RoomSets), roomSet);
        }


    }

}
