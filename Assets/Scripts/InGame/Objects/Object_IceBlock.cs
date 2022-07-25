using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Objects
{

    public class Object_IceBlock : MonoBehaviour
    {
        public float PushSpeed = 4.0f;


        private Rigidbody2D _Rigidbody;

        private Vector3 _OriginalSpawnPosition = Vector3.zero;



        // Start is called before the first frame update
        void Start()
        {
            _Rigidbody = GetComponent<Rigidbody2D>();

            _OriginalSpawnPosition = transform.position;
        }

        
        void Update()
        {
            Vector2 velocityNormalized = _Rigidbody.velocity.normalized;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, velocityNormalized, 0.55f);
            if (hit.collider != null && !hit.collider.gameObject.name.StartsWith("Object_Button"))
            {
                _Rigidbody.velocity = Vector2.zero;

                // Snap the block to the nearest grib position to ensure it lines up properly when it hits something. That way it looks nice when you push it onto a button.
                transform.position = new Vector3((int) Mathf.Floor(transform.position.x) + 0.5f, 
                                                 (int) Mathf.Floor(transform.position.y) + 0.5f, 
                                                 (int) Mathf.Floor(transform.position.z));
            }
        }


        void OnCollisionEnter2D(Collision2D collision)
        {
            ContactPoint2D hitPoint = collision.GetContact(0);            
            
            //Debug.Log("COLLISION! " + hitPoint.point + "   " + hitPoint.normal + "    " + collision.collider.ClosestPoint(hitPoint.point));
            if (collision.gameObject.CompareTag("Player"))
            {
                // Get the center point of the player's collider.
                // This originally used collision.GetContact() to get the contact point, but the player has a capsul collider, so sometimes
                // the rounded edges cause the coordinates of that point to be inside the width/height of the ice block's
                // collider, but off to one side. So I switched to using the player collider's center point.  
                Vector3 playerCenterPoint = collision.collider.bounds.center;
                
                // Get the center point of the ice block's collider.
                Vector3 centerPoint = collision.otherCollider.bounds.center;
                
                // Get the width and height of the ice block's collider.
                float rectWidth = collision.otherCollider.bounds.size.x;
                float rectHeight = collision.otherCollider.bounds.size.y;


                if (playerCenterPoint.y > centerPoint.y &&
                    (playerCenterPoint.x < centerPoint.x + rectWidth / 2 && playerCenterPoint.x > centerPoint.x - rectWidth / 2))
                {                    
                    // Player collided with top of ice block, so move it downward.
                    _Rigidbody.velocity = Vector2.down * PushSpeed;
                }
                else if (playerCenterPoint.y < centerPoint.y &&
                         (playerCenterPoint.x < centerPoint.x + rectWidth / 2 && playerCenterPoint.x > centerPoint.x - rectWidth / 2))
                {
                    // Player collided with bottom of ice block, so move it downward.
                    _Rigidbody.velocity = Vector2.up * PushSpeed;
                }
                else if (playerCenterPoint.x > centerPoint.x &&
                         (playerCenterPoint.y < centerPoint.y + rectHeight / 2 && playerCenterPoint.y > centerPoint.y - rectHeight / 2))
                {
                    // Player collided with right side of ice block, so move it downward.
                    _Rigidbody.velocity = Vector2.left * PushSpeed;
                }
                else if (playerCenterPoint.x < centerPoint.x &&
                         (playerCenterPoint.y < centerPoint.y + rectHeight / 2 && playerCenterPoint.y > centerPoint.y - rectHeight / 2))
                {
                    // Player collided with left side of ice block, so move it downward.
                    _Rigidbody.velocity = Vector2.right * PushSpeed;
                }

            }

        }



        public void ResetToSpawnPosition()
        {
            transform.position = _OriginalSpawnPosition;
        }


    }


}
