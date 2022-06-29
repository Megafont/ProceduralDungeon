using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    public enum ItemTypes
    {
        Default,
        Food,
        Equipment,
        Key,
    }

    public enum ItemAttributes
    {
        Agility,
        Intellect,
        Stamina,
        Strength,
    }



    /// <summary>
    /// This is the abstract base class for our item system. It is the definition or blueprint of an item object.
    /// I learned how to make this item/inventory system from the video series by Coding With Unity:
    /// https://www.youtube.com/watch?v=_IqTeruf3-s&list=PLJWSdH2kAe_Ij7d7ZFR2NIW8QCJE74CyT
    /// </summary>
    public abstract class ItemDefinitionBase : ScriptableObject
    {
        public uint ID;
        public uint InstanceID; // This is a unique ID for items with buffs so you can tell them apart since the ID field will be the same for all instances of an item.
        public Sprite Icon;
        public ItemTypes Type;

        [TextArea(15, 20)]
        public string Description;

        public ItemBuff[] Buffs;



        public ItemData CreateItem()
        {
            ItemData newItem = new ItemData(this);
            return newItem;
        }



        /// <summary>
        /// This is a compare method used for sorting lists of ItemDefinitions.
        /// </summary>
        /// <param name="x">The first of the two ItemDefinitions to compare.</param>
        /// <param name="y">The second of the two ItemDefinitions to compare.</param>
        /// <returns>A negative value if x < y, 0 if the two are considered equal, and a positive value if x > y.</y></returns>
        public static int CompareByName(ItemDefinitionBase x, ItemDefinitionBase y)
        {
            return x.name.CompareTo(y.name);
        }


    }

}