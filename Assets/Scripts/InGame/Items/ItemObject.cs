using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items
{
    public enum ItemTypes2
    {
        Food,
        Equipment,
        Default
    }

    public enum ItemAttributes
    {
        Agility,
        Intellect,
        Stamina,
        Strength,
    }



    /// <summary>
    /// This is the abstract base class for our item system.
    /// I learned how to make this item/inventory system from the video series by Coding With Unity:
    /// https://www.youtube.com/watch?v=_IqTeruf3-s&list=PLJWSdH2kAe_Ij7d7ZFR2NIW8QCJE74CyT
    /// </summary>
    public abstract class ItemObject : ScriptableObject
    {
        public uint ID;
        public uint InstanceID; // This is a unique ID for items with buffs so you can tell them apart since the ID field will be the same for all instances of an item.
        public Sprite Icon;
        public ItemTypes2 Type;

        [TextArea(15, 20)]
        public string Description;

        public ItemBuff[] Buffs;



        public Item CreateItem()
        {
            Item newItem = new Item(this);
            return newItem;
        }

    }

}