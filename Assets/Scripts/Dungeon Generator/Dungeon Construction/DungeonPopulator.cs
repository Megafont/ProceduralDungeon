using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

using ToolboxLib_Shared.Math;

using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.InGame.Items;
using ProceduralDungeon.InGame.Items.Definitions;
using ProceduralDungeon.InGame.Inventory;
using ProceduralDungeon.InGame.Objects;
using ProceduralDungeon.TileMaps;
using ProceduralDungeon.Utilities;


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;


namespace ProceduralDungeon.DungeonGeneration.DungeonConstruction
{

    public static class DungeonPopulator
    {
        private static GameObject _ItemsParent;
        private static GameObject _ObjectsParent;
        private static GameObject _Objects_Buttons_Parent;
        private static GameObject _Objects_Chests_Parent;
        private static GameObject _Objects_Doors_Parent;
        private static GameObject _Objects_Doors_BombableWalls_Parent;
        private static GameObject _Objects_IceBlocks_Parent;
        private static GameObject _Objects_Spikes_Parent;

        private static Dictionary<MissionStructureGraphNode, Object_Door> _LockedDoorsDictionary;
        private static Dictionary<MissionStructureGraphNode, InventoryObject> _KeyChestsDictionary;
        private static uint _NextKeyID;


        private static Vector3 _ObjectOffsetVector = new Vector3(0.5f, 0.5f);
        private static Vector3 _ItemOffsetVector = new Vector3(0.5f, 0.5f);



        public static void PopulateDungeon(DungeonGraph dungeonGraph, NoiseRNG rng, string roomSet)
        {
            Assert.IsNotNull(dungeonGraph, "DungeonPopulator.PopulateDungeon() - The passed in dungeon graph is null!");
            Assert.IsNotNull(rng, "DungeonPopulator.PopulateDungeon() - The passed in random number generator is null!");


            _LockedDoorsDictionary = new Dictionary<MissionStructureGraphNode, Object_Door>();
            _KeyChestsDictionary = new Dictionary<MissionStructureGraphNode, InventoryObject>();
            _NextKeyID = 0;


            // Find the parent game objects of spawned dungeon items/objects.
            _ItemsParent = GameObject.Find("SpawnedItems");

            _ObjectsParent = GameObject.Find("SpawnedObjects");
            _Objects_Buttons_Parent = _ObjectsParent.transform.Find("Buttons").gameObject;
            _Objects_Chests_Parent = _ObjectsParent.transform.Find("Chests").gameObject;
            _Objects_Doors_Parent = _ObjectsParent.transform.Find("Doors").gameObject;
            _Objects_Doors_BombableWalls_Parent = _ObjectsParent.transform.Find("Doors_BombableWalls").gameObject;
            _Objects_IceBlocks_Parent = _ObjectsParent.transform.Find("IceBlocks").gameObject;
            _Objects_Spikes_Parent = _ObjectsParent.transform.Find("Spikes").gameObject;


            // Clear out any previously spawned dungeon items/objects.
            ClearAnyPreviousSpawnedPrefabs();


            // Populate each room in the dungeon.
            DungeonDoor doorway;
            foreach (DungeonGraphNode roomNode in dungeonGraph.Nodes)
            {
                switch (roomNode.MissionStructureNode.GrammarSymbol)
                {
                    case GrammarSymbols.T_Lock:
                        doorway = GetDoorFromMiniBossRoomToThisRoom(roomNode) ?? GetDoorFromParentRoomToThisRoom(roomNode);

                        SpawnObject_Door(doorway, DoorLockTypes.Lock);
                        break;

                    case GrammarSymbols.T_Lock_Multi:
                        doorway = GetDoorFromMiniBossRoomToThisRoom(roomNode) ?? GetDoorFromParentRoomToThisRoom(roomNode);

                        SpawnObject_Door(doorway, DoorLockTypes.Lock_Multipart);
                        break;

                    case GrammarSymbols.T_Lock_Goal:
                        doorway = GetDoorFromMainBossRoomToThisRoom(roomNode) ?? GetDoorFromParentRoomToThisRoom(roomNode);

                        SpawnObject_Door(doorway, DoorLockTypes.Lock_Goal);
                        break;

                    case GrammarSymbols.T_Secret_Room:
                        doorway = GetDoorFromParentRoomToThisRoom(roomNode);

                        SpawnObject_Door_BombableWall(doorway);

                        InventoryData items = new InventoryData();
                        items.AddItem(new ItemData(DungeonGenerator.ItemDatabase.LookupByName("Bomb")), (uint) rng.RollRandomIntInRange(1, 3));

                        SpawnObject_Chest(roomNode, rng, ChestTypes.RandomTreasure, items);
                        break;

                    case GrammarSymbols.T_Treasure_Key:
                        SpawnItem_Key(roomNode, rng, KeyTypes.Key);
                        break;

                    case GrammarSymbols.T_Treasure_Key_Multipart:
                        SpawnItem_Key(roomNode, rng, KeyTypes.Key_Multipart);
                        break;

                    case GrammarSymbols.T_Treasure_Key_Goal:
                        SpawnItem_Key(roomNode, rng, KeyTypes.Key_Goal);
                        break; 

                } // end switch


                SpawnOtherRoomObjects(roomNode);

            } // end foreach room node


            FinalizeLockedDoors();

        }

        private static void SpawnOtherRoomObjects(DungeonGraphNode roomNode)
        {
            foreach (KeyValuePair<Vector3Int, SavedTile> pair in roomNode.RoomBlueprint.Placeholders_Object_Tiles)
            {
                switch (pair.Value.Tile.TileType)
                {
                    case DungeonTileTypes.Placeholders_Objects_Button:
                        SpawnObject_Button(pair.Key, roomNode);
                        break;

                    case DungeonTileTypes.Placeholders_Objects_IceBlock:
                        SpawnObject_IceBlock(pair.Key, roomNode);
                        break;

                    case DungeonTileTypes.Placeholders_Objects_Spikes:
                        SpawnObject_Spikes(pair.Key, roomNode);
                        break;

                } // end switch

            } // end foreach sTile

        }

        private static void ClearAnyPreviousSpawnedPrefabs()
        {

            // Destroy any previously spawned items.
            DestroyAllChildGameObjects(_ItemsParent);

            // Destroy any previously spawned objects.
            DestroyAllChildGameObjects(_Objects_Chests_Parent);
            DestroyAllChildGameObjects(_Objects_Buttons_Parent);
            DestroyAllChildGameObjects(_Objects_Doors_Parent);
            DestroyAllChildGameObjects(_Objects_Doors_BombableWalls_Parent);
            DestroyAllChildGameObjects(_Objects_IceBlocks_Parent);
            DestroyAllChildGameObjects(_Objects_Spikes_Parent);
        }
      
        /// <summary>
        /// Spawns a chest at a randomly chosen placeholder tile the type of which is determined by the specified chest type.
        /// </summary>
        /// <param name="roomNode">The DungeonGraphNode of the room that contains the chest.</param>
        /// <param name="rng">The random number generator to use.</param>
        /// <param name="chestType">The type of chest to spawn.</param>
        /// <param name="chestContents">The items to place inside the chest.</param>
        /// <returns>A reference to the spawned chest GameObject.</returns>
        /// <exception cref="System.Exception">When the parent room does not have any placeholder tiles of the type corresponding to the type of chest that is supposed to be spawned.</exception>
        private static GameObject SpawnObject_Chest(DungeonGraphNode roomNode, NoiseRNG rng, ChestTypes chestType, InventoryData chestContents)
        {
            // Select placeholders list.
            List<SavedTile> placeholdersList = null;
            if (chestType == ChestTypes.RandomTreasure)
                placeholdersList = roomNode.RoomBlueprint.Chest_RandomTreasure_Placeholders;

            // Randomly select a chest placeholder position.
            int index = rng.RollRandomIntInRange(0, placeholdersList.Count - 1);


            // Check for error before we get the local room coordinates of the selected chest spawn point.
            if (placeholdersList.Count < 1)
                throw new System.Exception($"DungeonPopulator.SpawnChest() - The room \"{roomNode.RoomBlueprint.RoomName}\" at {roomNode.RoomCenterPoint} does not contain any chest spawn points for chest type \"{chestType}\"!");

            // Get the chests's local position within the parent room, and its rotation.
            Vector3Int chestPosLocal = roomNode.RoomBlueprint.Chest_RandomTreasure_Placeholders[index].Position;
            Quaternion chestRotation = roomNode.RoomBlueprint.Chest_RandomTreasure_Placeholders[index].Rotation;

            // Calculate the world coordinates of the key position.
            Vector3Int chestPosWorld = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(chestPosLocal, roomNode.RoomPosition, roomNode.RoomFinalDirection);


            // Calculate the final rotation of the chest based on the room rotation.
            Directions chestDirection = Directions.North;
            chestDirection = chestDirection.DirectionFromRotation(chestRotation);            

            Directions chestFinalDirection = chestDirection.AddRotationDirection(roomNode.RoomFinalDirection);
            chestFinalDirection = MiscellaneousUtils.CorrectObjectRotationDirection(chestDirection, chestFinalDirection, roomNode.RoomFinalDirection);

            Quaternion chestFinalRotation = chestFinalDirection.DirectionToRotation();


            //Debug.LogError($"SPAWN CHEST:    Direction: {chestDirection}    FinalDirection: {chestFinalDirection}    Rotation: {chestRotation.eulerAngles}    FinalRotation: {chestFinalRotation.eulerAngles}    RoomPosition: {roomNode.RoomPosition}    RoomDirection: {roomNode.RoomDirection}");


            return SpawnObject_Chest(roomNode, chestType, chestContents, chestPosWorld, chestFinalRotation);
        }

        /// <summary>
        /// Spawns a chest of the specified type, at the specified position with the specified rotation and contents.
        /// </summary>
        /// <param name="roomNode">The DungeonGraphNode of the room that contains the chest.</param>
        /// <param name="chestType">The type of chest to spawn.</param>
        /// <param name="chestContents">The items to place inside the chest.</param>
        /// <param name="position">The position to spawn the chest at.</param>
        /// <param name="rotation">The rotation direction of the spawned chest.</param>
        /// <returns>A reference to the spawned chest GameObject.</returns>
        private static GameObject SpawnObject_Chest(DungeonGraphNode roomNode, ChestTypes chestType, InventoryData chestContents, Vector3Int position, Quaternion rotation)
        {
            // Select chest type.
            GameObject chestPrefab = null;
            if (chestType == ChestTypes.Key || chestType == ChestTypes.Key_Multipart || chestType == ChestTypes.RandomTreasure)
                chestPrefab = PrefabManager.GetPrefab("Object_Chest", roomNode.RoomBlueprint.RoomSet);
            else if (chestType == ChestTypes.Key_Goal)
                chestPrefab = PrefabManager.GetPrefab("Object_ChestGoal", roomNode.RoomBlueprint.RoomSet);

            
            GameObject chest = GameObject.Instantiate(chestPrefab, 
                                                      _ItemOffsetVector + position,
                                                      rotation,
                                                      _Objects_Chests_Parent.transform);


            // Fill the chest with the specified items.
            InventoryObject inventory = chest.GetComponent<Object_Chest>().Inventory;
            if (inventory != null)
                inventory.Data.AddItems(chestContents);


            // Setup the chest's sprite properties.
            Object_Chest objChest = chest.GetComponent<Object_Chest>();
            RoomSets roomSet = roomNode.RoomBlueprint.RoomSet;
            if (chestType == ChestTypes.Key || chestType == ChestTypes.Key_Multipart || chestType == ChestTypes.RandomTreasure)
            {
                objChest.ClosedSprite = SpriteManager.GetSprite("Object_Chest_Closed", roomSet);
                objChest.OpenSprite = SpriteManager.GetSprite("Object_Chest_Open", roomSet);
            }
            else
            {
                objChest.ClosedSprite = SpriteManager.GetSprite("Object_ChestGoal_Closed", roomSet);
                objChest.OpenSprite = SpriteManager.GetSprite("Object_ChestGoal_Open", roomSet);
            }

            objChest.GetComponent<SpriteRenderer>().sprite = objChest.ClosedSprite;
            objChest.ParentRoom = roomNode;


            return chest;
        }

        private static void SpawnObject_Door(DungeonDoor doorToSpawn, DoorLockTypes lockType)
        {
            Vector3 offset;
            Quaternion rotation;
            
            Directions doorDirection = doorToSpawn.ThisRoom_Node.RoomBlueprint.DoorsList[(int)doorToSpawn.ThisRoom_DoorIndex].DoorDirection;
            Directions doorAdjustedDirection = doorToSpawn.ThisRoom_DoorAdjustedDirection;

            if (doorAdjustedDirection == Directions.North ||
                doorAdjustedDirection == Directions.South)
            {
                offset = doorAdjustedDirection == Directions.North ? new Vector3(1.0f, 0.0f) :
                                                                     new Vector3(1.0f, 1.0f);
            }
            else
            {
                offset = doorAdjustedDirection == Directions.East ? new Vector3(0.0f, 0.0f) :
                                                                    new Vector3(1.0f, 0.0f);
            }


            if (doorDirection == Directions.East || doorDirection == Directions.West)
                doorAdjustedDirection = doorAdjustedDirection.FlipDirection();

            Directions correctedDirection = MiscellaneousUtils.CorrectObjectRotationDirection(doorDirection, 
                                                                                              doorAdjustedDirection,
                                                                                              doorToSpawn.ThisRoom_Node.RoomFinalDirection);
            rotation = correctedDirection.DirectionToRotation();
            
            //Debug.LogError($"ORIG: {doorToSpawn.ThisRoom_Node.RoomBlueprint.DoorsList[(int) doorToSpawn.ThisRoom_DoorIndex].DoorDirection}    FINAL: {doorToSpawn.ThisRoom_DoorAdjustedDirection}    CORRECTED: {correctedDirection}");


            // Calculate the center point of the door.
            Vector3 centerPoint = MiscellaneousUtils.GetUpperLeftMostTile(doorToSpawn.ThisRoom_DoorTile1WorldPosition, doorToSpawn.ThisRoom_DoorTile2WorldPosition);
            centerPoint += offset;


            // Spawn a door object and configure it.
            GameObject door = GameObject.Instantiate(PrefabManager.GetPrefab("Object_Door", doorToSpawn.ThisRoom_Node.RoomBlueprint.RoomSet), 
                                                     centerPoint, 
                                                     rotation,
                                                     _Objects_Doors_Parent.transform);

            Object_Door doorComponent = door.GetComponent<Object_Door>();


            // We use the other room node for non-goal locked doors, because these locked doors spawn in the room next to the lock room,
            // thus preventing access to the room.
            // This is also true if this lock is blocking a mini boss room.
            if (lockType != DoorLockTypes.Lock_Goal &&
                doorToSpawn.OtherRoom_Node.MissionStructureNode.GrammarSymbol != GrammarSymbols.T_Boss_Mini)
            {
                _LockedDoorsDictionary.Add(doorToSpawn.OtherRoom_Node.MissionStructureNode, doorComponent);
}
            else
            {
                _LockedDoorsDictionary.Add(doorToSpawn.ThisRoom_Node.MissionStructureNode, doorComponent);
            }
            


            doorComponent.Key_ID = _NextKeyID;
            _NextKeyID++;

            doorComponent.Doorway = doorToSpawn; // Give the new object a reference to the doorway it represents.
            doorComponent.DoorState = DoorStates.Locked;
            doorComponent.LockType = lockType;

            RoomSets roomSet = doorToSpawn.ThisRoom_Node.RoomBlueprint.RoomSet;
            doorComponent.ClosedSprite = SpriteManager.GetSprite("Object_Door_Closed", roomSet);
            doorComponent.LockedSprite = SpriteManager.GetSprite("Object_Door_Locked", roomSet);
            doorComponent.LockedMultipartSprite = SpriteManager.GetSprite("Object_Door_Locked_Multipart", roomSet);
            doorComponent.LockedGoalSprite = SpriteManager.GetSprite("Object_Door_Locked_Goal", roomSet);

            doorComponent.ToggleState();

        }

        private static void SpawnObject_Door_BombableWall(DungeonDoor doorToSpawn)
        {
            Vector3 offset;
            Quaternion rotation;

            Directions doorDirection = doorToSpawn.ThisRoom_Node.RoomBlueprint.DoorsList[(int)doorToSpawn.ThisRoom_DoorIndex].DoorDirection;
            Directions doorAdjustedDirection = doorToSpawn.ThisRoom_DoorAdjustedDirection;

            if (doorAdjustedDirection == Directions.North ||
                doorAdjustedDirection == Directions.South)
            {
                offset = doorAdjustedDirection == Directions.North ? new Vector3(1.0f, 0.5f) :
                                                                     new Vector3(1.0f, 0.5f);
            }
            else
            {
                offset = doorAdjustedDirection == Directions.East ? new Vector3(0.5f, 0.0f) :
                                                                    new Vector3(0.5f, 0.0f);
            }


            if (doorDirection == Directions.East || doorDirection == Directions.West)
                doorAdjustedDirection = doorAdjustedDirection.FlipDirection();

            Directions correctedDirection = MiscellaneousUtils.CorrectObjectRotationDirection(doorDirection,
                                                                                              doorAdjustedDirection,
                                                                                              doorToSpawn.ThisRoom_Node.RoomFinalDirection);
            rotation = correctedDirection.DirectionToRotation();


            // Calculate the center point of the door.
            Vector3 centerPoint = MiscellaneousUtils.GetUpperLeftMostTile(doorToSpawn.ThisRoom_DoorTile1WorldPosition, doorToSpawn.ThisRoom_DoorTile2WorldPosition);
            centerPoint += offset;


            // Spawn a door object and configure it.
            GameObject door = GameObject.Instantiate(PrefabManager.GetPrefab("Object_Door_Bombablewall", doorToSpawn.ThisRoom_Node.RoomBlueprint.RoomSet),
                                                     centerPoint, 
                                                     rotation, 
                                                     _Objects_Doors_BombableWalls_Parent.transform);


            // Give the new object a reference to the doorway it represents.
            door.GetComponent<Object_Door_BombableWall>().Doorway = doorToSpawn;
        }

        private static void SpawnObject_Button(Vector3Int position, DungeonGraphNode roomNode)
        {
            // Calculate the position of the spikes.
            Vector3 centerPoint = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(position, roomNode.RoomPosition, roomNode.RoomFinalDirection);


            // Spawn a spikes object and configure it.
            GameObject button = GameObject.Instantiate(PrefabManager.GetPrefab("Object_Button", roomNode.RoomBlueprint.RoomSet),
                                                       centerPoint + _ObjectOffsetVector,
                                                       Quaternion.identity,
                                                       _Objects_Buttons_Parent.transform);


            RoomSets roomSet = roomNode.RoomBlueprint.RoomSet;
            Object_Button buttonComponent = button.GetComponent<Object_Button>();
            buttonComponent._ButtonSprite = SpriteManager.GetSprite("Object_Button", roomSet);
            buttonComponent._ButtonPressedSprite = SpriteManager.GetSprite("Object_Button_Pressed", roomSet);
        }

        private static void SpawnObject_IceBlock(Vector3Int position, DungeonGraphNode roomNode)
        {
            // Calculate the position of the ice block.
            Vector3 centerPoint = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(position, roomNode.RoomPosition, roomNode.RoomFinalDirection);


            // Spawn an ice block and configure it.
            GameObject iceBlock = GameObject.Instantiate(PrefabManager.GetPrefab("Object_IceBlock", roomNode.RoomBlueprint.RoomSet),
                                                       centerPoint + _ObjectOffsetVector,
                                                       Quaternion.identity,
                                                       _Objects_IceBlocks_Parent.transform);


            RoomSets roomSet = roomNode.RoomBlueprint.RoomSet;
            Object_IceBlock iceBlockComponent = iceBlock.GetComponent<Object_IceBlock>();
            iceBlockComponent.GetComponent<SpriteRenderer>().sprite = SpriteManager.GetSprite("Object_IceBlock", roomSet);

        }

        private static void SpawnObject_Spikes(Vector3Int position, DungeonGraphNode roomNode)
        {
            // Calculate the position of the spikes.
            Vector3 centerPoint = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(position, roomNode.RoomPosition, roomNode.RoomFinalDirection);


            // Spawn a spikes object and configure it.
            GameObject spikes = GameObject.Instantiate(PrefabManager.GetPrefab("Object_Spikes", roomNode.RoomBlueprint.RoomSet), 
                                                       centerPoint + _ObjectOffsetVector, 
                                                       Quaternion.identity, 
                                                       _Objects_Spikes_Parent.transform);


            RoomSets roomSet = roomNode.RoomBlueprint.RoomSet;
            Object_Spikes spikesComponent = spikes.GetComponent<Object_Spikes>();
            spikesComponent.GetComponent<SpriteRenderer>().sprite = SpriteManager.GetSprite("Object_Spikes", roomSet);
        }



        private static void SpawnItem_Key(DungeonGraphNode roomNode, NoiseRNG rng, KeyTypes keyType)
        {
            // Randomly select a key placeholder position.
            int index = rng.RollRandomIntInRange(0, roomNode.RoomBlueprint.Key_Placeholders.Count - 1);

            // Check for error before we get the local room coordinates of the selected key spawn point.
            if (roomNode.RoomBlueprint.Key_Placeholders.Count < 1)
                throw new System.Exception($"DungeonPopulator.SpawnKey() - The room \"{roomNode.RoomBlueprint.RoomName}\" at {roomNode.RoomCenterPoint} does not contain any key spawn points!");


            // Get the key's local position within the parent room, and its rotation.
            Vector3Int keyPosLocal = Vector3Int.zero;
            Quaternion keyRotation = Quaternion.identity;
            ChestTypes chestType = ChestTypes.RandomTreasure;
            if (keyType == KeyTypes.Key)
            {
                chestType = ChestTypes.Key;
                keyPosLocal = roomNode.RoomBlueprint.Key_Placeholders[index].Position;
                keyRotation = roomNode.RoomBlueprint.Key_Placeholders[index].Rotation;
            }
            else if (keyType == KeyTypes.Key_Multipart)
            {
                chestType = ChestTypes.Key_Multipart;
                keyPosLocal = roomNode.RoomBlueprint.Key_Multipart_Placeholders[index].Position;
                keyRotation = roomNode.RoomBlueprint.Key_Multipart_Placeholders[index].Rotation;
            }
            else if (keyType == KeyTypes.Key_Goal)
            {
                chestType = ChestTypes.Key_Goal;
                keyPosLocal = roomNode.RoomBlueprint.Key_Goal_Placeholders[index].Position;
                keyRotation = roomNode.RoomBlueprint.Key_Goal_Placeholders[index].Rotation;
            }


            // Calculate the final rotation of the chest based on the room rotation.
            Directions keyDirection = Directions.North;
            keyDirection = keyDirection.DirectionFromRotation(keyRotation);

            Directions keyFinalDirection = keyDirection.AddRotationDirection(roomNode.RoomFinalDirection);
            keyFinalDirection = MiscellaneousUtils.CorrectObjectRotationDirection(keyDirection, keyFinalDirection, roomNode.RoomFinalDirection);

            Quaternion keyFinalRotation = keyFinalDirection.DirectionToRotation();


            // Calculate the world coordinates of the key position.
            Vector3Int keyPosWorld = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(keyPosLocal, roomNode.RoomPosition, roomNode.RoomFinalDirection);


            // Spawn a chest containing a key.
            GameObject chest = SpawnObject_Chest(roomNode, chestType, null, keyPosWorld, keyFinalRotation);

           
            // Get the Inventory component and add it to our dictionary to track it for a later pass to setup the key/lock pairs.
            InventoryObject chestInventory = chest.GetComponent<Object_Chest>().Inventory;
            _KeyChestsDictionary.Add(roomNode.MissionStructureNode, chestInventory);

        }

        private static void FinalizeLockedDoors()
        {
            // If Unity is not in play mode, then just return as the code below will cause a null reference exception then anyway.
            if (!Application.isPlaying)
                return;


            foreach (KeyValuePair<MissionStructureGraphNode, InventoryObject> pair in _KeyChestsDictionary)
            {
                foreach (MissionStructureChildNodeData childNodeData in pair.Key.ChildNodesData)
                {
                    // Get the chest inventory.
                    InventoryObject chestInventory = pair.Value;

                    // Determine which type of key to put in the chest.
                    GrammarSymbols symbol = childNodeData.ChildNode.GrammarSymbol;
                    if (symbol == GrammarSymbols.T_Lock || symbol == GrammarSymbols.T_Lock_Multi || symbol == GrammarSymbols.T_Lock_Goal)
                    {
                        Object_Door lockedDoor;
                        _LockedDoorsDictionary.TryGetValue(childNodeData.ChildNode, out lockedDoor);
                        if (lockedDoor == null)
                            continue;

                        ItemDefinition_Key keyType = null;
                        if (lockedDoor.LockType == DoorLockTypes.Lock)
                        {
                            keyType = (ItemDefinition_Key)chestInventory.ItemDatabase.LookupByName("Key");
                        }
                        else if (lockedDoor.LockType == DoorLockTypes.Lock_Multipart)
                        {
                            keyType = (ItemDefinition_Key)chestInventory.ItemDatabase.LookupByName("Key Part");

                            lockedDoor.MultipartKeyCount++;
                        }
                        else if (lockedDoor.LockType == DoorLockTypes.Lock_Goal)
                        {
                            keyType = (ItemDefinition_Key)chestInventory.ItemDatabase.LookupByName("Goal Key");
                        }


                        // Create a key item.
                        ItemData_Key key = new ItemData_Key(keyType);
                        key.KeyID = lockedDoor.Key_ID;

                        // Insert an appropriate key into the chest's inventory.
                        chestInventory.Data.AddItem(key, 1);


                        break;

                    } // end if child node is a lock room


                } // end foreach childNodeData

            } // end foreach pair

        }


        private static DungeonDoor GetDoorFromParentRoomToThisRoom(DungeonGraphNode roomNode)
        {
            DungeonGraphNode parentRoomNode = roomNode.Parent;

            // Find the door to this room's parent.
            foreach (DungeonDoor door in parentRoomNode.Doorways)
            {
                if (door.OtherRoom_Node == roomNode)
                    return door;
            }

            return null;
        }

        private static DungeonDoor GetDoorFromMiniBossRoomToThisRoom(DungeonGraphNode roomNode)
        {
            // Find the door to the boss room.
            foreach (DungeonDoor door in roomNode.Doorways)
            {
                if (door.OtherRoom_Node == null)
                    continue;
                else if (door.OtherRoom_Node.MissionStructureNode.GrammarSymbol == GrammarSymbols.T_Boss_Mini)
                    return door;
            }

            return null;
        }

        private static DungeonDoor GetDoorFromMainBossRoomToThisRoom(DungeonGraphNode roomNode)
        {
            // Find the door to the boss room.
            foreach (DungeonDoor door in roomNode.Doorways)
            {
                if (door.OtherRoom_Node == null)
                    continue;
                else if (door.OtherRoom_Node.MissionStructureNode.GrammarSymbol == GrammarSymbols.T_Boss_Main)
                    return door;
            }

            return null;
        }

        private static void DestroyAllChildGameObjects(GameObject parent)
        {
            // I couldn't get it to delete all child game objects myself, as there were always
            // a couple not getting deleted. The code in this function is a solution I found here:
            // https://stackoverflow.com/questions/46358717/how-to-loop-through-and-destroy-all-children-of-a-game-object-in-unity


            int i = 0;

            //Array to hold all child obj
            GameObject[] allChildren = new GameObject[parent.transform.childCount];

            //Find all child obj and store to that array
            foreach (Transform child in parent.transform)
            {
                allChildren[i] = child.gameObject;
                i += 1;
            }

            //Now destroy them
            foreach (GameObject child in allChildren)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }

        }


    }

}
