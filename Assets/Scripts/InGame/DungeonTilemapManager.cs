using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.InGame.Items;
using ProceduralDungeon.InGame.Enemies;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.InGame
{
    [ExecuteInEditMode]
    public class DungeonTilemapManager : MonoBehaviour
    {

        // *********************************************************************************************************************************************
        // * NOTE: Properties added to this class will not show up in the Unity Inspector until you add them to the DungeonTilemapManagerEditor class! *
        // *********************************************************************************************************************************************

        [SerializeField] private Tilemap _FloorsMap;
        [SerializeField] private Tilemap _WallsMap;
        [SerializeField] private Tilemap _Placeholders_Objects_Map;
        [SerializeField] private Tilemap _Placeholders_Items_Map;
        [SerializeField] private Tilemap _Placeholders_Enemies_Map;


        [SerializeField] private GameObject _Player;

        [SerializeField] private RoomSets _RoomSet; // Determines which folder a room will be saved/loaded to/from.
        [SerializeField] private ItemDatabaseObject _ItemDatabase;
        [SerializeField] private EnemySpawningData _EnemySpawningData;



        public GameObject Player { get { return _Player; } }
        public RoomSets RoomSet { get { return _RoomSet; } }

        public ItemDatabaseObject ItemDatabase { get { return _ItemDatabase;  } }
        public EnemySpawningData EnemySpawningData { get { return _EnemySpawningData; } }

        private DungeonMap _DungeonMap;



        void Awake()
        {
            Assert.IsNotNull(_FloorsMap, "DungeonTilemapManager.Awake() - The floors map field is null!");
            Assert.IsNotNull(_WallsMap, "DungeonTilemapManager.Awake() - The walls map field is null!");
            Assert.IsNotNull(_Placeholders_Objects_Map, "DungeonTilemapManager.Awake() - The object placeholders map field is null!");
            Assert.IsNotNull(_Placeholders_Items_Map, "DungeonTilemapManager.Awake() - The item placeholders map field is null!");
            Assert.IsNotNull(_Placeholders_Enemies_Map, "DungeonTilemapManager.Awake() - The enemy placeholders map field is null!");

            Assert.IsNotNull(_ItemDatabase, "DungeonTilemapManager.Awake() - The ItemDatabaseObject is null!");

            DungeonGenerator.Init(this);
        }



        // Start is called before the first frame update
        void Start()
        {
            DungeonGenerator.GenerateDungeon();

        }


        void OnDrawGizmos()
        {
            if (DungeonGenerator.IsInitialized)
            {
                DungeonGizmos.DrawDungeonGizmos();
                MissionStructureGraphGizmos.DrawDungeonGraphGizmos();
            }
        }


        public void PositionPlayerByStartDoor(Vector3 startDoorPos, Directions startDoorDirection)
        {
            Vector3 playerPos = startDoorPos;


            // Shift the player position so he appears properly inside the door.
            // The passed in position is the lower-left corner of the upper-left-most of the two door tiles.
            if (startDoorDirection == Directions.North)
                playerPos += new Vector3(1, -1, 0);
            else if (startDoorDirection == Directions.East)
                playerPos += Vector3.left;
            else if (startDoorDirection == Directions.South)
                playerPos += new Vector3(1, 2, 0);
            else if (startDoorDirection == Directions.West)
                playerPos += new Vector3(2, 0, 0);


            _Player.transform.position = playerPos;
        }

        private void InitDungeonMap()
        {
            _DungeonMap = new DungeonMap(_FloorsMap, _WallsMap, _Placeholders_Objects_Map, _Placeholders_Items_Map, _Placeholders_Enemies_Map);
        }

        public DungeonMap DungeonMap
        {
            get
            {
                if (_DungeonMap != null)
                    return _DungeonMap;

                InitDungeonMap();
                return _DungeonMap;
            }
        }


    }

}