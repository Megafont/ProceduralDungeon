using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    [CreateAssetMenu(fileName = "New Food Equipment Object", menuName = "Inventory System/Item Definitions/Equipment")]
    public class ItemDefinition_Equipment : ItemDefinition
    {

        public void Awake()
        {
            Type = ItemTypes2.Equipment;
        }

    }

}