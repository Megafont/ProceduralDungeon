using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;


namespace ProceduralDungeon.InGame.Objects
{

    /// <summary>
    /// A button to push an object onto or step on.
    /// </summary>
    /// <remarks>
    /// The collider of the button is smaller than the button sprite because otherwise if an ice block or player is
    /// on a diagonally adjacent tile, it will still trigger the OnTriggerEnter2D() method on the button.
    /// </remarks>
    public class Object_Button : MonoBehaviour
    {
        public Sprite _ButtonSprite;
        public Sprite _ButtonPressedSprite;

        public bool InvertButtonState;

        private bool _IsPressed;



        public bool IsPressed { get { return !InvertButtonState ? _IsPressed : !IsPressed; } }
                               

        public List<GameObject> ObjectsOnButton { get; private set; }


        private BoxCollider2D _Collider;
        private SpriteRenderer _Renderer;

        private ContactFilter2D _ContactFilter;
        


        // Start is called before the first frame update
        void Start()
        {
            _Collider = GetComponent<BoxCollider2D>();
            _Renderer = GetComponent<SpriteRenderer>();

            ObjectsOnButton = new List<GameObject>();
            
            _Renderer.sprite = _ButtonSprite;


            // Setup a contact filter.
            LayerMask layerMask = (1 << LayerMask.NameToLayer("Player")) |
                                  (1 << LayerMask.NameToLayer("Enemies")) |
                                  (1 << LayerMask.NameToLayer("Objects"));
            _ContactFilter = new ContactFilter2D();
            _ContactFilter.SetLayerMask(layerMask);
            _ContactFilter.useLayerMask = true;
        }



        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Simply return if this button's Start() method hasn't been called yet.
            if (ObjectsOnButton == null)
                return;

            
            CheckOverlappingColliders();
            ObjectsOnButton.Add(collision.gameObject);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            // Simply return if this button's Start() method hasn't been called yet.
            if (ObjectsOnButton == null)
                return;


            CheckOverlappingColliders();
            ObjectsOnButton.Remove(collision.gameObject);

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
            // Simply return if this button's Start() method hasn't been called yet.
            if (_Collider == null)
                return;


            // Get a filtered list of colliders that are overlapping the button's collider.
            List<Collider2D> colliders = new List<Collider2D>();
            int colliderCount = _Collider.OverlapCollider(_ContactFilter, colliders);


            if (colliderCount > 0)
            {
                _IsPressed = true;
                _Renderer.sprite = _ButtonPressedSprite;
            }
            else
            {
                _IsPressed = false;
                _Renderer.sprite = _ButtonSprite;
            }


            InGameUtils.CheckRoomPuzzleState(DungeonGenerator.LookupRoomFromTile(Vector3Int.FloorToInt(transform.position)));

        }



    }


}
