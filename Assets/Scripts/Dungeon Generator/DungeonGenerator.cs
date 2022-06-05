
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
using MSCNData = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.MissionStructureChildNodeData;


namespace ProceduralDungeon.DungeonGeneration
{
    public static class DungeonGenerator
    {
        private const float EXTRA_DOORS_ROOM_SPAWN_CHANCE = 0.25f; // The probability that each unused doorway will have a new room spawned next to it after the main dungeon generation is done.
        private const int LINEAR_DOOR_SCAN_LENGTH = 5; // How many tiles to scan in front of a door for room collisions and neighboring doors.
        private const int MAX_ROOM_BLUEPRINT_SELECTION_ATTEMPTS = 16; // Max. number of times to try choosing and placing a room blueprint before aborting.
        private const int MAX_ROOM_CONNECTION_ATTEMPTS = 16; // Max. number of times to try connecting a new room into one of the unconnected doors on the dungeon.


        private static DungeonTilemapManager _DungeonTilemapManager;


        private static DungeonGraph _DungeonGraph; // Holds the layout information of the dungeon used to build it with tiles.
        private static MissionStructureGraph _MissionStructureGraph; // Holds the mission structure information of the dungeon, such as locations of things like keys, locks, or bosses.


        private static Dictionary<MissionStructureGraphNode, DungeonGraphNode> _ParentDictionary;


        private static DungeonDoor _EntranceDoor;
        private static DungeonDoor _GoalDoor;


        private static bool _IsInitialized = false;
        private static bool _IsGeneratingDungeon = false;
        private static bool _IsFinalizingDoors = false;


        // References to room prefabs.
        private static List<RoomData> _AllRooms;
        private static Dictionary<uint, List<RoomData>> _FilteredRoomListsCache;


        private static NoiseRNG _RNG_Seed = null;
        private static NoiseRNG _RNG_MissionStructureGen = null;
        private static NoiseRNG _RNG_DungeonGen = null;


        private static Dictionary<Vector3Int, DungeonDoor> _DoorFromTileDict; // Associates tile positions with doors that are occupying them.
        private static Dictionary<Vector3Int, DungeonGraphNode> _RoomFromTileDict; // Associates tile positions with rooms that are occupying them.


        private static List<DungeonDoor> _BlockedDoorways;
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


            if (!_IsInitialized)
            {
                _IsInitialized = false;

                // Move these lists into a theme object later that holds all rooms from a certain set?
                // This would be necessary if I make this generator able to incorporate multiple themes into one dungeon (like cave and brick rooms or something);
                _AllRooms = new List<RoomData>();
                _FilteredRoomListsCache = new Dictionary<uint, List<RoomData>>();

                _DoorFromTileDict = new Dictionary<Vector3Int, DungeonDoor>();
                _RoomFromTileDict = new Dictionary<Vector3Int, DungeonGraphNode>();

                _ParentDictionary = new Dictionary<MissionStructureGraphNode, DungeonGraphNode>();

                _BlockedDoorways = new List<DungeonDoor>();
                _UnconnectedDoorways = new List<DungeonDoor>();
            }


            _IsFinalizingDoors = false;
            _IsGeneratingDungeon = false;

            ClearPreviousData();

            _DungeonTilemapManager = manager;


            LoadRoomsData();
            PreprocessRoomsData();

            _IsInitialized = true;
        }

        private static void ClearPreviousData()
        {
            _AllRooms.Clear();

            _DoorFromTileDict.Clear();
            _RoomFromTileDict.Clear();

            _BlockedDoorways.Clear();
            _UnconnectedDoorways.Clear();

            _EntranceDoor = null;
            _GoalDoor = null;
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


            // 3659200998 : Door into wall seeds
            // 3659201262
            // 3659201572 : Doors mismatched seed (two doors end up touching, but are not on the same floor as each other)
            // 3659388979
            // 3659392483
            // 3659556263
            // 3659563007
            // 3659637319
            // 3659734668
            // 3659799015

            // Init the random number generators.
            InitRNG(3660324596);
            Debug.Log($"SEED: {_RNG_Seed.GetSeed()}");


            // Generate the mission structure graph.           
            _MissionStructureGraph = MissionStructureGraph.Generate(_RNG_MissionStructureGen);


            BuildDungeonRooms();
            FinalizeUnusedDoorways();
            DungeonConstructionUtils.SealOffBlockedDoors(_DungeonTilemapManager, _BlockedDoorways, _RNG_DungeonGen);

            // Reset the node positions so the ones added by FinalizedUnusedDoorways() for extra rooms get positions assigned.
            // This is only needed so they show up properly in the Unity Editor when the MissionStructureGraphGizmos script is not set to snap nodes to their generated dungeon rooms.
            GrammarRuleProcessor.SetPositions();


            _DungeonTilemapManager.DungeonMap.CompressBoundsOfAllTileMaps();

            DungeonPopulator.PopulateDungeon(_DungeonGraph, _RNG_DungeonGen);

            _IsGeneratingDungeon = false;

        }

        public static void BuildDungeonRooms()
        {
            MissionStructureGraphNode curStructureNode = null;
            Queue<MissionStructureGraphNode> nodeQueue = new Queue<MissionStructureGraphNode>();


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
                    Debug.LogError($"DungeonGenerator.CreateStartingRoom() - Encountered non-terminal node \"{curStructureNode.GrammarSymbol}\" in the mission structure graph! This node will be ignored.");
                    continue;
                }


                DungeonGraphNode roomNode = SnapNewRoomIntoDungeon(curStructureNode);


                // Place the room in the tilemaps.
                DungeonConstructionUtils.PlaceRoomTiles(_DungeonTilemapManager, roomNode, _RoomFromTileDict);



                // We are getting a prioritized child node list here because we want to queue the tightly coupled nodes
                // first since they need to be connected to the parent. That way, we don't waste doors on the parent room
                // by connecting ones that aren't tightly coupled to it.
                foreach (MSCNData childNodeData in curStructureNode.GetPrioritizedChildNodeList())
                {
                    MissionStructureGraphNode childNode = childNodeData.ChildNode;

                    // If this child node is not tightly coupled to this node, then we need to check if it is tightly
                    // coupled to any other node. If so, we need to skip it here so it is generated just after the parent
                    // node it is tightly coupled to. This is necessary if there are two branches in the mission structure,
                    // and the child node is tightly coupled to a node in the longer branch that is deeper in the tree
                    // than the current node is. This way we ensure it is always after that parent in the dungeon layout
                    // as it should be.
                    if ((!curStructureNode.GetChildNodeData(childNode).IsTightlyCoupled) && // Check that the child is not tightly coupled to this node
                         _MissionStructureGraph.IsTightlyCoupledToAnyNode(childNode))       // Check if it is tightly coupled to any other node
                    {
                        continue;
                    }


                    // If the child node is already in the dictionary, skip it.
                    if (_ParentDictionary.ContainsKey(childNode))
                        continue;


                    // Add the child node and its parent into the parent association table.
                    _ParentDictionary.Add(childNode, roomNode);


                    // Enqueue the child node so we can build a room from it in a future iteration of this loop.
                    nodeQueue.Enqueue(childNode);

                } // end foreach childNode


                if (curStructureNode.GrammarSymbol == GrammarSymbols.T_Goal)
                    _DungeonGraph.GoalRoomNode = curStructureNode.DungeonRoomNode;

                Debug.Log($"Generated room \"{curStructureNode.GrammarSymbol}\"");


                // This debug code limits the generator to only generating n rooms before it stops and calls FinalizeUnusedDoorways().
                //if (roomCount == 10)
                //    break;


            } // while nodeQueue is not empty

        }

        private static DungeonGraphNode SnapNewRoomIntoDungeon(MissionStructureGraphNode missionStructureNode, DungeonDoor doorToConnectTo = null)
        {
            int attempts = 0;
            List<DungeonDoor> blockedDoors = new List<DungeonDoor>(); // Holds doors that are blocked
            List<DungeonDoor> doorsToLink = new List<DungeonDoor>(); // Holds doors that connected by chance.
            DungeonGraphNode roomNode = null;


            // Attempt to add a new room to the dungeon.
            while (true)
            {
                if (attempts >= MAX_ROOM_CONNECTION_ATTEMPTS)
                    throw new Exception("DungeonGenerator.SnapNewRoomIntoDungeon() - Failed to connect another room onto the dungeon! No more retry attempts left.");


                attempts++;

                blockedDoors.Clear();
                doorsToLink.Clear();


                // Try to find an existing door to connect a new room to.
                if (!_IsFinalizingDoors)
                {
                    doorToConnectTo = FindDoorToConnectNewRoomTo(missionStructureNode);

                    if (missionStructureNode != _MissionStructureGraph.StartNode)
                    {
                        // If the room we're trying to connect doesn't have any extra doorways left for connections after its tightly coupled child nodes get connected,
                        // and if the current node is not a tightly coupled child of that node, then jump back to the start of the while loop to try again since we
                        // can't connect the new room to this room.
                        if (doorToConnectTo.OtherRoom_Node.HasUnusedDoorway() == false &&
                            !doorToConnectTo.OtherRoom_Node.MissionStructureNode.ContainsTightlyCoupledChild(missionStructureNode))
                            continue;

                    }

                }


                if (doorToConnectTo.OtherRoom_Node != null)
                {
                    //Debug.Log($"Room1: \"{doorToConnectTo.OtherRoom_Node.MissionStructureNode.GrammarSymbol}\"    R1DIndex: {doorToConnectTo.OtherRoom_DoorIndex}");
                }

                // Generate the DungeonGraphNode for the new room.
                roomNode = CreateRoom(missionStructureNode, doorToConnectTo, doorsToLink);               
                if (roomNode == null)
                {
                    //Debug.LogWarning($"DungeonGenerator.SnapNewRoomIntoDungeon() - Attempt #{attempts}: Failed to connect new room onto the dungeon!");

                    if (!_IsFinalizingDoors)
                        continue; // We didn't fit a room successfully, so jump to the top of this while loop to try again.
                    else
                        return null; // We were called by the FinalizeUnusedDoorways() function, then bail out since we're only trying to snap a room to a specific spot in this case.

                }


                // Check the door layout of the possible placement of this
                int doorsLost = 0;
                if (!CheckPossibleRoomPlacementDoorLayout(roomNode, doorToConnectTo, blockedDoors, doorsToLink, out doorsLost))
                {
                    CleanupRejectedRoomData(roomNode);

                    Debug.LogWarning($"DungeonGenerator.SnapNewRoomIntoDungeon() - Room \"{roomNode.RoomBlueprint.RoomName}\" connected to another room by chance, but it does not have enough doors left for non-tightly-coupled child rooms!");

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
                    // If this isn't the start room node, then add the new room node into the graph.
                    // Otherwise, create a new DungeonGraph with the start room node in it.
                    if (missionStructureNode != _MissionStructureGraph.StartNode)
                        _DungeonGraph.AddNode(roomNode);
                    else
                        _DungeonGraph = new DungeonGraph(roomNode);


                    // Copy any blocked doors into the global blocked doors list since we know this room is now finalized for placement in the dungeon.
                    // Also remove them from the global unconnected doors list.
                    foreach (DungeonDoor door in blockedDoors)
                    {
                        if (!DungeonDoor.ListContainsDoor(_BlockedDoorways, door))
                            _BlockedDoorways.Add(door);

                        _UnconnectedDoorways.Remove(door);
                    }


                    // Since we successsfully added a new room to the dungeon, exit this while loop to continue the dungeon construction process.
                    break;
                }
                else // We failed to create this room, so clear its data.
                {
                    CleanupRejectedRoomData(roomNode);

                    Debug.LogWarning($"DungeonGenerator.SnapNewRoomIntoDungeon() - Not enough doors left for tightly coupled child rooms after collisions detected on room \"{roomNode.RoomBlueprint.RoomName}\".");
                }

            } // end while


            // If this is the goal room, then assign one of the unused doors to be the exit door.
            if (missionStructureNode == _MissionStructureGraph.GoalNode)
                ConfigureEntranceOrExitRoomDoor(roomNode, false);

            // Connect any doors that need to be linked.
            // NOTE: The DungeonDoor objects in this list are not actual doors, but being used as data holders.
            foreach (DungeonDoor doorData in doorsToLink)
                ConfigureConnectingDoorway(doorData);


            return roomNode;

        }

        private static DungeonGraphNode CreateRoom(MissionStructureGraphNode missionStructureNode, DungeonDoor doorToConnectTo, List<DungeonDoor> doorsToLink)
        {
            if (missionStructureNode.GrammarSymbol != GrammarSymbols.T_Entrance && doorToConnectTo.OtherRoom_Node == null)
                throw new Exception("DungeonGenerator.CreateRoom() - The parentRoom parameter is null! All dungeon rooms must have a parent except the starting room.");


            DungeonGraphNode newRoomNode = null;
            if (missionStructureNode == _MissionStructureGraph.StartNode)
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
                minDoorsNeeded = Mathf.Max(minDoorsNeeded, 2);

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


            RoomData newRoomData = null;
            uint newRoomDoorIndex = 0;
            int attempts = 0;
            while (true)
            {
                attempts++;


                // Select a random room on the same level as that door.
                RoomTypeFlags filterFlags = GetRoomTypeFlagsFromMissionStructureNode(missionStructureNode);
                if (filterFlags == 0)
                    newRoomData = SelectRandomRoomWithFilters((uint)minDoorsNeeded, greaterThanOrEqual, parentDoorLevel);
                else
                    newRoomData = SelectRandomRoomWithFilters((uint)minDoorsNeeded, greaterThanOrEqual, parentDoorLevel, true, filterFlags);


                // Select a random door on the new room to connect to the existing door we already chose.
                newRoomDoorIndex = (uint)SelectRandomDoorOnRoom(newRoomData, parentDoorLevel);


                // Create the new room and connect it to its parent.
                newRoomNode = DungeonConstructionUtils.CreateNewRoomConnectedToPrevious(doorToConnectTo.OtherRoom_Node,
                                                                                        doorToConnectTo.OtherRoom_DoorIndex,
                                                                                        newRoomData,
                                                                                        (uint)newRoomDoorIndex,
                                                                                        missionStructureNode);

                // Check for collisions.
                if (!DungeonConstructionUtils.RoomCollidesWithExistingRoom(newRoomNode, _RoomFromTileDict))
                {
                    // Update the doorToConnectTo object so the calling function will have access to this new information.
                    doorToConnectTo.ThisRoom_Node = newRoomNode;
                    doorToConnectTo.ThisRoom_DoorIndex = (uint)newRoomDoorIndex;

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
                    _UnconnectedDoorways.Remove(parentDoor);
                    if (!DungeonDoor.ListContainsDoor(_BlockedDoorways, parentDoor))
                        _BlockedDoorways.Add(parentDoor);


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
            Directions direction = (Directions)_RNG_DungeonGen.RollRandomIntInRange(0, (int)Directions.West);

            // Choose a random floor for the start room to be on.
            RoomLevels floor = (RoomLevels)_RNG_DungeonGen.RollRandomIntInRange(1, (int)RoomLevels.Level_2ndFloor); // We start the random number range at 1 rather than 0 since we don't want Level_AnyFloor to get selected.

            // Select a random starting room blueprint.
            RoomData roomBlueprint = SelectRandomRoomWithFilters(minDoorsNeeded, true, floor, true, RoomTypeFlags.CanBeStart);
            if (roomBlueprint == null)
                throw new Exception($"DungeonGenerator.CreateStartingRoom() - There are no starting room blueprints available on floor \"{floor}\"!");

            // Create the starting room.
            DungeonGraphNode startRoomNode = new DungeonGraphNode(null,
                                                                  roomBlueprint,
                                                                  Vector3Int.zero,
                                                                  direction,
                                                                  _MissionStructureGraph.StartNode);


            // Give the mission structure node a link to the room generated from it.
            _MissionStructureGraph.StartNode.DungeonRoomNode = startRoomNode;


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
                case GrammarSymbols.T_Treasure_Key:
                    flags = RoomTypeFlags.CanHaveKey;
                    break;
                case GrammarSymbols.T_Treasure_Key_Multipart:
                    flags = RoomTypeFlags.CanHaveKey_Multipart;
                    break;
                case GrammarSymbols.T_Treasure_Key_Goal:
                    flags = RoomTypeFlags.CanHaveKey_Goal;
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
                float random = _RNG_DungeonGen.RollRandomFloat_ZeroToOne();

                // Certain types of rooms are not allowed to have extra rooms attached, so check for those first.
                if (doorway.ThisRoom_Node.MissionStructureNode.GrammarSymbol == GrammarSymbols.T_Boss_Mini ||
                    doorway.ThisRoom_Node.MissionStructureNode.GrammarSymbol == GrammarSymbols.T_Boss_Main ||
                    doorway.ThisRoom_Node.MissionStructureNode.GrammarSymbol == GrammarSymbols.T_Treasure_Bonus)
                {
                    _BlockedDoorways.Add(doorway);
                }                
                else if (random <= EXTRA_DOORS_ROOM_SPAWN_CHANCE) // Should we spawn a new door?
                {
                    // Create a mission structure node for the new room.
                    MissionStructureGraphNode structureNode = new MissionStructureGraphNode(GrammarSymbols.T_Test_Combat);

                    // Create a door object from the perspective of the new room by setting its parent room fields.
                    DungeonDoor newRoomDoor = new DungeonDoor();
                    newRoomDoor.OtherRoom_Node = doorway.ThisRoom_Node;
                    newRoomDoor.OtherRoom_DoorIndex = doorway.ThisRoom_DoorIndex;

                    // Generate a new room attached to this doorway.
                    DungeonGraphNode roomNode = SnapNewRoomIntoDungeon(structureNode, newRoomDoor);
                    if (roomNode == null)
                    {
                        if (!DungeonDoor.ListContainsDoor(_BlockedDoorways, doorway))
                            _BlockedDoorways.Add(doorway);

                        _UnconnectedDoorways.Remove(doorway);

                        continue;
                    }

                    // Add this node to its parent's child nodes list.
                    MissionStructureGraphNode parentStructureNode = doorway.ThisRoom_Node.MissionStructureNode;
                    parentStructureNode.ChildNodesData.Add(new MSCNData(structureNode));

                    structureNode.LockCount = parentStructureNode.LockCount;

                    // Set the mission structure node's room node reference.
                    structureNode.DungeonRoomNode = roomNode;

                    // Add the new mission structure node into the structure graph.
                    _MissionStructureGraph.AddNode(structureNode);

                    // Place the room in the tilemaps.
                    DungeonConstructionUtils.PlaceRoomTiles(_DungeonTilemapManager, roomNode, _RoomFromTileDict);

                }
                else
                {
                    // Debug.LogError($"Sealed off unused door[{doorway.ThisRoom_DoorIndex}] in room \"{doorway.ThisRoom_Node.RoomBlueprint.RoomName}\" - Room Center Point: {doorway.ThisRoom_Node.RoomCenterPoint}");

                    // Since we're not adding a room on this doorway, add it to the blocked doors list so it will be sealed up with
                    // wall tiles.
                    if (!DungeonDoor.ListContainsDoor(_BlockedDoorways, doorway))
                    {
                        _BlockedDoorways.Add(doorway);

                        _UnconnectedDoorways.Remove(doorway);
                    }
                    else
                    {
                        Debug.LogError("DungeonGenerator.FinalizeUnusedDoorways() - Duplicate door detected in blocked doors list!");
                    }
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
                LinearScanFromDoorResults result = PlaceholderUtils_Doors.DoLinearScanFromDoor(_DungeonTilemapManager, 
                                                                                               roomNode, 
                                                                                               doorIndex,
                                                                                               out otherRoomDoor_Tile1Pos,
                                                                                               LINEAR_DOOR_SCAN_LENGTH);

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
                        return false; // Tell the calling code that this room placement is invalid.


                    if (doorway.ThisRoom_Node.MissionStructureNode.LockCount != doorToConnectTo.OtherRoom_Node.MissionStructureNode.LockCount)
                    {
                        // Add the doorway to the chance connections list and increment the doors lost count since this room now has one less unused doorway.
                        chanceConnectedDoors.Add(doorway);
                        doorsLost++;


                        Debug.LogWarning($"DungeonGenerator.SnapNewRoomIntoDungeon() - Door[{doorIndex}] on room \"{roomNode.RoomBlueprint.RoomName}\" connected with a neighboring room by chance, but this connection would bypass a locked door!");
                    }

                    // Add this chance connection to the global list to be fixed up later since it doesn't bypass any locked doors.
                    doorsLost++;
                }
                else if (result == LinearScanFromDoorResults.ChanceConnection_FloorMismatch)
                {
                    Debug.LogWarning($"DungeonGenerator.SnapNewRoomIntoDungeon() - Door[{doorIndex}] on room \"{roomNode.RoomBlueprint.RoomName}\" connected with a neighboring room by chance, but the two doors are not on the same floor!");

                    return false;
                }

                if (result != LinearScanFromDoorResults.Nothing)
                    Debug.LogWarning($"DungeonGenerator.SnapNewRoomIntoDungeon() - Door[{doorIndex}] on room \"{roomNode.RoomBlueprint.RoomName}\" got linear door scan result \"{result}\".");

            } // end foreach door


            return true;
        }

        private static DungeonDoor FindDoorToConnectNewRoomTo(MissionStructureGraphNode missionStructureNode)
        {
            int doorIndex = 0;
            DungeonDoor doorToConnectTo = new DungeonDoor();
            DungeonGraphNode parentRoomNode = null;


            // Get the parent room to this room, if there is one. If there isn't, then this is the dungeon's entrance room.
            _ParentDictionary.TryGetValue(missionStructureNode, out parentRoomNode);

            // If this mission structure node is tightly coupled to its parent, then connect it to the parent room.
            // Otherwise, connect it to a random unconnected door.
            if (parentRoomNode != null &&
                parentRoomNode.MissionStructureNode.GetChildNodeData(missionStructureNode).IsTightlyCoupled)
            {

                List<DungeonDoor> unconnectedParentRoomDoors = parentRoomNode.GetUnconnectedDoors();
                if (unconnectedParentRoomDoors.Count < 1)
                    throw new Exception($"DungeonGenerator.FindDoorToConnectNewRoomTo() - The parent room \"{parentRoomNode.RoomBlueprint.RoomName}\" has no unconnected doors available, and the child room is tightly coupled so it can't be added elsewhere!");

                // Select a random unconnected door on the parent room to connect the new room to.
                doorIndex = _RNG_DungeonGen.RollRandomIntInRange(0, unconnectedParentRoomDoors.Count - 1);

                doorToConnectTo.OtherRoom_Node = parentRoomNode;
                doorToConnectTo.OtherRoom_DoorIndex = unconnectedParentRoomDoors[doorIndex].ThisRoom_DoorIndex;
            }
            else
            {
                // Select a random unconnected door to connect the new room to.
                doorIndex = _RNG_DungeonGen.RollRandomIntInRange(0, _UnconnectedDoorways.Count - 1);

                // Set the parent room variable to the room that owns the randomly selected door.
                if (missionStructureNode != _MissionStructureGraph.StartNode)
                {
                    doorToConnectTo.OtherRoom_Node = _UnconnectedDoorways[doorIndex].ThisRoom_Node;
                    doorToConnectTo.OtherRoom_DoorIndex = _UnconnectedDoorways[doorIndex].ThisRoom_DoorIndex; // Get the index of the door within the parent room.
                }
            }


            return doorToConnectTo;

        }

        private static void ConfigureConnectingDoorway(DungeonDoor doorData)
        {
            DungeonGraphNode parentRoomNode = doorData.ThisRoom_Node;
            uint parentRoomDoorIndex = doorData.ThisRoom_DoorIndex;
            DungeonGraphNode newRoomNode = doorData.OtherRoom_Node;
            uint newRoomDoorIndex = doorData.OtherRoom_DoorIndex;


            // Get the parent node's doorway object.
            DungeonDoor doorToEdit = doorData.ThisRoom_Node.Doorways[(int) parentRoomDoorIndex];

            // Setup the fields that link it to the child room door.
            doorToEdit.ThisRoom_Node = parentRoomNode;
            doorToEdit.ThisRoom_DoorIndex = parentRoomDoorIndex;
            doorToEdit.OtherRoom_Node = newRoomNode;
            doorToEdit.OtherRoom_DoorIndex = newRoomDoorIndex;
            if (parentRoomNode.MissionStructureNode.ContainsTightlyCoupledChild(newRoomNode.MissionStructureNode))
                doorToEdit.IsTightlyCoupledRoomConnection = true;


            // Get the child room's doorway object.
            doorToEdit = newRoomNode.Doorways[(int) newRoomDoorIndex];

            // Setup the fields that link it to the parent room door.
            doorToEdit.ThisRoom_Node = newRoomNode;
            doorToEdit.ThisRoom_DoorIndex = newRoomDoorIndex;
            doorToEdit.OtherRoom_Node = parentRoomNode;
            doorToEdit.OtherRoom_DoorIndex = parentRoomDoorIndex;


            // Remove each room's door from the unconnected doors list since they are no longer unconnected.
            _UnconnectedDoorways.Remove(parentRoomNode.Doorways[(int) parentRoomDoorIndex]);
            _UnconnectedDoorways.Remove(newRoomNode.Doorways[(int) newRoomDoorIndex]);
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


                // Add this door to the room's connection's list, and also add it to the dungeon generator's unconnected doors list.
                _UnconnectedDoorways.Add(door);

            } // end for i

        }

        private static void ConfigureEntranceOrExitRoomDoor(DungeonGraphNode room, bool isEntranceDoor)
        {
            List<DungeonDoor> unusedDoors = new List<DungeonDoor>();


            // Find all unused doorways in the room.
            foreach (DungeonDoor doorToCheck in room.Doorways)
            {
                if (doorToCheck.OtherRoom_Node == null)
                    unusedDoors.Add(doorToCheck);
            }


            // Randomly select an unused doorway.
            int doorIndex = -1;
            while (true)
            {
                doorIndex = _RNG_DungeonGen.RollRandomIntInRange(0, unusedDoors.Count - 1);

                // Get the door's index within its parent room.
                doorIndex = (int)unusedDoors[doorIndex].ThisRoom_DoorIndex;

                // Do a linear scan outside the door to see if it collides with a wall or another door.
                Vector3Int otherDoor_Tile1WorldPos;
                LinearScanFromDoorResults result = PlaceholderUtils_Doors.DoLinearScanFromDoor(_DungeonTilemapManager,
                                                                                               room,
                                                                                               (int)doorIndex,
                                                                                               out otherDoor_Tile1WorldPos,
                                                                                               LINEAR_DOOR_SCAN_LENGTH);

                // If the door scan didn't find anything in the way, then break out of this loop and setup this door as the exit door.
                if (result == LinearScanFromDoorResults.Nothing)
                {
                    break;
                }
                else
                {
                    if (unusedDoors.Count < 1)
                        throw new Exception($"DungeonGenerator.SelectGoalRoomExitDoor() - Failed to select an { (isEntranceDoor ? "entrance" : "exit") } door because no viable unused doorways are available in the room!");

                    // Debug.LogError($"Room \"{room.RoomBlueprint.RoomName}\"    Door[{doorIndex}] -->    Unused: {unusedDoors.Count}    Total: {room.Doorways.Count}");
                }

            } // end while true


            // Get a reference to the entrance or exit door.
            DungeonDoor door = room.Doorways[doorIndex];
            door.ThisRoom_Node = room;
            door.ThisRoom_DoorIndex = (uint)doorIndex;

            // Set the selected door as the entrance or exit door.
            door.Flags = isEntranceDoor ? DungeonDoorFlags.IsEntranceDoor : DungeonDoorFlags.IsGoalDoor;

            // Remove this door from the unconnected doors list.
            _UnconnectedDoorways.Remove(door);


            if (isEntranceDoor)
            {
                // Position the player next to the entrance door.
                DungeonConstructionUtils.PositionPlayer(_DungeonTilemapManager, room, door);
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
            DungeonConstructionUtils.RegisterFakeTilesOutsideDoor(room, door, _RoomFromTileDict);

        }

        private static void CleanupRejectedRoomData(DungeonGraphNode roomNode)
        {
            // Remove the rejected room's doors from any lists they may have been added to.
            foreach (DungeonDoor door in roomNode.Doorways)
            {
                _BlockedDoorways.Remove(door);
                _UnconnectedDoorways.Remove(door);

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
            {
                throw new Exception($"DungeonGenerator.CreateRoom() - Failed to create a new room, because no room blueprint with {doorCount} doors was found on floor {roomLevel}!");
                //return null;
            }
            else
            {
                return list[roomIndex];
            }

        }


    }

}