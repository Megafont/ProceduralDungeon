using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items
{
    public enum ItemTypes
    {
        Item_Key = 0,
    }

    public abstract class Collectable : MonoBehaviour
    {
        protected GameObject _Player;



        private void Start()
        {
            _Player = GameObject.FindGameObjectWithTag("Player");
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.tag == "Player")
                OnCollected();
        }



        public void DestroyCollectable()
        {
            Destroy(this.gameObject);
        }



        protected virtual void OnCollected()
        {

        }


    }

}