using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items
{
    [CreateAssetMenu(fileName = "New Food Item Object", menuName = "Inventory System/Items/Food")]
    public class ItemObject_Food : ItemObject
    {

        public void Awake()
        {
            Type = ItemTypes2.Food;
        }
    }

}