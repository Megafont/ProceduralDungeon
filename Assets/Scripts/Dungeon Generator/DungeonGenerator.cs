
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using ToolboxLib_Shared.Math;

using ProceduralDungeon.DungeonGeneration.Utilities;
using ProceduralDungeon.DungeonGeneration.Utilities.PlaceholderUtilities;
using ProceduralDungeon.InGame;
using ProceduralDungeon.TileMaps;



namespace ProceduralDungeon.DungeonGeneration
{
    public static class DungeonGenerator
    {
        private static DungeonGraph _DungeonGraph;

        private static DungeonTilemapManager _DungeonTilemapManager;

        private static bool _IsInitialized = false;


        // References to room prefabs.
        private static List<RoomData> _AllRooms;
        private static List<RoomData> _RoomsWithNorthDoor;
        private static List<RoomData> _RoomsWithEastDoor;
        private static List<RoomData> _RoomsWithSouthDoor;
        private static List<RoomData> _RoomsWithWestDoor;



        private static NoiseRNG _SeedRNG = null;
        private static NoiseRNG _RNG_Room = null;
        private static NoiseRNG _RNG_Rotation = null;



        public static DungeonGraph DungeonGraph { get { return _DungeonGraph; } }
        public static bool IsInitialized { get { return _IsInitialized; } }



        public static void Init(DungeonTilemapManager manager)
        {
            if (manager == null)
            {
                Debug.LogError("DungeonGenerator.Init() - Cannot initialize the dungeon generator, because the passed in DungeonGenerator is null!");
                return;
            }


            if (_IsInitialized)
                ClearPreviousData();


            _DungeonTilemapManager = manager;


            // Move these lists into a theme object later that holds all rooms from a certain set?
            // This would be necessary if I make this generator able to incorporate multiple themes into one dungeon (like cave and brick rooms or something);
            _AllRooms = new List<RoomData>();
            _RoomsWithNorthDoor = new List<RoomData>();
            _RoomsWithEastDoor = new List<RoomData>();
            _RoomsWithSouthDoor = new List<RoomData>();
            _RoomsWithWestDoor = new List<RoomData>();

            LoadRoomsData();
            PreprocessRoomData();

            _IsInitialized = true;
        }



        public static void GenerateDungeon()
        {
            _DungeonTilemapManager.DungeonMap.ClearAllTileMaps();


            // Init the random number generators.
            InitRNG();

            // Select starting room.
            int index = _AllRooms.Count - 2; // SelectRandomRoom();
            int index2 = _AllRooms.Count - 2;

            // Create a new DungeonGraph with a node for the starting room.
            _DungeonGraph = new DungeonGraph(new DungeonGraphNode(_AllRooms[index], new Vector3Int(0, 0), Directions.West, 0));

            /*
            Vector3Int roomDoorPos = _DungeonGraph.StartNode.RoomBlueprint.DoorsList[0].Tile1Position;
            Vector3Int adjustedRoom1DoorPos = DungeonConstructionUtils.AdjustTileCoordsForRoomRotationAndPosition(roomDoorPos, Vector3Int.zero, _DungeonGraph.StartNode.Direction);


            Vector3Int room2DoorPos = roomDoorPos + Vector3Int.up; // Shift up one unit to get the position where the neighboring room's door will be.
            Vector3Int adjustedRoom2DoorPos = DungeonConstructionUtils.AdjustTileCoordsForRoomRotationAndPosition(roomDoorPos, Vector3Int.zero, Directions.South);
            Vector3Int room2Pos = room2DoorPos + -adjustedRoom2DoorPos;
            room2Pos += Vector3Int.right; // Move right one to fix the position when the connected room is facing south (similar to the code in DrawDoorGizmos()).
            DungeonGraphNode room2 = _DungeonGraph.AddNode(new DungeonGraphNode(_AllRooms[index], room2Pos, MiscellaneousUtils.FlipDirection(Directions.North), 1), _DungeonGraph.StartNode);
            */


            DungeonGraphNode newRoom = _DungeonGraph.GenerateNewRoomAndConnectToPrevious(_DungeonGraph.StartNode,
                                                                                         _DungeonGraph.StartNode.RoomBlueprint.DoorsList[0],
                                                                                         _AllRooms[index2],
                                                                                         _AllRooms[index2].DoorsList[0]);

            // Place the starting room.            
            DungeonConstructionUtils.PlaceRoom(_DungeonTilemapManager, _DungeonGraph.StartNode);
            DungeonConstructionUtils.PlaceRoom(_DungeonTilemapManager, newRoom);
        }



        private static void ClearPreviousData()
        {
            _AllRooms.Clear();

            _RoomsWithNorthDoor.Clear();
            _RoomsWithEastDoor.Clear();
            _RoomsWithSouthDoor.Clear();
            _RoomsWithWestDoor.Clear();
        }

        private static int SelectRandomRoom()
        {
            return _RNG_Room.RollRandomIntInRange(0, _AllRooms.Count);
        }

        private static void InitRNG()
        {
            // Create a temporary RNG seeded with the number of seconds since midnight.
            _SeedRNG = new NoiseRNG((uint)new TimeSpan(DateTime.Now.Ticks).TotalSeconds);

            // Create the Room RNG (used for selecting rooms) and give it a random seed.
            _RNG_Room = new NoiseRNG(_SeedRNG.RollRandomUInt32());

            // Create the Rotation RNG (used for selecting rotations) and give it a random seed.
            _RNG_Rotation = new NoiseRNG(_SeedRNG.RollRandomUInt32());
        }

        private static void LoadRoomsData()
        {
            string roomSet = _DungeonTilemapManager.RoomSet;

            foreach (string file in Directory.GetFiles(ScriptableRoomUtilities.GetRoomSetPath(_DungeonTilemapManager.RoomSet)))
            {
                // If the file does not have the .asset file extension, then skip it. This prevents us from trying to load in the .meta files for each asset as if they were assets themselves.
                if (!file.EndsWith(".asset"))
                    continue;


                string roomToLoad = Path.GetFileNameWithoutExtension(file);
                string filePath = ScriptableRoomUtilities.GetRoomFileLoadPath(roomToLoad, roomSet);

                ScriptableRoom loadedRoom = Resources.Load<ScriptableRoom>(filePath);
                if (loadedRoom != null)
                {
                    RoomData roomData = new RoomData(loadedRoom);

                    _AllRooms.Add(roomData);
                    //Debug.Log($"DungeonGenerator.CollectRoomsData() - Loaded room asset \"{filePath}.asset\".");
                }
                else
                {
                    Debug.LogError($"DungeonGenerator.CollectRoomsData() - Could not load room asset \"{filePath}.asset\"! The file does not exist or something else went wrong.");
                }

            } // end foreach

        }

        private static void PreprocessRoomData()
        {
            PlaceholderUtils_Doors.DetectDoorLocations(_AllRooms);
        }


    }

}