using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.InGame.Inventory;
using ProceduralDungeon.InGame.UI;
using ProceduralDungeon.Utilities;


namespace ProceduralDungeon.InGame.Objects
{
    public enum ChestTypes
    {
        Key,
        Key_Multipart,
        Key_Goal,
        RandomTreasure,
    }



    public class Object_Chest : MonoBehaviour
    {
        [SerializeField]
        public Sprite ClosedSprite;
        [SerializeField]
        public Sprite OpenSprite;
        
        [SerializeField]
        [Range(0f, 5f)]
        public float CollectedItemPopupDelay = 0.8f;

        [SerializeField]
        private InventoryObject _Inventory;



        public DungeonGraphNode ParentRoom;


        private SpriteRenderer _SpriteRenderer;

        private static GameObject _UI_Objects_Parent;
        private static GameObject _Player;



        public InventoryObject Inventory { get { return _Inventory; } } 



        private void Awake()
        {
            _Inventory ??= ScriptableObject.CreateInstance<InventoryObject>();

            _Player ??= GameObject.FindGameObjectWithTag("Player");

            _SpriteRenderer ??= GetComponent<SpriteRenderer>();


            GameObject obj = GameObject.Find("SpawnedObjects");
            _UI_Objects_Parent = obj.transform.Find("UI").gameObject;


            _SpriteRenderer.sprite = ClosedSprite;

            _Inventory = ScriptableObject.CreateInstance<InventoryObject>();

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.tag == "Player" && _SpriteRenderer.sprite == ClosedSprite)
            {
                _SpriteRenderer.sprite = OpenSprite;

                InventoryObject playerInventory = _Player.GetComponent<Player>().Inventory;

                playerInventory.Data.AddItems(_Inventory);

                StartCoroutine(ShowCollectedItemPopups());
            }
        }

        private IEnumerator ShowCollectedItemPopups()
        {
            for (int i = 0; i < _Inventory.Data.Items.Count; i++)
            {
                InventorySlot slot = _Inventory.Data.Items[i];

                GameObject popup = Instantiate(PrefabManager.GetPrefab("UI_CollectedItemPopup", ParentRoom.RoomBlueprint.RoomSet),
                                               transform.position + Vector3.up * 0.2f,
                                               Quaternion.identity,
                                               _UI_Objects_Parent.transform); ;

                popup.GetComponent<UI_CollectedItemPopup>().SetItem(slot, ParentRoom.RoomBlueprint.RoomSet);

                yield return new WaitForSeconds(CollectedItemPopupDelay);

            }


            _Inventory.Clear();

        }


    }

}