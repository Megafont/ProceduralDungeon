using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame
{
    public class TakeWeaponDamage : MonoBehaviour
    {
        private Health _Health;



        // Start is called before the first frame update
        void Start()
        {
            _Health = GetComponent<Health>();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Weapons"))
                _Health.DealDamage(10f, DamageTypes.Weapon);

        }


    }

}
