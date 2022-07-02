using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    [CreateAssetMenu(fileName = "New Key Item Object", menuName = "Inventory System/Item Definitions/Key")]
    public class ItemDefinition_Key : ItemDefinition, IItemDefinition
    {
        public KeyTypes KeyType;
        public uint KeyID;



        public new void Awake()
        {
            Type = ItemTypes.Key;
        }


        public override ItemData CreateItemInstance()
        {
            ItemData_Key newItem = new ItemData_Key(this);
            return newItem;
        }

    }

}