using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    [CreateAssetMenu(fileName = "New Food Item Object", menuName = "Inventory System/Item Definitions/Food")]
    public class ItemDefinition_Food : ItemDefinition, IItemDefinition
    {
        public float RecoveryAmount;



        public new void Awake()
        {
            Type = ItemTypes.Food;
        }


        public override ItemData CreateItemInstance()
        {
            ItemData_Food newItem = new ItemData_Food(this);
            return newItem;
        }


    }

}