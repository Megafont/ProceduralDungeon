using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.InGame.Items.Definitions;
using ProceduralDungeon.InGame.UI;
using ProceduralDungeon.TileMaps;
using ProceduralDungeon.Utilities;


namespace ProceduralDungeon.InGame.Items
{
    [ExecuteInEditMode]
    public class CollectableItem : MonoBehaviour
    {
        public ItemDefinition Item;
        public uint ItemCount;
        public ItemDatabaseObject ItemDatabase;



        public void Start()
        {
            Sprite sprite = ItemDatabase.LookupByID(Item.ID).Icon;
            GetComponent<SpriteRenderer>().sprite = sprite;
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (other.tag == "Player")
            {
                RoomSets roomSet = DungeonGenerator.DungeonTilemapManager.RoomSet;


                other.gameObject.GetComponent<Player>().Inventory.Data.AddItem(new ItemData(Item), ItemCount);

                GameObject obj = GameObject.Find("SpawnedObjects");
                GameObject uiObjectsParent = obj.transform.Find("UI").gameObject;

                GameObject popup = Instantiate(PrefabManager.GetUIPrefab("UI_CollectedItemPopup", roomSet),
                                                          transform.position + Vector3.up * 0.2f,
                                                          Quaternion.identity,
                                                          uiObjectsParent.transform);

                popup.GetComponent<UI_CollectedItemPopup>().SetItem(new Inventory.InventorySlot(new ItemData(Item), ItemCount), 
                                                                    roomSet);
                

                Destroy(gameObject);
            }
        }

    }


}
