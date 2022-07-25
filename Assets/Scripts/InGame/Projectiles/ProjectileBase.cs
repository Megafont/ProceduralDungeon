using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Projectiles
{

    public abstract class ProjectileBase : MonoBehaviour
    {
        public float DamageAmount = 5;
        public DamageTypes DamageType = DamageTypes.Projectile;



        public delegate void OnCollisionHandler(GameObject sender, Collision2D collidedWith);

        public event OnCollisionHandler OnCollision;



        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Call the method to tell Projectile sub classes that this projectile has collided with something.
            OnProjectileCollision(gameObject, collision);

            // Fire the event for the object that owns the projectile.
            OnCollision?.Invoke(gameObject, collision);
        }


        protected abstract void OnProjectileCollision(GameObject sender, Collision2D collidedWith);


    }

}
