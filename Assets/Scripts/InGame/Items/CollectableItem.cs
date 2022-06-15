using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items
{
    public class CollectableItem : MonoBehaviour
    {
        public ItemObject Item;


        public void OnTriggerEnter2D(Collider2D other)
        {
            if (other.tag == "Player")
            {
                other.gameObject.GetComponent<Player>().Inventory.Data.AddItem(new Item(Item), 1);
                Destroy(gameObject);
            }
        }

    }


}
