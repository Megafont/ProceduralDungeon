using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    [CreateAssetMenu(fileName = "New Equipment Object", menuName = "Inventory System/Item Definitions/Equipment")]
    public class ItemDefinition_Equipment : ItemWithBuffsDefinition, IItemDefinition
    {

        public new void Awake()
        {
            Type = ItemTypes.Equipment;
        }

        public override ItemData CreateItemInstance()
        {
            ItemDataWithBuffs newItem = new ItemDataWithBuffs(this);
            return newItem;
        }

    }

}