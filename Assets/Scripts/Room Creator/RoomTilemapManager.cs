using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

using ProceduralDungeon.DungeonGeneration.DungeonConstruction.PlaceholderUtilities;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.RoomCreator
{

    [ExecuteInEditMode]
    public class RoomTilemapManager : MonoBehaviour
    {

        // *********************************************************************************************************************************************
        // * NOTE: Properties added to this class will not show up in the Unity Inspector until you add them to the RoomTilemapManagerEditor class!    *
        // *********************************************************************************************************************************************

        [SerializeField] private Tilemap _FloorsMap;
        [SerializeField] private Tilemap _WallsMap;
        [SerializeField] private Tilemap _Placeholders_General_Map; // Holds special placeholder tiles used by the dungeon generator to tell it where it should place certain types of objects.
        [SerializeField] private Tilemap _Placeholders_Items_Map;
        [SerializeField] private Tilemap _Placeholders_Enemies_Map;


        [SerializeField] private RoomLevels _RoomLevel = RoomLevels.Level_1stFloor;
        [SerializeField] private string _RoomName = "New Room";

        // Determines which folder a room will be saved/loaded to/from.
        [SerializeField] private RoomSets _RoomSet;

        [SerializeField] private RoomTypeFlags _RoomTypeFlags;



        private DungeonMap _RoomMap;



        public string RoomName { get { return _RoomName; } set { _RoomName = value; } }
        public string RoomSet { get { return Enum.GetName(typeof(RoomSets), _RoomSet); } }



        void Awake()
        {
            Assert.IsNotNull(_FloorsMap, "RoomTilemapManager: The floors map field is null!");
            Assert.IsNotNull(_WallsMap, "RoomTilemapManager: The walls map field is null!");
            Assert.IsNotNull(_Placeholders_General_Map, "RoomTilemapManager: The placeholders map field is null!");
            Assert.IsNotNull(_Placeholders_Items_Map, "RoomTilemapManager: The items map field is null!");
            Assert.IsNotNull(_Placeholders_Enemies_Map, "RoomTilemapManager: The enemies map field is null!");
        }



        public enum SaveRoomReturnCodes
        {
            Success = 0,
            Error_InvalidTiles = 1,
            Error_InvalidPlaceholders = 2,
        }
        public SaveRoomReturnCodes SaveRoom()
        {
            ScriptableRoom newRoom = ScriptableObject.CreateInstance<ScriptableRoom>();

            newRoom.RoomLevel = _RoomLevel;
            newRoom.RoomName = _RoomName;
            newRoom.RoomTypeFlags = _RoomTypeFlags;



            if (RoomMap.GetSaveDataFromTileMaps(newRoom))
            {
                // Validate placeholders.
                if (!(PlaceholderUtils_Doors.FindAndValidateRoomDoors(newRoom.Placeholders_General_Tiles, newRoom.FloorTiles)))
                {
                    return SaveRoomReturnCodes.Error_InvalidPlaceholders;
                }


                ScriptableRoomEditorUtilities.SaveRoomAsset(newRoom, RoomSet);


                return SaveRoomReturnCodes.Success;
            }
            else
            {
                return SaveRoomReturnCodes.Error_InvalidTiles;
            }

        }



        public enum LoadRoomReturnCodes
        {
            Success = 0,
            Error_FileNotFound = 1,
            Error_InvalidTiles = 2,
        }
        public LoadRoomReturnCodes LoadRoom(string roomToLoad)
        {
            string filePath = ScriptableRoomUtilities.GetRoomFileLoadPath(roomToLoad, RoomSet);
            ScriptableRoom loadedRoom = Resources.Load<ScriptableRoom>(filePath);
            if (loadedRoom == null)
            {
                Debug.LogError($"Could not load room asset \"{filePath}\"! The file does not exist or something else went wrong.");
                return LoadRoomReturnCodes.Error_FileNotFound;
            }


            if (RoomMap.FillTileMapsWithTileData(loadedRoom))
            {
                _RoomLevel = loadedRoom.RoomLevel;
                _RoomName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                _RoomTypeFlags = loadedRoom.RoomTypeFlags;
                return LoadRoomReturnCodes.Success;
            }
            else
            {
                return LoadRoomReturnCodes.Error_InvalidTiles;
            }

        }


        private void InitRoomMap()
        {
            _RoomMap = new DungeonMap(_FloorsMap, _WallsMap, _Placeholders_General_Map, _Placeholders_Items_Map, _Placeholders_Enemies_Map);
        }


        public DungeonMap RoomMap
        {
            get
            {
                if (_RoomMap != null)
                    return _RoomMap;

                InitRoomMap();
                return _RoomMap;
            }
        }

    }


}