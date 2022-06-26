using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items
{

    public class Object_Button : MonoBehaviour
    {
        public Sprite _ButtonSprite;
        public Sprite _ButtonPressedSprite;


        public bool IsPressed { get; private set; }


        BoxCollider2D _Collider;
        SpriteRenderer _Renderer;



        // Start is called before the first frame update
        void Start()
        {
            _Collider = GetComponent<BoxCollider2D>();
            _Renderer = GetComponent<SpriteRenderer>();

            _Renderer.sprite = _ButtonSprite;
        }



        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Player") ||
                collision.gameObject.layer == LayerMask.NameToLayer("Objects"))
            {
                CheckOverlappingColliders();
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            // Check that the collider that exited is one of the types that can trigger the button.
            // This way it can't be tricked into thinking its not pressed anymore.
            if (collision.gameObject.layer == LayerMask.NameToLayer("Player") ||
                collision.gameObject.layer == LayerMask.NameToLayer("Objects"))
            {
                CheckOverlappingColliders();
            }

        }


        /// <summary>
        /// This function checks if any valid object is pressing the button.
        /// </summary>
        /// <remarks>
        /// This function was added to fix a bug where if the player walked up to the edge of a button
        /// that had an ice block on it, the button would become unpressed when the player walked away.
        /// This was because the player got just close enough to cause the OnTriggerExit() method to get called.
        /// </remnarks>
        private void CheckOverlappingColliders()
        {
            LayerMask layerMask = (1 << LayerMask.NameToLayer("Player")) |
                                  (1 << LayerMask.NameToLayer("Enemies")) |
                                  (1 << LayerMask.NameToLayer("Objects"));
            
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(layerMask);
            filter.useLayerMask = true;

            // Get a filtered list of colliders that are overlapping the button's collider.
            List<Collider2D> colliders = new List<Collider2D>();
            int colliderCount = _Collider.OverlapCollider(filter, colliders);


            if (colliderCount > 0)
            {
                IsPressed = true;
                _Renderer.sprite = _ButtonPressedSprite;
            }
            else
            {
                IsPressed = false;
                _Renderer.sprite = _ButtonSprite;
            }

        }



    }


}
