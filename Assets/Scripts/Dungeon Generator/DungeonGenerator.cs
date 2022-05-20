
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


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;


namespace ProceduralDungeon.DungeonGeneration
{
    public static class DungeonGenerator
    {
        private static DungeonTilemapManager _DungeonTilemapManager;


        private static DungeonGraph _DungeonGraph; // Holds the layout information of the dungeon used to build it with tiles.
        private static MissionStructureGraph _MissionStructureGraph; // Holds the mission structure information of the dungeon, such as locations of things like keys, locks, or bosses.


        private static bool _IsInitialized = false;
        private static bool _IsGeneratingDungeon = false;


        // References to room prefabs.
        private static List<RoomData> _AllRooms;
        private static Dictionary<uint, List<RoomData>> _FilteredRoomListsCache;


        private static NoiseRNG _RNG_Seed = null;
        private static NoiseRNG _RNG_MissionStructureGen = null;
        private static NoiseRNG _RNG_DungeonGen = null;


        private static Dictionary<Vector3Int, DungeonDoor> _DoorFromTileDict; // Associates tile positions with doors that are occupying them.
        private static Dictionary<Vector3Int, DungeonGraphNode> _RoomFromTileDict; // Associates tile positions with rooms that are occupying them.


        private static List<DungeonDoor> _UnconnectedDoorways;



        public static DungeonGraph DungeonGraph { get { return _DungeonGraph; } }
        public static bool IsGeneratingDungeon { get { return _IsGeneratingDungeon; } }
        public static bool IsInitialized { get { return _IsInitialized; } }
        public static MissionStructureGraph MissionStructureGraph { get { return _MissionStructureGraph; } }



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

            _DoorFromTileDict = new Dictionary<Vector3Int, DungeonDoor>();
            _RoomFromTileDict = new Dictionary<Vector3Int, DungeonGraphNode>();

            _UnconnectedDoorways = new List<DungeonDoor>();


            LoadRoomsData();
            PreprocessRoomsData();

            _IsInitialized = true;
        }

        private static void ClearPreviousData()
        {
            _AllRooms.Clear();

            _DoorFromTileDict.Clear();
            _RoomFromTileDict.Clear();

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
            InitRNG(3659042729);
            Debug.Log($"SEED: {_RNG_Seed.GetSeed()}");


            // Generate the mission structure graph.           
            _MissionStructureGraph = MissionStructureGraph.Generate(_RNG_MissionStructureGen);

            BuildDungeonRooms();


            _DungeonTilemapManager.DungeonMap.CompressBoundsOfAllTileMaps();


            _IsGeneratingDungeon = false;

        }

        public static void BuildDungeonRooms()
        {
            Dictionary<MissionStructureGraphNode, DungeonGraphNode> parentDictionary = new Dictionary<MissionStructureGraphNode, DungeonGraphNode>();

            MissionStructureGraphNode curStructureNode = null;
            Queue<MissionStructureGraphNode> nodeQueue = new Queue<MissionStructureGraphNode>();


            if (_MissionStructureGraph.StartNode.IsTightlyCoupled)
                throw new Exception("DungeonGenerator.BuildDungeonRooms() - The start node in the mission start graph cannot be set as tightly coupled!");


            // Add the start node to the queue.
            nodeQueue.Enqueue(_MissionStructureGraph.StartNode);

            int roomCount = 0;
            while (nodeQueue.Count > 0)
            {
                roomCount++;

                // Get the next node.
                curStructureNode = nodeQueue.Dequeue();

                //Debug.Log($"DEQUEUE: {curNode.GrammarSymbol}");


                // Check if this is a terminal node.
                if (GenerativeGrammar.IsNonTerminalSymbol(curStructureNode.GrammarSymbol))
                {
                    Debug.LogWarning($"DungeonGenerator.CreateStartingRoom() - Encountered non-terminal node \"{curStructureNode.GrammarSymbol}\" in the mission structure graph! This node will be ignored.");
                    continue;
                }


                // If this mission structure node is tightly coupled to its parent, then connect it to the parent room.
                // Otherwise, connect it to a random unconnected door.
                int doorIndex = 0;
                DungeonGraphNode parentRoomNode = null;
                if (curStructureNode.IsTightlyCoupled)
                {
                    // Set the parent room to the last generated room.
                    parentRoomNode = parentDictionary[curStructureNode];

                    List<DungeonDoor> unconnectedDoors = parentRoomNode.GetUnconnectedDoors();
                    if (unconnectedDoors.Count < 1)
                        throw new Exception($"DungeonGenerator.BuildDungeonRooms() - The parent room \"{parentRoomNode.RoomBlueprint.RoomName}\" has no unconnected doors available!");

                    // Select a random unconnected door on the parent room to connect the new room to.
                    doorIndex = _RNG_DungeonGen.RollRandomIntInRange(0, unconnectedDoors.Count - 1);

                    // Get the index of the randomly selected door.
                    doorIndex = (int)unconnectedDoors[doorIndex].ThisRoom_DoorIndex;
                }
                else
                {
                    // Select a random unconnected door to connect the new room to.
                    doorIndex = _RNG_DungeonGen.RollRandomIntInRange(0, _UnconnectedDoorways.Count - 1);

                    // Set the parent room variable to the room that owns the randomly selected door.
                    if (curStructureNode != _MissionStructureGraph.StartNode)
                    {
                        parentRoomNode = _UnconnectedDoorways[doorIndex].ThisRoom_Node;
                        doorIndex = (int)_UnconnectedDoorways[doorIndex].ThisRoom_DoorIndex; // Get the index of the door within the parent room.
                    }
                }


                if (parentRoomNode != null)
                    Debug.Log($"Room1: \"{parentRoomNode.MissionStructureNode.GrammarSymbol}    R1DIndex: {doorIndex}");

                // Generate the DungeonGraphNode for the new room.
                DungeonGraphNode roomNode = CreateRoom(curStructureNode, parentRoomNode, doorIndex);
                if (curStructureNode == _MissionStructureGraph.StartNode)
                    _DungeonGraph = new DungeonGraph(roomNode);
                else
                    _DungeonGraph.AddNode(roomNode);


                // Place the room in the tilemaps.
                DungeonConstructionUtils.PlaceRoomTiles(_DungeonTilemapManager, roomNode, _RoomFromTileDict);


                foreach (MissionStructureGraphNode childNode in curStructureNode.ChildNodes)
                {
                    if (parentDictionary.ContainsKey(childNode))
                        continue;

                    parentDictionary.Add(childNode, roomNode);
                    nodeQueue.Enqueue(childNode);
                }


                Debug.Log($"Generated room \"{curStructureNode.GrammarSymbol}\"");
                if (roomCount == 10)
                    break;

            } // while nodeQueue is not empty

        }

        private static DungeonGraphNode CreateRoom(MissionStructureGraphNode missionStructureNode, DungeonGraphNode parentRoomNode, int indexOfDoorToConnectTo)
        {
            if (missionStructureNode.GrammarSymbol != GrammarSymbols.T_Entrance && parentRoomNode == null)
                throw new Exception("DungeonGenerator.CreateRoom() - The parentRoom parameter is null! All dungeon rooms must have a parent except the starting room.");


            DungeonGraphNode newRoomNode;
            if (missionStructureNode == _MissionStructureGraph.StartNode)
            {
                newRoomNode = CreateStartingRoom(2);
                return newRoomNode;
            }


            // Get minumum number of doors needed for the room.
            int minDoorsNeeded = missionStructureNode.GetTightlyCoupledChildNodeCount() + 2;
            minDoorsNeeded = Mathf.Max(minDoorsNeeded, 2);
            //Debug.Log($"MIN DOORS NEEDED: {minDoorsNeeded}    TightlyCoupledChildren: {missionStructureNode.GetTightlyCoupledChildNodeCount()}");

            // Get the parent room's data.
            RoomData parentRoomData = parentRoomNode.RoomBlueprint;

            // Get the door data for the parent room's door we're connecting to.
            int parentRoomDoorIndex = indexOfDoorToConnectTo;
            DoorData parentRoomDoorData = parentRoomData.DoorsList[parentRoomDoorIndex];

            // Get the level the door is on.
            RoomLevels parentDoorLevel = parentRoomDoorData.DoorLevel;

            // Select a random room on the same level as that door.
            RoomData newRoomData = SelectRandomRoomWithFilters((uint)minDoorsNeeded, true, parentDoorLevel);
            if (newRoomData == null)
                throw new Exception("DungeonGenerator.CreateRoom() - Failed to create a new room!");

            // Select a random door on the new room to connect to.
            int newRoomDoorIndex = SelectRandomDoorOnRoom(newRoomData, parentDoorLevel);

            // Create the new room and connect it to its parent.
            newRoomNode = DungeonConstructionUtils.CreateNewRoomConnectedToPrevious(parentRoomNode,
                                                                                    parentRoomData.DoorsList[parentRoomDoorIndex],
                                                                                    newRoomData,
                                                                                    newRoomData.DoorsList[newRoomDoorIndex],
                                                                                    missionStructureNode);


            // Initialize the doorways list on the new room's node.
            InitRoomDoors(newRoomNode);


            // Setup the fields that link the doors.
            parentRoomNode.Doorways[parentRoomDoorIndex].OtherRoom_Node = newRoomNode;
            parentRoomNode.Doorways[parentRoomDoorIndex].OtherRoom_DoorIndex = (uint)newRoomDoorIndex;
            newRoomNode.Doorways[newRoomDoorIndex].OtherRoom_Node = parentRoomNode;
            newRoomNode.Doorways[newRoomDoorIndex].OtherRoom_DoorIndex = (uint)parentRoomDoorIndex;


            // Remove each room's door from the unconnected doors list since they are no longer unconnected.
            _UnconnectedDoorways.Remove(parentRoomNode.Doorways[parentRoomDoorIndex]);
            _UnconnectedDoorways.Remove(newRoomNode.Doorways[newRoomDoorIndex]);


            // Give the mission structure node a link to the room generated from it.
            missionStructureNode.DungeonRoomNode = newRoomNode;


            /*
            switch (node.GrammarSymbol)
            {


                default:
                    throw new Exception($"DungeonGenerator.CreateRoom() - Received the invalid node type \"{node.GrammarSymbol}\"!");
            }
            */

            return newRoomNode;

        }

        private static DungeonGraphNode CreateStartingRoom(uint minDoorsNeeded)
        {
            // Choose a random rotation direction for the room.
            Directions direction = (Directions)_RNG_DungeonGen.RollRandomIntInRange(0, (int)Directions.West);

            // Choose a random floor for the start room to be on.
            RoomLevels floor = (RoomLevels)_RNG_DungeonGen.RollRandomIntInRange(1, (int)RoomLevels.Level_2ndFloor); // We start the random number range at 1 rather than 0 since we don't want Level_AnyFloor to get selected.

            // Select a random starting room blueprint.
            RoomData roomBlueprint = SelectRandomRoomWithFilters(minDoorsNeeded, true, floor, true, RoomTypeFlags.CanBeStart);
            if (roomBlueprint == null)
                throw new Exception($"DungeonGenerator.CreateStartingRoom() - There are no starting room blueprints available on \"{floor}\"!");

            // Create the starting room.
            DungeonGraphNode startRoomNode = new DungeonGraphNode(null,
                                                                  roomBlueprint,
                                                                  Vector3Int.zero,
                                                                  direction,
                                                                  _MissionStructureGraph.StartNode);


            // Give the mission structure node a link to the room generated from it.
            _MissionStructureGraph.StartNode.DungeonRoomNode = startRoomNode;


            InitRoomDoors(startRoomNode);

            ConfigureEntranceOrExitDoor(startRoomNode, true);


            return startRoomNode;
        }

        private static void InitRoomDoors(DungeonGraphNode room)
        {
            // Create a connection struct for each door to track connections to this room.
            for (int i = 0; i < room.RoomBlueprint.DoorsList.Count; i++)
            {
                DungeonDoor door = new DungeonDoor();

                door.ThisRoom_Node = room;
                door.ThisRoom_DoorIndex = (uint)i;

                DoorData data = room.RoomBlueprint.DoorsList[i];


                // Get the world position of both tiles of this door.
                Vector3Int tile1AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(data.Tile1Position, room.RoomPosition, room.RoomDirection);
                Vector3Int tile2AdjustedPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(data.Tile2Position, room.RoomPosition, room.RoomDirection);

                if (_DoorFromTileDict.ContainsKey(tile1AdjustedPos))
                    throw new Exception($"DungeonGenerator.InitRoomDoors() - Couldn't register tile 1 of door (index={i}) in room \"{room.RoomBlueprint.RoomName}\" in the tracking dictionary because one is already registered at this position!");
                else if (_DoorFromTileDict.ContainsKey(tile2AdjustedPos))
                    throw new Exception($"DungeonGenerator.InitRoomDoors() - Couldn't register tile 2 of door (index={i}) in room \"{room.RoomBlueprint.RoomName}\" in the tracking dictionary because one is already registered at this position!");



                //Debug.Log($"Room \"{room.RoomBlueprint.RoomName}\"    Door[{i}]:  Tile1: {tile1AdjustedPos}  Tile2: {tile2AdjustedPos}");


                // Register them in the dictionary that associates tiles with the doors that occupy them.
                _DoorFromTileDict.Add(tile1AdjustedPos, door);
                _DoorFromTileDict.Add(tile2AdjustedPos, door);


                // Add this door to the room's connection's list, and also add it to the dungeon generator's unconnected doors list.
                room.Doorways.Add(door);
                _UnconnectedDoorways.Add(door);

            } // end for i

        }

        private static void ConfigureEntranceOrExitDoor(DungeonGraphNode room, bool isEntranceDoor)
        {
            // Select a random door in the room to be the entrance door.
            int index = _RNG_DungeonGen.RollRandomIntInRange(0, room.Doorways.Count - 1);


            // Get a reference to the entrance or exit door.
            DungeonDoor door = room.Doorways[index];

            // Set the selected door as the entrance or exit door.
            door.Flags = isEntranceDoor ? DungeonDoorFlags.IsEntranceDoor : DungeonDoorFlags.IsGoalDoor;

            // Remove this door from the unconnected doors list.
            _UnconnectedDoorways.Remove(door);


            if (isEntranceDoor)
            {
                // Position the player next to the entrance door.
                DungeonConstructionUtils.PositionPlayer(_DungeonTilemapManager, room, door);
            }

        }

        private static int SelectRandomDoorOnRoom(RoomData room, RoomLevels doorLevel)
        {
            List<DoorData> doorsOnDesiredLevel = new List<DoorData>();

            foreach (DoorData door in room.DoorsList)
            {
                if (door.DoorLevel == doorLevel)
                    doorsOnDesiredLevel.Add(door);
            }

            // Select a random door from the list of those that are on the desired floor.
            int doorIndex = _RNG_DungeonGen.RollRandomIntInRange(0, doorsOnDesiredLevel.Count - 1);

            if (doorsOnDesiredLevel.Count < 1)
                throw new Exception($"DungeonGenerator.SelectRandomDoor() - The specified room \"{room.RoomName}\" has no doors on the floor \"{doorLevel}\"!");


            // Return the selected door's data.
            return room.DoorsList.IndexOf(doorsOnDesiredLevel[doorIndex]);
        }

        /// <summary>
        /// Selects a random room with n doors.
        /// </summary>
        /// <param name="doorCount">The desired number of doors.</param>
        /// <param name="greaterThanOrEqual">If this is true, then instead of randomly choosing a room with n doors, this function will randomly choose a room whose door count is greater than or equal to n.</param>
        /// <param name="roomLevel">The floor the room should be on.
        /// <param name="filterByRoomType">Indicates whether to also filter by room type flags.
        /// <param name="roomTypeFlags">The room type flags the room should have.
        /// <returns>The index of the selected room.</returns>
        private static RoomData SelectRandomRoomWithFilters(uint doorCount, bool greaterThanOrEqual = false, RoomLevels roomLevel = RoomLevels.Level_AnyFloor, bool filterByRoomType = false, RoomTypeFlags roomTypeFlags = 0)
        {
            Assert.IsFalse(doorCount == 0, "DungeonGenerator.SelectRandomRoomByDoorCount() - The passed in door count must be greater than 0!");


            List<RoomData> list;

            uint key = doorCount;
            if (greaterThanOrEqual)
                key += 100;
            if (roomLevel != RoomLevels.Level_AnyFloor)
                key += 1000 * (uint)roomLevel;
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
                    if (!filterByRoomType)
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


            /*
            string temp = greaterThanOrEqual ? ">=" : "==";
            Debug.Log($"Select random room params:    DoorCount: {temp}{doorCount}    RoomLevel: {roomLevel}    FilterByRoomTypeFlags: {filterByRoomType}    RoomTypeFlags: {roomTypeFlags}");
            Debug.Log($"{list.Count} rooms passed the filters.");
            */


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