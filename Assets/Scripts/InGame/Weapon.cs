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


        private void OnTriggerEnter2D(Collider2D collision)
        {

            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemies"))
            {
                if (WeaponItem == null)
                {
                    Debug.LogError("Weapon.OnTriggerEnter2D() - Cannot deal damage because no weapon item is equipped!");
                    return;
                }


                // NOTE: This can allow for some enemies to be weaker or not aganist certain types of damage by
                //       using different weapon attributes than Strength, such as maybe FireDamage. The Health class
                //       could also be extended to allow equipping certain equipment items on it to add resistances
                //       to certain types of damage.
                collision.gameObject.GetComponent<Health>().DealDamage(WeaponItem.Buffs[ItemAttributes.Strength], 
                                                                       DamageTypes.Weapon);
            }

        }


    }

}