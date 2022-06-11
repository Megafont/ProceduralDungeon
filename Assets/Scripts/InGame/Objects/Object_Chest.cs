using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.InGame.UI;


namespace ProceduralDungeon.InGame.Objects
{

    [RequireComponent(typeof(Inventory))]
    public class Object_Chest : MonoBehaviour
    {
        [SerializeField]
        public Sprite ClosedSprite;
        [SerializeField]
        public Sprite OpenSprite;
        
        [SerializeField]
        [Range(0f, 5f)]
        public float CollectedItemPopupDelay = 0.8f;


        public DungeonGraphNode ParentRoom;


        private UI_CollectedItemPopup _Prefab_UI_CollectedItemPopup;


        private Inventory _Inventory;
        private SpriteRenderer _SpriteRenderer;

        private static GameObject _UI_Objects_Parent;

        private static GameObject _Player;



        public Inventory Inventory { get { return Inventory; } }



        private void Start()
        {
            if (_Player == null)
                _Player = GameObject.FindGameObjectWithTag("Player");

            if (_SpriteRenderer == null)
                _SpriteRenderer = GetComponent<SpriteRenderer>();


            GameObject obj = GameObject.Find("SpawnedObjects");
            _UI_Objects_Parent = obj.transform.Find("UI").gameObject;


            _SpriteRenderer.sprite = ClosedSprite;

            _Inventory = GetComponent<Inventory>();


            if (_Prefab_UI_CollectedItemPopup == null)
            {
                GameObject prefab = (GameObject)Resources.Load("Prefabs/UI/UI_CollectedItemPopup");
                _Prefab_UI_CollectedItemPopup = prefab.GetComponent<UI_CollectedItemPopup>();
            }

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.tag == "Player" && _SpriteRenderer.sprite == ClosedSprite)
            {
                _SpriteRenderer.sprite = OpenSprite;

                Inventory playerInventory = _Player.GetComponent<Inventory>();

                playerInventory.InsertItems(_Inventory);

                StartCoroutine(ShowCollectedItemPopups());
            }
        }

        private IEnumerator ShowCollectedItemPopups()
        {
            for (int i = 0; i < _Inventory.GetItemDataEntryCount(); i++)
            {
                ItemData itemData = _Inventory.GetItemDataEntry((uint) i);

                UI_CollectedItemPopup popup = Instantiate(_Prefab_UI_CollectedItemPopup, 
                                                          transform.position + Vector3.up * 0.2f, 
                                                          Quaternion.identity, 
                                                          _UI_Objects_Parent.transform);

                popup.SetItemType(itemData, ParentRoom.RoomBlueprint.RoomSet);

                yield return new WaitForSeconds(CollectedItemPopupDelay);

            }


            _Inventory.Clear();

        }


    }

}