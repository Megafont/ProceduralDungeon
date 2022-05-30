using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
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
        [SerializeField] private Tilemap _Placeholders_General_Map; // Holds special placeholder tiles used by the dungeon generator to tell it where it should place certain types of objects.
        [SerializeField] private Tilemap _Placeholders_Items_Map;
        [SerializeField] private Tilemap _Placeholders_Enemies_Map;


        [SerializeField] private GameObject _Player;

        [SerializeField] private RoomSets _RoomSet; // Determines which folder a room will be saved/loaded to/from.


        public GameObject Player { get { return _Player; } }
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