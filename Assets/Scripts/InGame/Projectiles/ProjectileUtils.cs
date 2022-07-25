using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame;


namespace ProceduralDungeon.InGame.Projectiles
{
    public static class ProjectileUtils
    {
        private static GameObject _Player;


        public static void DamagePlayer(float damageAmount, DamageTypes damageType)
        {
            if (_Player == null)
                _Player = GameObject.Find("Player");

            _Player.GetComponent<Health>().DealDamage(damageAmount, damageType);
        }

    }


}
