using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.InGame;


namespace ProceduralDungeon.InGame.Objects
{

    public class Object_Door_BombableWall : MonoBehaviour
    {
        [SerializeField]
        public DungeonDoor Doorway; // The doorway this entity represents.


        private DungeonTilemapManager _TilemapManager;



        // Start is called before the first frame update
        void Start()
        {
            GameObject obj = GameObject.Find("Dungeon Manager");
            _TilemapManager = obj.GetComponent<DungeonTilemapManager>();
        }



        public void OpenBombWall()
        {
            DungeonConstructionUtils.PlaceBombableWallDoor(_TilemapManager, Doorway, true);

            // Disable the box collider of this wall that we used to detect a bomb explosion colliding with it.
            // Otherwise it will block the player.
            GetComponent<BoxCollider2D>().enabled = false;
        }


    }

}
