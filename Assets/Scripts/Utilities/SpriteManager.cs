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
            string spritePath = GetFullSpritePath(spriteName, roomSetName);


            dict = GetRoomSetDictionary(roomSetName);
 
            dict.TryGetValue(spritePath, out sprite);
            if (sprite != null)
                return sprite;


            sprite = Resources.Load<Sprite>(spritePath);

            //Debug.Log($"Loaded sprite: \"{spritePath}\"");


            if (dict.ContainsKey(spritePath))
                dict[spritePath] = sprite;
            else
                dict.Add(spritePath, sprite);


            return sprite;

        }

        public static Sprite GetSpriteFromSheet(string spriteSheetName, string spriteName, RoomSets roomSet, bool cacheAllSpritesFromSheet = false)
        {
            Sprite sprite;
            Sprite[] spriteSheetSprites;
            Dictionary<string, Sprite> dict;


            string roomSetName = ManagerUtils.GetRoomSetName(roomSet);
            string spriteSheetPath = GetFullSpritePath(spriteSheetName, roomSetName); // We intentionally pass in the sprite sheet name here rather than the sprite name since accessing a sprite from a sheet is a bit different than an individual.
            string spriteSheetPathKey = $"{spriteSheetPath}.{spriteName}";

            dict = GetRoomSetDictionary(roomSetName);

            dict.TryGetValue(spriteSheetPathKey, out sprite);
            if (sprite != null)
                return sprite;


            spriteSheetSprites = Resources.LoadAll<Sprite>(spriteSheetPath);

            //Debug.Log($"Loaded sprite from sheet: \"{spriteSheetPathKey}\"");


            foreach (Sprite s in spriteSheetSprites)
            {
                bool isMatch = false;
                string spriteKey = $"{spriteSheetPath}.{s.name}";


                if (s.name == spriteName)
                {
                    sprite = s;
                    isMatch = true;
                }

                if (isMatch || cacheAllSpritesFromSheet)
                {
                    if (dict.ContainsKey(spriteKey))
                        dict[spriteKey] = s;
                    else
                        dict.Add(spriteKey, s);
                }


                if (isMatch && !cacheAllSpritesFromSheet)
                    return sprite;
            }


            return sprite;

        }

        private static void CacheSpriteSheet(Sprite[] spriteSheetSprites, string spriteSheetPath, Dictionary<string, Sprite> dict)
        {
            foreach (Sprite sprite in spriteSheetSprites)
            {
                string key = $"{spriteSheetPath}.{sprite.name}";

                if (dict.ContainsKey(key))
                    dict[key] = sprite;
                else
                    dict.Add(key, sprite);
            }

        }

        private static string GetFullSpritePath(string spriteName, string roomSetName)
        {
            string spritesPath = ScriptableRoomUtilities.GetRoomSetSpritesPath(roomSetName);
            string type = ManagerUtils.GetResourceTypeFromName(spriteName);


            return $"{spritesPath}/{type}{spriteName}";
        }

        private static Dictionary<string, Sprite> GetRoomSetDictionary(string roomSetName)
        {
            Dictionary<string, Sprite> dict;


            _SpritesDictionary.TryGetValue(roomSetName, out dict);
            if (dict == null)
            {
                dict = new Dictionary<string, Sprite>();
                _SpritesDictionary.Add(roomSetName, dict);
            }


            return dict;
        }


    }

}
