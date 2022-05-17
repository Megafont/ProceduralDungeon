
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.Assertions;

using ToolboxLib_Shared.Math;
using ToolboxLib_Shared.Utilities;

using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction.PlaceholderUtilities;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.TileMaps;



namespace ProceduralDungeon.DungeonGeneration
{
    public static class DungeonGenerator
    {
        private static DungeonGraph _DungeonGraph; // Holds the layout information of the dungeon used to build it with tiles.
        private static MissionStructureGraph _MissionStructureGraph; // Holds the mission structure information of the dungeon, such as locations of things like keys, locks, or bosses.


        private static DungeonTilemapManager _DungeonTilemapManager;

        private static bool _IsInitialized = false;
        private static bool _IsGeneratingDungeon = false;


        // References to room prefabs.
        private static List<RoomData> _AllRooms;
        private static Dictionary<uint, List<RoomData>> _FilteredRoomListsCache;

        private static List<DungeonDoor> _UnconnectedDoorways;

        public static DungeonGraph DungeonGraph { get { return _DungeonGraph; } }
        public static bool IsGeneratingDungeon { get { return _IsGeneratingDungeon; } }
        public static bool IsInitialized { get { return _IsInitialized; } }
        public static MissionStructureGraph MissionStructureGraph { get { return _MissionStructureGraph; } }


        private static NoiseRNG _RNG_Seed = null;
        private static NoiseRNG _RNG_MissionStructureGen = null;
        private static NoiseRNG _RNG_DungeonGen = null;



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
            _FilteredRoomListsCache = new Dictionary<uint, List<RoomData>>();

            _UnconnectedDoorways = new List<DungeonDoor>();


            LoadRoomsData();
            PreprocessRoomsData();

            _IsInitialized = true;
        }

        private static void ClearPreviousData()
        {
            _AllRooms.Clear();

            _UnconnectedDoorways.Clear();
        }


        private static uint InitRNG()
        {
            // Auto-generate a main seed.
            uint mainSeed = (uint)new TimeSpan(DateTime.Now.Ticks).TotalSeconds;


            // Pass the seed into the other overload of this method.
            return InitRNG(mainSeed);
        }

        private static uint InitRNG(uint mainSeed)
        {
            // Create the seed RNG (used for seeding the other RNGs) seeded with the number of seconds since midnight.
            // We use this RNG to seed a few others. This way we can randomly generate different types of things independently of each other
            // and still have them be deterministic per seed. As long as items in a category are always generated in the same way,
            // the seed will always create the same result.
            _RNG_Seed = new NoiseRNG(mainSeed);

            // Create the Mission Structure RNG (used for generating the missiong structure).
            _RNG_MissionStructureGen = new NoiseRNG(_RNG_Seed.RollRandomUInt32());

            // Create the Room RNG (used for generating rooms) and give it a random seed.
            _RNG_DungeonGen = new NoiseRNG(_RNG_Seed.RollRandomUInt32());


            return mainSeed;
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

                    //Debug.Log($"DungeonGenerator.CollectRoomsData() - Loaded room asset \"{filePath}.asset\".");


                    _AllRooms.Add(roomData);
                }
                else
                {
                    Debug.LogError($"DungeonGenerator.CollectRoomsData() - Could not load room asset \"{filePath}.asset\"! The file does not exist or something else went wrong.");
                }

            } // end foreach

        }

        private static void PreprocessRoomsData()
        {
            PlaceholderUtils_Doors.DetectDoorLocations(_AllRooms);
        }

        public static void GenerateDungeon()
        {
            _IsGeneratingDungeon = true;


            _DungeonTilemapManager.DungeonMap.ClearAllTileMaps();


            // Init the random number generators.
            InitRNG(3658779416); // 3658764288 - This was a problematic seed that revealed a bug in the node positioning before nodes were snapped on rooms.
                                 // 3658765094


            Debug.Log($"SEED: {_RNG_Seed.GetSeed()}");


            // Generate the mission structure graph.           
            _MissionStructureGraph = MissionStructureGraph.Generate(_RNG_MissionStructureGen);


            // Create a new DungeonGraph with a node for the starting room.
            _DungeonGraph = new DungeonGraph(CreateStartingRoom());


            /*
            int index = _AllRooms.Count - 2;
            DungeonGraphNode newRoom = DungeonConstructionUtils.GenerateNewRoomAndConnectToPrevious(_DungeonGraph.StartNode,
                                                                                                    _DungeonGraph.StartNode.RoomBlueprint.DoorsList[0],
                                                                                                    _AllRooms[index],
                                                                                                    _AllRooms[index].DoorsList[0]);
            */

            // Place the starting room.           
            DungeonConstructionUtils.PlaceRoomTiles(_DungeonTilemapManager, _DungeonGraph.StartRoomNode);
            //DungeonConstructionUtils.PlaceRoomTiles(_DungeonTilemapManager, newRoom);


            _DungeonTilemapManager.DungeonMap.CompressBoundsOfAllTileMaps();


            _IsGeneratingDungeon = false;

        }

        private static DungeonGraphNode CreateStartingRoom()
        {

            // Choose a random floor for the start room to be on.
            RoomLevels floor = (RoomLevels)_RNG_DungeonGen.RollRandomIntInRange(1, (int)RoomLevels.Level_2ndFloor); // We start the random number range at 1 rather than 0 since we don't want Level_AnyFloor to get selected.

            // Choose a random rotation direction for the room.
            Directions direction = (Directions)_RNG_DungeonGen.RollRandomIntInRange(0, (int)Directions.West);

            // Select a random starting room blueprint.
            RoomData roomBlueprint = SelectRandomRoomWithFilters(2, true, floor, RoomTypeFlags.CanBeStart);
            if (roomBlueprint == null)
                throw new Exception($"DungeonGenerator.CreateStartingRoom() - There are no starting room blueprints available on \"{floor}\"!");


            // Create the starting room.
            DungeonGraphNode startRoom = new DungeonGraphNode(null,
                                                              roomBlueprint,
                                                              Vector3Int.zero,
                                                              direction,
                                                              _MissionStructureGraph.StartNode);


            InitRoomDoors(startRoom);

            ConfigureEntranceDoor(startRoom);


            return startRoom;
        }

        private static void InitRoomDoors(DungeonGraphNode room)
        {
            // Create a connection struct for each door to track connections to this room.
            for (int i = 0; i < room.RoomBlueprint.DoorsList.Count; i++)
            {
                DungeonDoor rConnection = new DungeonDoor();

                rConnection.ParentRoom_Node = room;
                rConnection.ParentRoom_DoorIndex = (uint)i;

                // Add this door to the room's connection's list, and also add it to the dungeon generator's unconnected doors list.
                room.Doorways.Add(rConnection);
                _UnconnectedDoorways.Add(rConnection);

            } // end for i

        }

        private static void ConfigureEntranceDoor(DungeonGraphNode startRoom)
        {
            // Select a random door in the room to be the entrance door.
            int index = _RNG_DungeonGen.RollRandomIntInRange(0, startRoom.Doorways.Count - 1);


            // Get a reference to the entrance door.
            DungeonDoor entranceDoor = startRoom.Doorways[index];

            // Set the selected door as the entrance door.
            entranceDoor.Flags = DungeonDoorFlags.IsEntranceDoor;

            // Remove this door from the unconnected doors list.
            _UnconnectedDoorways.Remove(entranceDoor);


            // Position the player next to the entrance door.
            PositionPlayer(startRoom, entranceDoor);

        }

        private static void PositionPlayer(DungeonGraphNode startRoom, DungeonDoor entranceDoor)
        {
            int index = (int)entranceDoor.ParentRoom_DoorIndex;

            // Get the position of both tiles of the entrance door.
            Vector3Int doorTile1Pos = entranceDoor.ParentRoom_Node.RoomBlueprint.DoorsList[index].Tile1Position;
            Vector3Int doorTile2Pos = entranceDoor.ParentRoom_Node.RoomBlueprint.DoorsList[index].Tile2Position;

            // Adjust the tile positions to take into account the position and rotation direction of the room.
            doorTile1Pos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(doorTile1Pos, startRoom.RoomPosition, startRoom.RoomDirection);
            doorTile2Pos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(doorTile2Pos, startRoom.RoomPosition, startRoom.RoomDirection);

            // Get the position of the upper-left-most of the two tiles.
            Vector3Int playerPos = MiscellaneousUtils.GetUpperLeftMostTile(doorTile1Pos, doorTile2Pos);


            // Get the direction of the entrance door.
            Directions doorDirection = entranceDoor.ParentRoom_Node.RoomBlueprint.DoorsList[index].DoorDirection;

            // Adjust the door direction to take into account the room's rotation direction.
            doorDirection = MiscellaneousUtils.AddRotationDirectionsTogether(doorDirection, startRoom.RoomDirection);


            // Move the player character next to the entrance door.
            _DungeonTilemapManager.PositionPlayerByStartDoor(playerPos, doorDirection);
        }

        /// <summary>
        /// Selects a random room with n doors.
        /// </summary>
        /// <param name="doorCount">The desired number of doors.</param>
        /// <param name="greaterThanOrEqual">If this is true, then instead of randomly choosing a room with n doors, this function will randomly choose a room whose door count is greater than or equal to n.</param>
        /// <returns>The index of the selected room.</returns>
        private static RoomData SelectRandomRoomWithFilters(uint doorCount, bool greaterThanOrEqual = false, RoomLevels roomLevel = RoomLevels.Level_AnyFloor, RoomTypeFlags roomTypeFlags = 0)
        {
            Assert.IsFalse(doorCount == 0, "DungeonGenerator.SelectRandomRoomByDoorCount() - The passed in door count must be greater than 0!");


            List<RoomData> list;

            uint key = doorCount;
            if (greaterThanOrEqual)
                key += 100;
            if (roomLevel != RoomLevels.Level_AnyFloor)
                key += 1000;
            if (roomTypeFlags != 0)
                key += 10000 * (uint)roomTypeFlags;


            _FilteredRoomListsCache.TryGetValue(key, out list);


            if (list == null)
            {
                list = new List<RoomData>();

                foreach (RoomData room in _AllRooms)
                {
                    bool levelFilterPassed = false;
                    if (roomLevel == RoomLevels.Level_AnyFloor)
                        levelFilterPassed = true;
                    else if (room.RoomLevel == roomLevel)
                        levelFilterPassed = true;


                    bool roomTypeFlagsFilterPassed = false;
                    if (roomTypeFlags == 0)
                        roomTypeFlagsFilterPassed = true;
                    else if (Flags.HasAllFlags((uint)room.RoomTypeFlags, (uint)roomTypeFlags))
                        roomTypeFlagsFilterPassed = true;


                    if (greaterThanOrEqual)
                    {
                        if (room.DoorsList.Count >= doorCount && levelFilterPassed && roomTypeFlagsFilterPassed)
                            list.Add(room);
                    }
                    else
                    {
                        if (room.DoorsList.Count == doorCount && levelFilterPassed && roomTypeFlagsFilterPassed)
                            list.Add(room);
                    }


                } // end foreach room


                // Cache this filtered list so it can be reused.
                _FilteredRoomListsCache.Add(key, list);

            } // end if (list == null)



            // Select a random room from the filtered list.
            int roomIndex = _RNG_DungeonGen.RollRandomIntInRange(0, list.Count - 1);

            // Return the selected room blueprint.
            // I added this if statement, because it crashed when my test room set had no start rooms on the 2nd floor. So now it returns null in this case.
            if (list.Count == 0)
                return null;
            else
                return list[roomIndex];

        }


    }

}