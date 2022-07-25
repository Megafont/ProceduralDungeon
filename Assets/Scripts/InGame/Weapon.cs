using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items;
using ProceduralDungeon.InGame.Items.Definitions;


namespace ProceduralDungeon.InGame
{ 
    public class Weapon : MonoBehaviour
    {
        public ItemDataWithBuffs WeaponItem;


        private CharacterStats _OwnerStats;


        private void Start()
        {
            if (gameObject.transform.parent.name == "Player")
                _OwnerStats = GameObject.Find("Player").GetComponent<Player>().CharacterStats;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {

            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemies"))
            {
                if (WeaponItem == null)
                {
                    Debug.LogWarning("Weapon.OnTriggerEnter2D() - Cannot deal damage because no weapon item is equipped!");
                    return;
                }


                Health health = collision.gameObject.GetComponent<Health>();
                if (health == null)
                {
                    Debug.LogWarning($"Weapon.OnTriggerEnter2D() - Cannot deal damage because the object hit (\"{collision.name}\") does not have a health component!");
                    return;
                }


                // NOTE: This can allow for some enemies to be weaker or not aganist certain types of damage by
                //       using different weapon attributes than Strength, such as maybe FireDamage. The Health class
                //       could also be extended to allow equipping certain equipment items on it to add resistances
                //       to certain types of damage.
                health.DealDamage(_OwnerStats.Attack + WeaponItem.Buffs[ItemAttributes.Attack],
                                  DamageTypes.Weapon);

            }

        }


    }

}