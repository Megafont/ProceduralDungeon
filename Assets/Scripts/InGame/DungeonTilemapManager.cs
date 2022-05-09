using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.InGame
{
    [ExecuteInEditMode]
    public class DungeonTilemapManager : MonoBehaviour
    {
        [SerializeField] private Tilemap _FloorsMap;
        [SerializeField] private Tilemap _WallsMap;
        [SerializeField] private Tilemap _Placeholders_General_Map; // Holds special placeholder tiles used by the dungeon generator to tell it where it should place certain types of objects.
        [SerializeField] private Tilemap _Placeholders_Items_Map;
        [SerializeField] private Tilemap _Placeholders_Enemies_Map;


        [Header("Dungeon Generation Parameters")]

        // Determines which folder a room will be saved/loaded to/from.
        [SerializeField] public RoomSets _RoomSet;

        [SerializeField] public int _MaxRooms = 20;



        public string RoomSet { get { return Enum.GetName(typeof(RoomSets), _RoomSet); } }


        private DungeonMap _DungeonMap;



        void Awake()
        {
            Assert.IsNotNull(_FloorsMap, "DungeonManager: The floors map field is null!");
            Assert.IsNotNull(_WallsMap, "DungeonManager: The walls map field is null!");
            Assert.IsNotNull(_Placeholders_General_Map, "DungeonManager: The placeholders map field is null!");
            Assert.IsNotNull(_Placeholders_Items_Map, "DungeonManager: The items map field is null!");
            Assert.IsNotNull(_Placeholders_Enemies_Map, "DungeonManager: The enemies map field is null!");
        }



        // Start is called before the first frame update
        void Start()
        {
            DungeonGenerator.Init(this);
            DungeonGenerator.GenerateDungeon();

        }


        void OnDrawGizmos()
        {
            if (DungeonGenerator.IsInitialized)
                DungeonGizmos.DrawDungeonGizmos();
        }


        private void InitDungeonMap()
        {
            _DungeonMap = new DungeonMap(_FloorsMap, _WallsMap, _Placeholders_General_Map, _Placeholders_Items_Map, _Placeholders_Enemies_Map);
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