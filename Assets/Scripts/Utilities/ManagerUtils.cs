using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.Utilities
{
    internal class ManagerUtils
    {
        public static string GetResourceTypeFromName(string name)
        {
            string nameLower = name.ToLower();


            // These are in order of how frequenetly they are used to make this function a bit faster.
            if (nameLower.StartsWith("object_"))
                return "Objects/";
            else if (nameLower.StartsWith("item_"))
                return "Items/";
            else if (nameLower.StartsWith("ui_"))
                return "UI/";
            else if (nameLower.StartsWith(@"enemy_"))
                return "Enemies/";
            else if (nameLower.StartsWith(@"boss_"))
                return "Enemies/Bosses/";
            else if (nameLower.StartsWith(@"miniboss_"))
                return "Enemies/Minibosses/";
            else if (nameLower.StartsWith(@"projectile_"))
                return "Projectiles/";
            else
                return "";
            
        }

        public static string GetRoomSetName(RoomSets roomSet)
        {
            return Enum.GetName(typeof(RoomSets), roomSet);
        }


    }


}
