using System;

using UnityEngine;

using ProceduralDungeon.InGame.Items.Definitions;


namespace ProceduralDungeon.InGame.Items
{
    public enum KeyTypes
    {
        Key = 0,
        Key_Multipart,
        Key_Goal,
    }



    [Serializable]
    public class ItemData_Key : ItemData
    {
        public KeyTypes KeyType;
        public uint KeyID;



        /// <summary>
        /// This class represents an instance of an item in an inventory.
        /// </summary>
        /// <param name="item"></param>
        public ItemData_Key(ItemDefinition_Key item)
            : base(item)
        {
            KeyType = item.KeyType;
            KeyID = item.KeyID;
        }


    }


}
