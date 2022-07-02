using System;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items.Definitions;


namespace ProceduralDungeon.InGame.Items
{
    [Serializable]
    public class ItemDataWithBuffs : ItemData
    {
        //[HideInInspector]
        public uint InstanceID; // This is a unique ID for items with buffs so you can tell them apart since the ID field will be the same for all instances of an item.


        public readonly Dictionary<ItemAttributes, float> Buffs;



        /// <summary>
        /// This class represents an instance of an item in an inventory.
        /// </summary>
        /// <param name="item"></param>
        public ItemDataWithBuffs(ItemWithBuffsDefinition item)
            : base(item)
        {
            InstanceID = item.InstanceID;


            if (item.Buffs.Length > 0)
            {
                // This is initialized inside this if statement, because there is no sense in creating the dictionary if there are no buffs to store in it. So we just leave it null in that case.
                Buffs = new Dictionary<ItemAttributes, float>(); 

                for (int i = 0; i < item.Buffs.Length; i++)
                {
                    ItemBuff buff = item.Buffs[i];

                    Buffs.Add(buff.Attribute, buff.GenerateAttributeValue());

                } // end for i

            }

        }


    }

}
