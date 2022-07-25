using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame
{

    public class GiveContactDamage : MonoBehaviour
    {
        public float DamageAmount = 10f;
        public DamageTypes DamageType = DamageTypes.Normal;

        public bool ColliderIsTrigger;



        void OnCollisionStay2D(Collision2D collision)
        {
            if (ColliderIsTrigger)
                return;


            if (collision.gameObject.CompareTag("Player"))
                collision.gameObject.GetComponent<Health>().DealDamage(DamageAmount, DamageType);

        }

        private void OnTriggerStay2D(Collider2D collision)
        {            
            if (!ColliderIsTrigger)
                return;


            if (collision.gameObject.CompareTag("Player"))
                collision.gameObject.GetComponent<Health>().DealDamage(DamageAmount, DamageType);
        }


    }

}
