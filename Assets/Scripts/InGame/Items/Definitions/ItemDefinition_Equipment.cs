using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    [CreateAssetMenu(fileName = "New Food Equipment Object", menuName = "Inventory System/Item Definitions/Equipment")]
    public class ItemDefinition_Equipment : ItemDefinitionBase
    {

        public void Awake()
        {
            Type = ItemTypes.Equipment;
        }

    }

}