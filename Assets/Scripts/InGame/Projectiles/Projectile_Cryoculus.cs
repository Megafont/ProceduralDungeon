using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.Utilities;


namespace ProceduralDungeon.InGame.Projectiles
{

    public class Projectile_Cryoculus : ProjectileBase
    {

        private void Start()
        {
            DamageType = DamageTypes.Projectile;
        }

        private void OnEnable()
        {
            DungeonGraphNode parentRoom = DungeonGenerator.LookupRoomFromTile(Vector3Int.FloorToInt(gameObject.transform.parent.position));

            GetComponent<SpriteRenderer>().sprite = SpriteManager.GetSprite("Projectile_Cryoculus", parentRoom.RoomBlueprint.RoomSet);
        }


        protected override void OnProjectileCollision(GameObject sender, Collision2D collidedWith)
        {
            if (collidedWith.gameObject.tag == "Player")
            {
                ProjectileUtils.DamagePlayer(DamageAmount, DamageType);
            }


            gameObject.SetActive(false);
        }


    }

}
