using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame;


namespace ProceduralDungeon.InGame.Objects
{

    [RequireComponent(typeof(Inventory))]
    public class Object_Chest : MonoBehaviour
    {
        [SerializeField]
        public Sprite ClosedSprite;
        [SerializeField]
        public Sprite OpenSprite;



        private Inventory _Inventory;
        private SpriteRenderer _SpriteRenderer;


        private static GameObject _Player;



        public Inventory Inventory { get { return Inventory; } }



        public Object_Chest()
        {

        }



        private void Start()
        {
            if (_Player == null)
                _Player = GameObject.FindGameObjectWithTag("Player");

            if (_SpriteRenderer == null)
                _SpriteRenderer = GetComponent<SpriteRenderer>();

            _SpriteRenderer.sprite = ClosedSprite;

            _Inventory = GetComponent<Inventory>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.tag == "Player" && _SpriteRenderer.sprite == ClosedSprite)
            {
                Inventory playerInventory = _Player.GetComponent<Inventory>();

                playerInventory.InsertItems(_Inventory);
                _Inventory.Clear();

                _SpriteRenderer.sprite = OpenSprite;
            }


        }


    }

}