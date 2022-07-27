
using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction.PlaceholderUtilities;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.InGame.Items;
using ProceduralDungeon.TileMaps;
using System;
using System.Collections.Generic;
using System.IO;
using ToolboxLib_Shared.Math;
using ToolboxLib_Shared.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;
using MSCNData = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.MissionStructureChildNodeData;


namespace ProceduralDungeon.DungeonGeneration
{
    public static class DungeonGenerator
    {
        private const float EXTRA_DOORS_ROOM_SPAWN_CHANCE = 0.3f; // The probability that each unused doorway will have a new room spawned next to it after the main dungeon generation is done.
        private const int LINEAR_DOOR_SCAN_LENGTH = 5; // How many tiles to scan in front of a door for room collisions and neighboring doors.
        private const int MAX_FIND_DOOR_ATTEMPTS = 256; // Max. number of times FindDoorToConnectNewRoomTo() will try to find a door to connect a new room to.
        private const int MAX_ROOM_BLUEPRINT_SELECTION_ATTEMPTS = 256; // Max. number of times to try choosing and placing a room blueprint before aborting.
        private const int MAX_ROOM_CONNECTION_ATTEMPTS = 256; // Max. number of times to try connecting a new room into one of the unconnected doors on the dungeon.


        public static bool ForceStartRoomOnFirstFloor = true; // If enabled, forces the dungeon's starting room to be on the first floor.


        private static Dictionary<MissionStructureGraphNode, DungeonGraphNode> _ParentDictionary;


        // DO NOT delete these variables. They are functional.
        private static DungeonDoor _EntranceDoor;
        private static DungeonDoor _GoalDoor;

        private static bool _IsFinalizingDoors = false;


        // References to room prefabs.
        private static List<RoomData> _AllRooms;
        private static Dictionary<uint, List<RoomData>> _FilteredRoomListsCache;


        private static NoiseRNG _RNG_Seed = null;
        private static NoiseRNG _RNG_MissionStructureGen = null;
        private static Dictionary<Vector3Int, DungeonDoor> _DoorFromTileDict; // Associates tile positions with doors that are occupying them.
        private static Dictionary<Vector3Int, DungeonGraphNode> _RoomFromTileDict; // Associates tile positions with rooms that are occupying them.

        // Holds references too fake tiles.
        // These are non-existant tiles registered outside the dungeon's entrance and exit doors.
        // We register these fake tiles in the _RoomFromTileDict dictionary to trick the generator into not generating rooms directly outside the entry or exit doors of the dungeon by making it think something is already there.
        private static List<Vector3Int> _FakeRoomTiles; 

        private static List<DungeonDoor> _BlockedDoorways;
        private static Dictionary<int, List<DungeonDoor>> _DoorsBehindLocks;
        private static List<DungeonDoor> _UnconnectedDoorways;



        public static NoiseRNG RNG_DungeonGen { get; private set; }
        public static NoiseRNG RNG_InGame { get; private set; }        
        public static DungeonGraph DungeonGraph { get; private set; }
        public static DungeonTilemapManager DungeonTilemapManager { get; private set; }
        public static bool IsGeneratingDungeon { get; private set; } = false;
        public static bool IsInitialized { get; private set; } = false;
        public static ItemDatabaseObject ItemDatabase { get; private set; }
        public static MissionStructureGraph MissionStructureGraph { get; private set; }

        

        public static void Init(DungeonTilemapManager manager)
        {
            Assert.IsNotNull(manager, "DungeonGenerator.Init() - Cannot initialize the dungeon generator, because the passed in DungeonGenerator is null!");


            if (!IsInitialized)
            {
                IsInitialized = false;

                ItemDatabase = manager.ItemDatabase;


                // Move these lists into a theme object later that holds all rooms from a certain set?
                // This would be necessary if I make this generator able to incorporate multiple themes into one dungeon (like cave and brick rooms or something);
                _AllRooms = new List<RoomData>();
                _FilteredRoomListsCache = new Dictionary<uint, List<RoomData>>();

                _DoorFromTileDict = new Dictionary<Vector3Int, DungeonDoor>();
                _RoomFromTileDict = new Dictionary<Vector3Int, DungeonGraphNode>();
                _FakeRoomTiles = new List<Vector3Int>();

                _ParentDictionary = new Dictionary<MissionStructureGraphNode, DungeonGraphNode>();

                _BlockedDoorways = new List<DungeonDoor>();
                _DoorsBehindLocks = new Dictionary<int, List<DungeonDoor>>();
                _UnconnectedDoorways = new List<DungeonDoor>();

            }


            _IsFinalizingDoors = false;
            IsGeneratingDungeon = false;

            ClearPreviousData();

            DungeonTilemapManager = manager;


            LoadRoomsData();
            PreprocessRoomsData();

            IsInitialized = true;
        }

        private static void ClearPreviousData()
        {
            _AllRooms.Clear();

            _DoorFromTileDict.Clear();
            _RoomFromTileDict.Clear();
            _FakeRoomTiles.Clear();

            _BlockedDoorways.Clear();
            _DoorsBehindLocks.Clear();
            _UnconnectedDoorways.Clear();

            _EntranceDoor = null;
            _GoalDoor = null;
        }

        /// <summary>
        /// Initializes the random number generator using the current time as the seed.
        /// </summary>
        /// <returns>The seed.</returns>
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
            RNG_DungeonGen = new NoiseRNG(_RNG_Seed.RollRandomUInt32());

            // Create the in game RNG (used for enemies and other things going on during gameplay).
            RNG_InGame = new NoiseRNG(_RNG_Seed.RollRandomUInt32());


            return mainSeed;
        }

        private static void LoadRoomsData()
        {
            string roomSet = Enum.GetName(typeof(RoomSets), DungeonTilemapManager.RoomSet);

            foreach (string file in Directory.GetFiles(ScriptableRoomUtilities.GetRoomSetPath(roomSet)))
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
            IsGeneratingDungeon = true;



            DungeonTilemapManager.DungeonMap.ClearAllTileMaps();


            // Init the random number generators.
            InitRNG(3664913279); // Previous test seed: 3660483198
            Debug.Log($"SEED: {_RNG_Seed.GetSeed()}");


            DungeonPopulator.ClearPreviouslySpawnedPrefabs();



            // Generate the mission structure graph.
#pragma warning disable CS0168 // Disable the variable is declared but never used warning so it doesn't show up for the variable (Exception e) below.
            try
            {
                MissionStructureGraph = MissionStructureGraph.Generate(_RNG_MissionStructureGen);
                BuildDungeonRooms();
            }
            catch (Exception e)
            {
                IsGeneratingDungeon = false;


                // Rethrow the exception just so Unity will log it with its stack trace.
                // We don't specify the variable e here, as doing so will rethrow the exception with new stack trace information referencing this function instead of the one that originally threw the exception.
                // Source: https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2200
                throw;
            }
#pragma warning restore CS0168 // Re-enable the variable is declared but never used warning



            FinalizeUnusedDoorways();
            DungeonConstructionUtils.SealOffBlockedDoors(DungeonTilemapManager, _BlockedDoorways, RNG_DungeonGen);

            // Reset the node positions so the ones added by FinalizedUnusedDoorways() for extra rooms get positions assigned.
            // This is only needed so they show up properly in the Unity Editor when the MissionStructureGraphGizmos script is not set to snap nodes to their generated dungeon rooms.
            GrammarRuleProcessor.SetPositions();


            DungeonTilemapManager.DungeonMap.CompressBoundsOfAllTileMaps();

            DungeonPopulator.PopulateDungeon(DungeonGraph, RNG_DungeonGen);

            if (Application.isPlaying)
            {
                DungeonEnemySpawner.SpawnEnemiesInDungeon();
            }


            IsGeneratingDungeon = false;

        }

        public static void BuildDungeonRooms()
        {
            MissionStructureGraphNode curStructureNode;
            Queue<MissionStructureGraphNode> nodeQueue = DungeonGeneratorUtils.DetermineRoomGenerationOrder(MissionStructureGraph);


            // Add the start node to the queue.
            //nodeQueue.Enqueue(MissionStructureGraph.StartNode);

            int roomCount = 0;
            while (nodeQueue.Count > 0)
            {
                roomCount++;
                
                // Get the next node.
                curStructureNode = nodeQueue.Dequeue();


                // Check if this is a terminal node.
                if (GenerativeGrammar.IsNonTerminalSymbol(curStructureNode.GrammarSymbol))
                {
                    Debug.LogError($"DungeonGenerator.BuildDungeonRooms() - Encountered a non-terminal node (\"{curStructureNode.GrammarSymbol}\") in the mission structure graph! This node will be ignored.");
                    continue;
                }


                // If it hasn't already been added, then add the current node and its parent into the parent association table.
                if (curStructureNode.GrammarSymbol != GrammarSymbols.T_Entrance &&
                    !_ParentDictionary.ContainsKey(curStructureNode))
                {
                    _ParentDictionary.Add(curStructureNode, MissionStructureGraph.FindFirstParent(curStructureNode).DungeonRoomNode);
                }


                DungeonGraphNode roomNode = SnapNewRoomIntoDungeon(curStructureNode);


                // Place the room in the tilemaps.
                DungeonConstructionUtils.PlaceRoomTiles(DungeonTilemapManager, roomNode, _RoomFromTileDict);



                if (curStructureNode.GrammarSymbol == GrammarSymbols.T_Goal)
                    DungeonGraph.GoalRoomNode = curStructureNode.DungeonRoomNode;

                Debug.Log($"Generated room \"{curStructureNode.GrammarSymbol}\"");


                // This debug code limits the generator to only generating n rooms before it stops.
                //if (roomCount == 10)
                //    break;


            } // while nodeQueue is not empty

        }

        public static DungeonDoor LookupDoorFromTile(Vector3Int tilePos)
        {
            DungeonDoor result;

            _DoorFromTileDict.TryGetValue(tilePos, out result);

            return result;
        }

        public static DungeonGraphNode LookupRoomFromTile(Vector3Int tilePos)
        {
            DungeonGraphNode result;

            _RoomFromTileDict.TryGetValue(tilePos, out result);

            return result;
        }

        public static bool IsFakeTile(Vector3Int tilePos)
        {
            return _FakeRoomTiles.Contains(tilePos);
        }

        public static List<Vector3Int> GetTilePositionsInRoom(DungeonGraphNode roomNode)
        {
            List<Vector3Int> roomTilePositions = new List<Vector3Int>();

            foreach (KeyValuePair<Vector3Int, DungeonGraphNode> pair in _RoomFromTileDict)
            {
                if (pair.Value == roomNode)
                    roomTilePositions.Add(pair.Key);
            }

            return roomTilePositions;
        }

        private static DungeonGraphNode SnapNewRoomIntoDungeon(MissionStructureGraphNode missionStructureNode, DungeonDoor doorToConnectTo = null)
        {
            int attempts = 0;
            List<DungeonDoor> blockedDoors = new List<DungeonDoor>(); // Holds doors that are blocked
            List<DungeonDoor> doorsToLink = new List<DungeonDoor>(); // Holds doors that connected by chance.
            DungeonGraphNode roomNode;


            // Clear the lists that keep track of available doors and doors we've attempted to connect a room to so there is no old data in there.
            // NOTE: These lists are used by FindDoorToConnectRoomTo().
            _AttemptedDoors.Clear();
            _AvailableDoors.Clear();


            // Attempt to add a new room to the dungeon.
            while (true)
            {
                if (attempts >= MAX_ROOM_CONNECTION_ATTEMPTS)
                    throw new Exception($"DungeonGenerator.SnapNewRoomIntoDungeon() - Failed to connect another room \"{missionStructureNode.GrammarSymbol}\" onto the dungeon! No more retry attempts left.");


                attempts++;

                blockedDoors.Clear();
                doorsToLink.Clear();


                // Try to find an existing door to connect a new room to.
                if (!_IsFinalizingDoors && missionStructureNode.GrammarSymbol != GrammarSymbols.T_Entrance)
                {
                    doorToConnectTo = FindDoorToConnectNewRoomTo(missionStructureNode);
                    
                    // If the room we're trying to connect doesn't have any extra doorways left for connections after its tightly coupled child nodes get connected,
                    // and if the current node is not a tightly coupled child of that node, then jump back to the start of the while loop to try again since we
                    // can't connect the new room to this room.
                    if (doorToConnectTo == null)
                    {
                        if (_AvailableDoors.Count < 1)
                            throw new Exception($"DungeonGenerator.SnapNewRoomIntoDungeon() - Failed to connect another room \"{missionStructureNode.GrammarSymbol}\" onto the dungeon! No more available doors left.");

                        //Debug.LogWarning($"Could not connect new room \"{missionStructureNode.GrammarSymbol}\" to the dungeon!");
                        continue;
                    }

                }


                if (doorToConnectTo != null && doorToConnectTo.OtherRoom_Node != null)
                {
                    //Debug.Log($"Room1: \"{doorToConnectTo.OtherRoom_Node.MissionStructureNode.GrammarSymbol}\"    R1DIndex: {doorToConnectTo.OtherRoom_DoorIndex}");
                }

                // Generate the DungeonGraphNode for the new room.
                roomNode = CreateRoom(missionStructureNode, doorToConnectTo, doorsToLink);               
                if (roomNode == null)
                {
                    //Debug.LogWarning($"DungeonGenerator.SnapNewRoomIntoDungeon() - Attempt #{attempts}: Failed to connect new room onto the dungeon!");

                    if (!_IsFinalizingDoors)
                    {
                        // NOTE: Unlike the failure cases further down in this function, we do not call CleanupRejectedRoomData() here
                        //       since the room node's Doorways list has not been set up yet, so there is nothing to clean up.

                        continue; // We didn't fit a room successfully, so jump to the top of this while loop to try again.
                    }
                    else
                        return null; // We were called by the FinalizeUnusedDoorways() function, so bail out since we're only trying to snap a room to a specific spot in this case.

                }


                if (missionStructureNode.GrammarSymbol == GrammarSymbols.T_Entrance)
                {
                    DungeonGraph = new DungeonGraph(roomNode);
                    break; // This is the starting room, so we don't need to do any doorway validations. Simply break out of the loop instead.
                }


                // Check the door layout of the current possible placement of this room.
                if (!CheckPossibleRoomPlacementDoorLayout(roomNode, doorToConnectTo, blockedDoors, doorsToLink, out int doorsLost))
                {
                    CleanupRejectedRoomData(roomNode);

                    continue;
                }

                // See if there are still enough doors available on the room after collisions were detected.
                int doorsLeft = roomNode.RoomBlueprint.DoorsList.Count - 1 - doorsLost; // We subtract one since the room already has one door unavailable since it is already used to connect it to the parent room.
                int minRemainingDoorsRequired;
                if (missionStructureNode.GrammarSymbol != GrammarSymbols.T_Goal)
                    minRemainingDoorsRequired = missionStructureNode.GetTightlyCoupledChildNodeCount();
                else
                    minRemainingDoorsRequired = 1; // If this is the goal room, set this to one. We are basically pretending this final dungeon room has one child. This way we end up with an extra door on that room to be the exit door.

                if (doorsLeft >= minRemainingDoorsRequired)
                {
                    DungeonGraph.AddNode(roomNode);


                    // Copy any blocked doors into the global blocked doors list since we know this room is now finalized for placement in the dungeon.
                    // Also remove them from the global unconnected doors list.
                    foreach (DungeonDoor door in blockedDoors)
                        MarkDoorAsUnavailable(door, true);


                    // Since we successfully added a new room to the dungeon, exit this while loop to continue the dungeon construction process.
                    break;
                }
                else // We failed to create this room, so clear its data.
                {
                    CleanupRejectedRoomData(roomNode);

                    Debug.LogWarning($"DungeonGenerator.SnapNewRoomIntoDungeon() - Not enough doors left for tightly coupled child rooms after collisions detected on room \"{roomNode.RoomBlueprint.RoomName}\".");
                }


                // If this is the goal room, then assign one of the unused doors to be the exit door.
                if (missionStructureNode == MissionStructureGraph.GoalNode)
                {
                    if (ConfigureEntranceOrExitRoomDoor(roomNode, false))
                    {
                        break;
                    }
                    else
                    {
                        CleanupRejectedRoomData(roomNode);
                        continue;
                    }
                }

            } // end while



            // Connect any doors that need to be linked.
            // NOTE: The DungeonDoor objects in this list are not actual doors, but being used as data holders.
            foreach (DungeonDoor doorData in doorsToLink)
                ConfigureConnectingDoorway(doorData);


            return roomNode;

        }

        private static DungeonGraphNode CreateRoom(MissionStructureGraphNode missionStructureNode, DungeonDoor doorToConnectTo, List<DungeonDoor> doorsToLink)
        {
            if (missionStructureNode.GrammarSymbol != GrammarSymbols.T_Entrance && doorToConnectTo.OtherRoom_Node == null)
                throw new Exception("DungeonGenerator.CreateRoom() - The door to connect to does not have a parent room!");


            DungeonGraphNode newRoomNode;
            if (missionStructureNode == MissionStructureGraph.StartNode)
            {
                newRoomNode = CreateStartingRoom(2);
                return newRoomNode;
            }


            // Get minumum number of doors needed for the room.
            int minDoorsNeeded;
            bool greaterThanOrEqual;
            if (!_IsFinalizingDoors)
            {
                minDoorsNeeded = missionStructureNode.GetTightlyCoupledChildNodeCount() + 1;
                minDoorsNeeded = Mathf.Max(minDoorsNeeded, 1);

                greaterThanOrEqual = true;
            }
            else
            {
                minDoorsNeeded = 1;
                greaterThanOrEqual = false;
            }


            //Debug.Log($"MIN DOORS NEEDED: {minDoorsNeeded}    TightlyCoupledChildren: {missionStructureNode.GetTightlyCoupledChildNodeCount()}");

            // Get the parent room's data.
            RoomData parentRoomData = doorToConnectTo.OtherRoom_Node.RoomBlueprint;

            // Get the door data for the parent room's door we're connecting to.
            DoorData parentRoomDoorData = parentRoomData.DoorsList[(int)doorToConnectTo.OtherRoom_DoorIndex];

            // Get the level the door is on.
            RoomLevels parentDoorLevel = parentRoomDoorData.DoorLevel;


            RoomData newRoomData;
            uint newRoomDoorIndex;
            int attempts = 0;
            while (true)
            {
                attempts++;


                // Select a random room on the same level as that door.
                RoomTypeFlags filterFlags = GetRoomTypeFlagsFromMissionStructureNode(missionStructureNode);
//                if (filterFlags == 0)
//                    newRoomData = SelectRandomRoomWithFilters((uint)minDoorsNeeded, greaterThanOrEqual, parentDoorLevel);
//                else
                    newRoomData = SelectRandomRoomWithFilters((uint)minDoorsNeeded, greaterThanOrEqual, parentDoorLevel, true, filterFlags);

                if (parentDoorLevel == RoomLevels.Level_AnyFloor)
                {
                    // Debug.LogError($"FLOOR ANY:   R: {newRoomData.RoomName}    M: {missionStructureNode.GrammarSymbol}    DR: {doorToConnectTo.OtherRoom_Node.RoomBlueprint.RoomName}    DM: {doorToConnectTo.OtherRoom_Node.MissionStructureNode.GrammarSymbol}");
                }

                // Select a random door on the new room to connect to the existing door we already chose.
                newRoomDoorIndex = (uint)SelectRandomDoorOnRoomAndFloor(newRoomData, parentDoorLevel);


                // Create the new room and connect it to its parent.
                newRoomNode = DungeonConstructionUtils.CreateNewRoomConnectedToPrevious(doorToConnectTo.OtherRoom_Node,
                                                                                        doorToConnectTo.OtherRoom_DoorIndex,
                                                                                        newRoomData,
                                                                                        newRoomDoorIndex,
                                                                                        missionStructureNode);

                // Check for collisions.
                if (!DungeonConstructionUtils.RoomCollidesWithExistingRoom(newRoomNode, _RoomFromTileDict))
                {
                    // Update the doorToConnectTo object so the calling function will have access to this new information.
                    doorToConnectTo.ThisRoom_Node = newRoomNode;
                    doorToConnectTo.ThisRoom_DoorIndex = newRoomDoorIndex;

                    break; // We found and fitted a new room into the dungeon successfully, so break out of this loop.
                }
                else
                {
                    //Debug.LogWarning($"DungeonGenerator.CreateRoom() - Attempt #{attempts}: Room \"{newRoomNode.RoomBlueprint.RoomName}\" collided with an existing room! Trying a different room.");
                }


                // If we've reached the max. number of attempts, give up and return null to ensure we don't get stuck in an infinite loop here.
                if (attempts >= MAX_ROOM_BLUEPRINT_SELECTION_ATTEMPTS)
                {
                    // Get the parent room's door we tried to snap a room to.
                    DungeonDoor parentDoor = doorToConnectTo.OtherRoom_Node.Doorways[(int)doorToConnectTo.OtherRoom_DoorIndex];

                    // Add the parent room's door to the global blocked doorways list, and remove it from the global unconnected doorways list.
                    // This way we won't try to connect a room here again.
                    MarkDoorAsUnavailable(parentDoor, true);

                    return null;
                }

            } // end while


            // Give the mission structure node a link to the room generated from it.
            missionStructureNode.DungeonRoomNode = newRoomNode;


            // Initialize the doorways list on the new room's node.
            InitRoomDoors(newRoomNode);

            // Create a DungeonDoor object with all the data needed to link up these doorways if this room is kept as part of the dungeon as it is now.
            DungeonDoor doorToLink = new DungeonDoor();
            doorToLink.ThisRoom_Node = doorToConnectTo.OtherRoom_Node;
            doorToLink.ThisRoom_DoorIndex = doorToConnectTo.OtherRoom_DoorIndex;
            doorToLink.OtherRoom_Node = newRoomNode;
            doorToLink.OtherRoom_DoorIndex = newRoomDoorIndex;
            doorsToLink.Add(doorToLink);


            return newRoomNode;

        }

        private static DungeonGraphNode CreateStartingRoom(uint minDoorsNeeded)
        {
            // Choose a random rotation direction for the room.
            Directions direction = (Directions)RNG_DungeonGen.RollRandomIntInRange(0, (int)Directions.West);

            // Choose a random floor for the start room to be on.
            RoomLevels floor;
            if (ForceStartRoomOnFirstFloor)
                floor = RoomLevels.Level_1stFloor;
            else
                floor = (RoomLevels)RNG_DungeonGen.RollRandomIntInRange(1, (int)RoomLevels.Level_2ndFloor); // We start the random number range at 1 rather than 0 since we don't want Level_AnyFloor to get selected.


            // Select a random starting room blueprint.
            RoomData roomBlueprint = SelectRandomRoomWithFilters(minDoorsNeeded, true, floor, true, RoomTypeFlags.CanBeStart);
            if (roomBlueprint == null)
                throw new Exception($"DungeonGenerator.CreateStartingRoom() - There are no starting room blueprints available on floor \"{floor}\"!");

            // Create the starting room.
            DungeonGraphNode startRoomNode = new DungeonGraphNode(null,
                                                                  roomBlueprint,
                                                                  Vector3Int.zero,
                                                                  direction,
                                                                  MissionStructureGraph.StartNode);


            // Give the mission structure node a link to the room generated from it.
            MissionStructureGraph.StartNode.DungeonRoomNode = startRoomNode;

            InitRoomDoors(startRoomNode);

            ConfigureEntranceOrExitRoomDoor(startRoomNode, true);

            return startRoomNode;
        }

        private static RoomTypeFlags GetRoomTypeFlagsFromMissionStructureNode(MissionStructureGraphNode missionStructureNode)
        {
            RoomTypeFlags flags = 0;
            switch (missionStructureNode.GrammarSymbol)
            {
                case GrammarSymbols.T_Goal:
                    flags = RoomTypeFlags.CanBeGoal;
                    break;
                case GrammarSymbols.T_Secret_Room:
                    flags = RoomTypeFlags.CanBeSecretRoom;
                    break;
                case GrammarSymbols.T_Test_Secret:
                    flags = RoomTypeFlags.PuzzleRoom;
                    break;
                case GrammarSymbols.T_Treasure_Key:
                    flags = RoomTypeFlags.CanHaveKey;
                    break;
                case GrammarSymbols.T_Treasure_Key_Multipart:
                    flags = RoomTypeFlags.CanHaveKey_Multipart;
                    break;
                case GrammarSymbols.T_Treasure_Key_Goal:
                    flags = RoomTypeFlags.CanHaveKey_Goal;
                    break;
                case GrammarSymbols.T_Treasure_Bonus:
                    flags = RoomTypeFlags.CanHaveTreasure;
                    break;

                default:
                    flags = RoomTypeFlags.GenericRoom;
                    break;
            }

            return flags;
        }

        /// <summary>
        /// Iterates through all unused doorways and for each one randomly adds a room or seals the doorway with wall tiles.
        /// </summary>
        private static void FinalizeUnusedDoorways()
        {
            _IsFinalizingDoors = true;

            List<DungeonDoor> unusedDoorways = new List<DungeonDoor>(_UnconnectedDoorways);

            foreach (DungeonDoor doorway in unusedDoorways)
            {
                float random = RNG_DungeonGen.RollRandomFloat_ZeroToOne();

                // Certain types of rooms are not allowed to have extra rooms attached, so check for those first.
                if (RoomIsTypeNotAllowedToHaveExtraRoomsAttached(doorway.ThisRoom_Node.MissionStructureNode.GrammarSymbol))
                {
                    _BlockedDoorways.Add(doorway);
                }                
                else if (random <= EXTRA_DOORS_ROOM_SPAWN_CHANCE) // Should we spawn a new door?
                {
                    // Create a mission structure node for the new room.
                    MissionStructureGraphNode structureNode = new MissionStructureGraphNode(GrammarSymbols.T_Secret_Room);

                    // Create a door object from the perspective of the new room by setting its parent room fields.
                    DungeonDoor newRoomDoor = new DungeonDoor();
                    newRoomDoor.OtherRoom_Node = doorway.ThisRoom_Node;
                    newRoomDoor.OtherRoom_DoorIndex = doorway.ThisRoom_DoorIndex;

                    // Generate a new room attached to this doorway.
                    DungeonGraphNode roomNode = SnapNewRoomIntoDungeon(structureNode, newRoomDoor);
                    if (roomNode == null)
                    {
                        MarkDoorAsUnavailable(doorway, true);

                        continue;
                    }

                    // Add this node to its parent's child nodes list.
                    MissionStructureGraphNode parentStructureNode = doorway.ThisRoom_Node.MissionStructureNode;
                    parentStructureNode.ChildNodesData.Add(new MSCNData(structureNode));

                    structureNode.LockCount = parentStructureNode.LockCount;

                    // Set the mission structure node's room node reference.
                    structureNode.DungeonRoomNode = roomNode;

                    // Add the new mission structure node into the structure graph.
                    MissionStructureGraph.AddNode(structureNode);

                    // Place the room in the tilemaps.
                    DungeonConstructionUtils.PlaceRoomTiles(DungeonTilemapManager, roomNode, _RoomFromTileDict);

                    // Add the door that connects to the new room into the blocked doors list so the wall will get changed to a bomb wall when we seal up all blocked doors.
                    MarkDoorAsUnavailable(newRoomDoor.OtherRoom_Node.Doorways[(int) newRoomDoor.OtherRoom_DoorIndex], true);
                }
                else
                {
                    // Debug.LogError($"Sealed off unused door[{doorway.ThisRoom_DoorIndex}] in room \"{doorway.ThisRoom_Node.RoomBlueprint.RoomName}\" - Room Center Point: {doorway.ThisRoom_Node.RoomCenterPoint}");

                    // Since we're not adding a room on this doorway, add it to the blocked doors list so it will be sealed up with
                    // wall tiles.
                    MarkDoorAsUnavailable(doorway, true);
                }

            } // end foreach doorway


            _UnconnectedDoorways.Clear();

        }

        private static bool CheckPossibleRoomPlacementDoorLayout(DungeonGraphNode roomNode, DungeonDoor doorToConnectTo, List<DungeonDoor> blockedDoors, List<DungeonDoor> chanceConnectedDoors, out int doorsLost)
        {
            int doorIndex = -1;

            doorsLost = 0;


            foreach (DoorData door in roomNode.RoomBlueprint.DoorsList)
            {
                blockedDoors.Clear();

                doorIndex++;

                // If this door is the one being snapped to a pre-existing room, then skip it.
                // This case would otherwise always cause a false positive since obviously there is a room there.
                if (doorIndex == doorToConnectTo.ThisRoom_DoorIndex)
                    continue;


                Vector3Int otherRoomDoor_Tile1Pos;
                LinearScanFromDoorResults result = PlaceholderUtils_Doors.DoLinearScanFromDoor(DungeonTilemapManager, 
                                                                                               roomNode, 
                                                                                               doorIndex,
                                                                                               out otherRoomDoor_Tile1Pos,
                                                                                               LINEAR_DOOR_SCAN_LENGTH);


                if (result != LinearScanFromDoorResults.Nothing)
                    Debug.LogWarning($"DungeonGenerator.CheckPossibleRoomPlacementDoorLayout() - Door[{doorIndex}] on room \"{roomNode.RoomBlueprint.RoomName} ({roomNode.MissionStructureNode.GrammarSymbol})\" got linear door scan result \"{result}\".");


                //Debug.LogError($"DoorScan: \"{roomNode.MissionStructureNode.GrammarSymbol}\"    \"{roomNode.RoomBlueprint.RoomName}\"    door[{doorIndex}] facing {roomNode.Doorways[doorIndex].ThisRoom_DoorAdjustedDirection}    {LINEAR_DOOR_SCAN_LENGTH}    \"{result}\"");
                if (result == LinearScanFromDoorResults.Collision)
                {
                    if (!DungeonDoor.ListContainsDoor(blockedDoors, roomNode.Doorways[doorIndex]))
                        blockedDoors.Add(roomNode.Doorways[doorIndex]);

                    doorsLost++;
                }
                else if (result == LinearScanFromDoorResults.ChanceConnection_MatchingFloor)
                {
                    // Check that this chance connection doesn't bypass a locked door.
                    DungeonDoor doorway = roomNode.Doorways[doorIndex];

                    // Get the data from the other room's door and copy it into this door.
                    // That way this door has all the data needed when we connect all the chance connected doorways via
                    // calls to ConfigureConnectingDoorway().
                    DungeonDoor otherRoomDoor = _DoorFromTileDict[otherRoomDoor_Tile1Pos];
                    doorway.OtherRoom_Node = otherRoomDoor.ThisRoom_Node;
                    doorway.OtherRoom_DoorIndex = otherRoomDoor.ThisRoom_DoorIndex;


                    // Check if the room we connected to by chance has any extra unused doors available beyond those needed for any of its tightly coupled neighbors that haven't been generated yet.
                    if (!doorway.OtherRoom_Node.HasUnusedDoorway())
                    {
                        Debug.LogWarning($"DungeonGenerator.CheckPossibleRoomPlacementDoorLayout() - Door[{doorIndex}] on room \"{roomNode.RoomBlueprint.RoomName}\" ({roomNode.MissionStructureNode.GrammarSymbol}) connected with a neighboring room \"{doorway.OtherRoom_Node.RoomBlueprint.RoomName}\" ({doorway.OtherRoom_Node.MissionStructureNode.GrammarSymbol}) by chance, but that room does not have any doors available for non-tightly coupled rooms!");

                        return false; // Tell the calling code that this room placement is invalid.
                    }


                    if (doorway.ThisRoom_Node.MissionStructureNode.LockCount != doorToConnectTo.OtherRoom_Node.MissionStructureNode.LockCount)
                    {
                        // Add the doorway to the chance connections list and increment the doors lost count since this room now has one less unused doorway.
                        chanceConnectedDoors.Add(doorway);
                        doorsLost++;

                        Debug.LogWarning($"DungeonGenerator.CheckPossibleRoomPlacementDoorLayout() - Door[{doorIndex}] on room \"{roomNode.RoomBlueprint.RoomName}\" ({roomNode.MissionStructureNode.GrammarSymbol}) connected with a neighboring room by chance, but this connection would bypass a locked door!");
                    }

                }
                else if (result == LinearScanFromDoorResults.ChanceConnection_FloorMismatch)
                {
                    Debug.LogWarning($"DungeonGenerator.CheckPossibleRoomPlacementDoorLayout() - Door[{doorIndex}] on room \"{roomNode.RoomBlueprint.RoomName}\" ({roomNode.MissionStructureNode.GrammarSymbol}) connected with a neighboring room by chance, but the two doors are not on the same floor!");

                    return false;
                }


            } // end foreach door


            return true;
        }

        private static List<DungeonDoor> _AttemptedDoors = new List<DungeonDoor>();
        private static List<DungeonDoor> _AvailableDoors = new List<DungeonDoor>();
        private static DungeonDoor FindDoorToConnectNewRoomTo(MissionStructureGraphNode missionStructureNode)
        {
            DungeonDoor doorToConnectTo;
            DungeonGraphNode parentRoomNode;


            // Get the parent room of this room, if there is one. If there isn't, then this is the dungeon's entrance room.
            _ParentDictionary.TryGetValue(missionStructureNode, out parentRoomNode);

            if (parentRoomNode == null)
                throw new Exception($"DungeonGenerator.FindDoorToConnectNewRoomTo() - The parent node is null!");


            _AvailableDoors.Clear();


            if (parentRoomNode.MissionStructureNode.ContainsTightlyCoupledChild(missionStructureNode) ||
                RoomIsTypeRequiringDirectCouplingOfTightlyCoupledChildren(parentRoomNode.MissionStructureNode.GrammarSymbol))
            {
                List<DungeonDoor> unconnectedParentRoomDoors = parentRoomNode.GetUnconnectedDoors();
                if (unconnectedParentRoomDoors.Count < 1)
                {
                    Debug.LogError($"DungeonGenerator.FindDoorToConnectNewRoomTo() - The parent room \"{parentRoomNode.RoomBlueprint.RoomName}\" ({parentRoomNode.MissionStructureNode.GrammarSymbol}) has no unconnected doors available!");
                    return null;
                }


                // Get a list of available doors.
                foreach (DungeonDoor door in unconnectedParentRoomDoors)
                {
                    if (!_AttemptedDoors.Contains(door))
                        _AvailableDoors.Add(door);
                }

                if (_AvailableDoors.Count < 1)
                {
                    Debug.LogError($"There are no doors available to connect the tightly coupled room \"{missionStructureNode.GrammarSymbol}\" to!");
                    return null;
                }


                // Select a random unconnected door on the parent room to connect the new room to.
                // NOTE: This function returns a new DungeonDoor object with the This* and Other* fields swapped.
                //       The reason is that this DungeonDoor object is being created for the door in the room we're adding, so we have to flip its perspective to that room.
                doorToConnectTo = SelectRandomDoorFromList(_AvailableDoors, out int doorIndex);

                // NOTE: The following line is using availableDoors[doorIndex] intentionally, rather than doorToConnectTo for the reason mentioned in the comments above.
                _AttemptedDoors.Add(_AvailableDoors[doorIndex]);
                return doorToConnectTo;
            }
            else
            {            
                int attempts = 0;
                GrammarSymbols symbol = missionStructureNode.GrammarSymbol;

                bool isLockRoom = (symbol == GrammarSymbols.T_Lock || symbol == GrammarSymbols.T_Lock_Multi || symbol == GrammarSymbols.T_Lock_Goal);
                int offset = isLockRoom ? 1 : 0;
                int lockNumber = (int) missionStructureNode.LockCount - offset;
                lockNumber = Math.Max(0, lockNumber);


                // Get a list of available doors.
                if (!_DoorsBehindLocks.TryGetValue(lockNumber, out List<DungeonDoor> tempList))
                {
                    Debug.LogError($"There are no doors available with the required lock number to connect the room \"{missionStructureNode.GrammarSymbol}\" to!");
                    return null;
                }

                foreach (DungeonDoor door in tempList)
                {
                    if (!_AttemptedDoors.Contains(door))
                        _AvailableDoors.Add(door);
                }

                if (_AvailableDoors.Count < 1)
                {
                    Debug.LogError($"There are no doors available to connect the room \"{missionStructureNode.GrammarSymbol}\" to!");
                    return null;
                }


                while (true)
                {
                    //Debug.LogWarning($"Remaining: {_AvailableDoors.Count}");

                    // Select a random unconnected door to connect the new room to.
                    // NOTE: This function returns a new DungeonDoor object with the This* and Other* fields swapped.
                    //       The reason is that this DungeonDoor object is being created for the door in the room we're adding, so we have to flip its perspective to that room.
                    doorToConnectTo = SelectRandomDoorFromList(_AvailableDoors, out int doorIndex);

                    // Debug.LogWarning($"Parent: {parentRoomNode.MissionStructureNode.GrammarSymbol}: {parentRoomNode.MissionStructureNode.LockCount}    This: {missionStructureNode.GrammarSymbol}: {lockNumber + offset}    Offset: {offset}");

                    DungeonDoor originalDoorObject = _AvailableDoors[doorIndex];
                    _AvailableDoors.Remove(originalDoorObject);


                    if (missionStructureNode != MissionStructureGraph.StartNode)
                    {
                        // Check that the lockNumber for this door's parent room is not greater than that of the new room.
                        if (lockNumber >= originalDoorObject.ThisRoom_Node.MissionStructureNode.LockCount)
                        {
                            if (originalDoorObject.ThisRoom_Node.HasUnusedDoorway())
                            {
                                _AttemptedDoors.Add(originalDoorObject);
                                return doorToConnectTo;
                            }
                        }
                    }


                    if (_AvailableDoors.Count < 1)
                    {
                        Debug.LogError($"Ran out of doors to attempt to connect the room \"{missionStructureNode.GrammarSymbol}\" to!");
                        break;
                    }    

                    attempts++;
                    if (attempts > MAX_FIND_DOOR_ATTEMPTS)
                    {                    
                        Debug.LogError($"Failed to find a door to connect the room \"{missionStructureNode.GrammarSymbol}\" to! No more retry attempts left.");
                        break;
                    }


                } // end while

            }


            // This only runs if the while loop above fails.
            return null;

        }

        private static void InitRoomDoors(DungeonGraphNode room)
        {
            // Create a connection struct for each door to track connections to this room.
            for (int i = 0; i < room.RoomBlueprint.DoorsList.Count; i++)
            {
                DungeonDoor door = room.Doorways[i];

                door.ThisRoom_Node = room;
                door.ThisRoom_DoorIndex = (uint)i;

                DoorData data = room.RoomBlueprint.DoorsList[i];


                if (_DoorFromTileDict.ContainsKey(door.ThisRoom_DoorTile1WorldPosition))
                    throw new Exception($"DungeonGenerator.InitRoomDoors() - Couldn't register tile 1 of door (index={i}) in room \"{room.RoomBlueprint.RoomName}\" in the tracking dictionary because one is already registered at this position {door.ThisRoom_DoorTile1WorldPosition}!");
                else if (_DoorFromTileDict.ContainsKey(door.ThisRoom_DoorTile2WorldPosition))
                    throw new Exception($"DungeonGenerator.InitRoomDoors() - Couldn't register tile 2 of door (index={i}) in room \"{room.RoomBlueprint.RoomName}\" in the tracking dictionary because one is already registered at this position {door.ThisRoom_DoorTile2WorldPosition}!");



                //Debug.Log($"Room \"{room.RoomBlueprint.RoomName}\"    Door[{i}]:  Tile1: {tile1AdjustedPos}  Tile2: {tile2AdjustedPos}");


                // Register them in the dictionary that associates tiles with the doors that occupy them.
                _DoorFromTileDict.Add(door.ThisRoom_DoorTile1WorldPosition, door);
                _DoorFromTileDict.Add(door.ThisRoom_DoorTile2WorldPosition, door);


                // Add this door to the available doors and unconnected doors lists.
                int lockNNumber = (int) room.MissionStructureNode.LockCount;
                if (!_DoorsBehindLocks.ContainsKey(lockNNumber))
                    _DoorsBehindLocks[lockNNumber] = new List<DungeonDoor>();

                _DoorsBehindLocks[lockNNumber].Add(door);
                _UnconnectedDoorways.Add(door);


            } // end for i

        }

        private static bool ConfigureEntranceOrExitRoomDoor(DungeonGraphNode room, bool isEntranceDoor)
        {
            // Find the entry/exit door.
            int doorIndex = -1;

            for (int i = 0; i < room.RoomBlueprint.DoorsList.Count; i++)
            {
                if (room.RoomBlueprint.DoorsList[i].IsEntryOrExitDoor)
                {
                    doorIndex = i;
                    break;
                }
            }

            if (doorIndex < 0)
                throw new Exception("DungeonGenerator.ConfigureEntranceOrExitDoor() - The specified start or end room of the dungeon does not contain an entry/exit door!");



            // Do a linear scan outside the door to see if it collides with a wall or another door.
            Vector3Int otherDoor_Tile1WorldPos;
            LinearScanFromDoorResults result = PlaceholderUtils_Doors.DoLinearScanFromDoor(DungeonTilemapManager,
                                                                                            room,
                                                                                            doorIndex,
                                                                                            out otherDoor_Tile1WorldPos,
                                                                                            LINEAR_DOOR_SCAN_LENGTH);

            // If the door scan didn't find anything in the way, then break out of this loop and setup this door as the entry or exit door.
            if (result != LinearScanFromDoorResults.Nothing)
            {
                Debug.LogError($"DungeonGenerator.SelectGoalRoomExitDoor() - Failed to setup the entry/exit door! Something is blocking it!");
                return false;
            }


            // Get a reference to the entrance or exit door.
            DungeonDoor door = room.Doorways[doorIndex];
            door.ThisRoom_Node = room;
            door.ThisRoom_DoorIndex = (uint)doorIndex;

            // Set the selected door as the entrance or exit door.
            door.Flags = isEntranceDoor ? DungeonDoorFlags.IsEntranceDoor : DungeonDoorFlags.IsGoalDoor;

            // Remove this door from the available doors and unconnected doors lists.
            MarkDoorAsUnavailable(door, false);


            if (isEntranceDoor)
            {
                // Position the player next to the entrance door.
                DungeonConstructionUtils.PositionPlayer(DungeonTilemapManager, room, door);
                _EntranceDoor = door;
            }
            else
            {
                _GoalDoor = door;
            }

            // Register some fake tiles outside the door as part of this room. This 2-wide strip of
            // fake tiles extending away from the outside of the door will make any room that tries
            // to generate outside the door think it collided with this room. That way no rooms will
            // spawn outside the entrance or exit doors.
            DungeonConstructionUtils.RegisterFakeTilesOutsideDoor(room, door, _RoomFromTileDict, _FakeRoomTiles);


            return true;
        }

        private static void ConfigureConnectingDoorway(DungeonDoor doorData)
        {
            DungeonGraphNode parentRoomNode = doorData.ThisRoom_Node;
            uint parentRoomDoorIndex = doorData.ThisRoom_DoorIndex;
            DungeonGraphNode newRoomNode = doorData.OtherRoom_Node;
            uint newRoomDoorIndex = doorData.OtherRoom_DoorIndex;


            // Get the parent node's doorway object.
            DungeonDoor doorToEdit = doorData.ThisRoom_Node.Doorways[(int)parentRoomDoorIndex];

            // Setup the fields that link it to the child room door.
            doorToEdit.ThisRoom_Node = parentRoomNode;
            doorToEdit.ThisRoom_DoorIndex = parentRoomDoorIndex;
            doorToEdit.OtherRoom_Node = newRoomNode;
            doorToEdit.OtherRoom_DoorIndex = newRoomDoorIndex;
            if (parentRoomNode.MissionStructureNode.ContainsTightlyCoupledChild(newRoomNode.MissionStructureNode))
                doorToEdit.IsTightlyCoupledRoomConnection = true;


            // Get the child room's doorway object.
            doorToEdit = newRoomNode.Doorways[(int)newRoomDoorIndex];

            // Setup the fields that link it to the parent room door.
            doorToEdit.ThisRoom_Node = newRoomNode;
            doorToEdit.ThisRoom_DoorIndex = newRoomDoorIndex;
            doorToEdit.OtherRoom_Node = parentRoomNode;
            doorToEdit.OtherRoom_DoorIndex = parentRoomDoorIndex;


            // Remove each room's door from the available doors and unconnected doors lists since they are no longer unconnected.
            MarkDoorAsUnavailable(parentRoomNode.Doorways[(int)parentRoomDoorIndex], false);
            MarkDoorAsUnavailable(newRoomNode.Doorways[(int)newRoomDoorIndex], false);

        }

        private static void CleanupRejectedRoomData(DungeonGraphNode roomNode)
        {
            // Remove the rejected room's doors from any lists they may have been added to.
            foreach (DungeonDoor door in roomNode.Doorways)
            {
                MarkDoorAsUnavailable(door, false);

                _DoorFromTileDict.Remove(door.ThisRoom_DoorTile1WorldPosition);
                _DoorFromTileDict.Remove(door.ThisRoom_DoorTile2WorldPosition);

                if (door.OtherRoom_Node != null)
                {
                    foreach (DungeonDoor otherDoor in door.OtherRoom_Node.Doorways)
                    {
                        if (otherDoor.OtherRoom_Node == roomNode)
                        {
                            otherDoor.OtherRoom_Node = null;
                            otherDoor.OtherRoom_DoorIndex = 0;
                        }
                    } // end foreach otherDoor

                }

            } // end foreach door

        }

        private static DungeonDoor SelectRandomDoorFromList(List<DungeonDoor> doorsList, out int doorIndex)
        {
            DungeonDoor doorToConnectTo = new DungeonDoor();


            doorIndex = RNG_DungeonGen.RollRandomIntInRange(0, doorsList.Count - 1);

            doorToConnectTo.OtherRoom_Node = doorsList[doorIndex].ThisRoom_Node;
            doorToConnectTo.OtherRoom_DoorIndex = doorsList[doorIndex].ThisRoom_DoorIndex;


            return doorToConnectTo;
        }

        private static int SelectRandomDoorOnRoomAndFloor(RoomData room, RoomLevels doorLevel)
        {
            List<DoorData> doorsOnDesiredLevel = new List<DoorData>();

            foreach (DoorData door in room.DoorsList)
            {
                if (door.DoorLevel == doorLevel || doorLevel == RoomLevels.Level_AnyFloor)
                    doorsOnDesiredLevel.Add(door);
            }


            if (doorsOnDesiredLevel.Count < 1)
                throw new Exception($"DungeonGenerator.SelectRandomDoor() - The specified room \"{room.RoomName}\" has no doors on the floor \"{doorLevel}\"!");

            // Select a random door from the list of those that are on the desired floor.
            int doorIndex = RNG_DungeonGen.RollRandomIntInRange(0, doorsOnDesiredLevel.Count - 1);



            // Return the selected door's data.
            return room.DoorsList.IndexOf(doorsOnDesiredLevel[doorIndex]);
        }

        private static void MarkDoorAsUnavailable(DungeonDoor doorway, bool markAsBlocked)
        {
            int lockNumber = (int)  doorway.ThisRoom_Node.MissionStructureNode.LockCount;
            _DoorsBehindLocks[lockNumber].Remove(doorway);
            

            _UnconnectedDoorways.Remove(doorway);


            if (markAsBlocked &&
                (!DungeonDoor.ListContainsDoor(_BlockedDoorways, doorway)))
            {
                _BlockedDoorways.Add(doorway);
            }

        }
        
        private static bool RoomIsTypeNotAllowedToHaveExtraRoomsAttached(GrammarSymbols symbol)
        {
            if (symbol == GrammarSymbols.T_Boss_Mini || symbol == GrammarSymbols.T_Boss_Main)
            {
                return true;
            }

            return false;
        }        

        private static bool RoomIsTypeRequiringDirectCouplingOfTightlyCoupledChildren(GrammarSymbols symbol)
        {
            if (symbol == GrammarSymbols.T_Boss_Mini || symbol == GrammarSymbols.T_Boss_Main ||
                symbol == GrammarSymbols.T_Lock || symbol == GrammarSymbols.T_Lock_Multi || symbol == GrammarSymbols.T_Lock_Goal)
            {
                return true;
            }

            return false;
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
                    bool startOrExitFilterPassed = false;
                    uint roomFlags = (uint)room.RoomTypeFlags;
                    uint newRoomFlags = (uint) roomTypeFlags;
                    uint startEndFlags = (uint) RoomTypeFlags.CanBeStart | (uint) RoomTypeFlags.CanBeGoal;

                    // Only allow start or exit rooms if the appropriate flag is specified.
                    if (!Flags.HasAnyFlags(roomFlags, startEndFlags))
                    {
                        // The room is not the start or goal room, so it passes this filter.
                        startOrExitFilterPassed = true;
                    }                    
                    else if (filterByRoomType &&
                             (Flags.HasAllFlags(roomFlags, (uint) RoomTypeFlags.CanBeStart) && Flags.HasAllFlags(newRoomFlags, (uint) RoomTypeFlags.CanBeStart)) ||
                             (Flags.HasAllFlags(roomFlags, (uint) RoomTypeFlags.CanBeGoal) && Flags.HasAllFlags(newRoomFlags, (uint) RoomTypeFlags.CanBeGoal)))
                    {
                        // Filter by room type is on and the room has the appropriate flags.
                        startOrExitFilterPassed = true;
                    }


                    bool levelFilterPassed = false;
                    if (roomLevel == RoomLevels.Level_AnyFloor)
                        levelFilterPassed = true;
                    else if (room.RoomLevel == roomLevel)
                        levelFilterPassed = true;


                    bool roomTypeFlagsFilterPassed = false;
                    if (!filterByRoomType)
                        roomTypeFlagsFilterPassed = true;
                    else if ((newRoomFlags == 0 && roomFlags == 0) || // Are we looking for a room with no flags?
                             (newRoomFlags > 0 && Flags.HasAllFlags(roomFlags, newRoomFlags))) // If we are looking for a room with flags, ones with no flags are excluded.
                    {
                        roomTypeFlagsFilterPassed = true;
                    }


                    if (greaterThanOrEqual)
                    {
                        if (room.DoorsList.Count >= doorCount && startOrExitFilterPassed && levelFilterPassed && roomTypeFlagsFilterPassed)
                            list.Add(room);
                    }
                    else
                    {
                        if (room.DoorsList.Count == doorCount && startOrExitFilterPassed && levelFilterPassed && roomTypeFlagsFilterPassed)
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
            int roomIndex = RNG_DungeonGen.RollRandomIntInRange(0, list.Count - 1);

            // Return the selected room blueprint.
            // I added this if statement, because it crashed when my test room set had no start rooms on the 2nd floor. So now it returns null in this case.
            if (list.Count == 0)
            {
                if (!filterByRoomType)
                    throw new Exception($"DungeonGenerator.CreateRoom() - Failed to create a new room, because no room blueprint with {(greaterThanOrEqual ? ">=" : "==")} {doorCount} doors was found on floor {roomLevel}!");
                else
                    throw new Exception($"DungeonGenerator.CreateRoom() - Failed to create a new room, because no room blueprint with {(greaterThanOrEqual ? ">=" : "==")} {doorCount} doors was found on floor {roomLevel} with room type flags [{roomTypeFlags}]!");

                //return null;
            }
            else
            {
                return list[roomIndex];
            }

        }


    }

}