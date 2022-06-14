using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items
{
    [CreateAssetMenu(fileName = "New Default Item Object", menuName = "Inventory System/Items/Default")]
    public class ItemObject_Default : ItemObject
    {
        public void Awake()
        {
            Type = ItemTypes2.Default;
        }
    }

}