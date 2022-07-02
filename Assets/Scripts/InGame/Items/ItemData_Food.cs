using System;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items.Definitions;


namespace ProceduralDungeon.InGame.Items
{
    [Serializable]
    public class ItemData_Food : ItemData
    {
        public float RecoveryAmount;


        /// <summary>
        /// This class represents an instance of an item in an inventory.
        /// </summary>
        /// <param name="item"></param>
        public ItemData_Food(ItemDefinition_Food foodItemDefinition)
            : base(foodItemDefinition as ItemDefinition)
        {
            RecoveryAmount = foodItemDefinition.RecoveryAmount;
        }


    }

}
