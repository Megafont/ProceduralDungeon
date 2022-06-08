using System.Collections;
using System.Collections.Generic;

using UnityEngine;


//[RequireComponent(typeof(BoxCollider2D))]
public class Object_Spikes : MonoBehaviour
{
    [SerializeField]
    private float _DamageDelay = 1.0f; // Time before damage is dealt again if still in contact with the spikes.
    [SerializeField]
    private float _DamageAmount = 15f; // Amount of damage to deal on contact.

    Health _PlayerHealth;
    float _TimeSinceLastDamage = 0;



    // Start is called before the first frame update
    void Start()
    {
        GameObject player = GameObject.Find("Player");
        _PlayerHealth = player.GetComponent<Health>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        _TimeSinceLastDamage = 0;
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Player" && _TimeSinceLastDamage >= _DamageDelay)
        {
            _PlayerHealth.DealDamage(_DamageAmount);
            _TimeSinceLastDamage = 0;
        }

        _TimeSinceLastDamage += Time.deltaTime;
    }


}
