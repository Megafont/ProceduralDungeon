using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Objects
{

    [RequireComponent(typeof(BoxCollider2D))]
    public class Object_Spikes : MonoBehaviour
    {
        [SerializeField]
        private float _DamageAmount = 15f; // Amount of damage to deal on contact.

        Health _PlayerHealth;



        // Start is called before the first frame update
        void Start()
        {
            GameObject player = GameObject.Find("Player");
            _PlayerHealth = player.GetComponent<Health>();
        }

        void OnTriggerStay2D(Collider2D collision)
        {
            if (collision.tag == "Player")
                _PlayerHealth.DealDamage(_DamageAmount, DamageTypes.Spikes);
        }


    }

}
