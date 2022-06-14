using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items
{
    [CreateAssetMenu(fileName = "New Food Equipment Object", menuName = "Inventory System/Items/Equipment")]
    public class ItemObject_Equipment : ItemObject
    {

        public void Awake()
        {
            Type = ItemTypes2.Equipment;
        }

    }

}