using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

using ToolboxLib_Shared.Math;

using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.InGame.Items;
using ProceduralDungeon.InGame.Objects;
using ProceduralDungeon.TileMaps;


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;


namespace ProceduralDungeon.DungeonGeneration.DungeonConstruction
{

    public static class DungeonPopulator
    {
        private static string _CurrentRoomSet;
        private static GameObject _ItemsParent;
        private static GameObject _ObjectsParent;
        private static GameObject _Objects_Chests_Parent;
        private static GameObject _Objects_Doors_Parent;
        private static GameObject _Objects_Doors_BombableWalls_Parent;
        private static GameObject _Objects_Spikes_Parent;

        private static Dictionary<MissionStructureGraphNode, Object_Door> _LockedDoorsDictionary;
        private static Dictionary<MissionStructureGraphNode, InventoryOld> _KeyChestsDictionary;
        private static uint _NextKeyID;


        private static Vector3 _ObjectOffsetVector = new Vector3(0.5f, 0.5f);
        private static Vector3 _ItemOffsetVector = new Vector3(0.5f, 0.5f);

        private static GameObject _Prefab_Object_Chest;
        private static GameObject _Prefab_Object_ChestGoal;
        private static GameObject _Prefab_Object_Door;
        private static GameObject _Prefab_Object_Door_BombableWall;
        private static GameObject _Prefab_Object_Spikes;

        private static GameObject _Prefab_Item_Key;



        public static void PopulateDungeon(DungeonGraph dungeonGraph, NoiseRNG rng, string roomSet)
        {
            Assert.IsNotNull(dungeonGraph, "DungeonPopulator.PopulateDungeon() - The passed in dungeon graph is null!");
            Assert.IsNotNull(rng, "DungeonPopulator.PopulateDungeon() - The passed in random number generator is null!");


            _CurrentRoomSet = roomSet;


            LoadPrefabs();


            _LockedDoorsDictionary = new Dictionary<MissionStructureGraphNode, Object_Door>();
            _KeyChestsDictionary = new Dictionary<MissionStructureGraphNode, InventoryOld>();
            _NextKeyID = 0;


            // Find the parent game objects of spawned dungeon items/objects.
            _ItemsParent = GameObject.Find("SpawnedItems");

            _ObjectsParent = GameObject.Find("SpawnedObjects");
            _Objects_Chests_Parent = _ObjectsParent.transform.Find("Chests").gameObject;
            _Objects_Doors_Parent = _ObjectsParent.transform.Find("Doors").gameObject;
            _Objects_Doors_BombableWalls_Parent = _ObjectsParent.transform.Find("Doors_BombableWalls").gameObject;
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
                        doorway = GetDoorFromMiniBossRoomToThisRoom(roomNode);
                        if (doorway == null)
                            doorway = GetDoorFromParentRoomToThisRoom(roomNode);

                        SpawnObject_Door(doorway, DoorLockTypes.Lock);
                        break;

                    case GrammarSymbols.T_Lock_Multi:
                        doorway = GetDoorFromMiniBossRoomToThisRoom(roomNode);
                        if (doorway == null)
                            doorway = GetDoorFromParentRoomToThisRoom(roomNode);

                        SpawnObject_Door(doorway, DoorLockTypes.Lock_Multipart);
                        break;

                    case GrammarSymbols.T_Lock_Goal:
                        doorway = GetDoorFromMainBossRoomToThisRoom(roomNode);
                        if (doorway == null)
                            doorway = GetDoorFromParentRoomToThisRoom(roomNode);

                        SpawnObject_Door(doorway, DoorLockTypes.Lock_Goal);
                        break;

                    case GrammarSymbols.T_Secret_Room:
                        doorway = GetDoorFromParentRoomToThisRoom(roomNode);

                        SpawnObject_Door_BombableWall(doorway);
                        SpawnObject_Chest(roomNode, rng, ChestTypes.RandomTreasure, new ItemData[] { new ItemData() { ItemType = ItemTypes.Item_Bomb, ItemCount = (uint)rng.RollRandomIntInRange(0, 3) } } );
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
            DestroyAllChildGameObjects(_Objects_Doors_Parent);
            DestroyAllChildGameObjects(_Objects_Doors_BombableWalls_Parent);
            DestroyAllChildGameObjects(_Objects_Spikes_Parent);
        }

        private static void LoadPrefabs()
        {
            LoadObjectPrefabs();
            LoadItemPrefabs();
        }

        private static void LoadObjectPrefabs()
        {
            if (_Prefab_Object_Chest == null)
                _Prefab_Object_Chest = (GameObject)Resources.Load("Prefabs/Objects/Object_Chest");

            if (_Prefab_Object_ChestGoal == null)
                _Prefab_Object_ChestGoal = (GameObject)Resources.Load("Prefabs/Objects/Object_ChestGoal");

            if (_Prefab_Object_Door == null)
                _Prefab_Object_Door = (GameObject)Resources.Load("Prefabs/Objects/Object_Door");

            if (_Prefab_Object_Door_BombableWall == null)
                _Prefab_Object_Door_BombableWall = (GameObject)Resources.Load("Prefabs/Objects/Object_Door_BombableWall");


            if (_Prefab_Object_Spikes == null)
                _Prefab_Object_Spikes = (GameObject)Resources.Load("Prefabs/Objects/Object_Spikes");
        }

        private static void LoadItemPrefabs()
        {
            if (_Prefab_Item_Key == null)
                _Prefab_Item_Key = (GameObject)Resources.Load("Prefabs/Items/Item_Key");
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
        private static GameObject SpawnObject_Chest(DungeonGraphNode roomNode, NoiseRNG rng, ChestTypes chestType, ItemData[] chestContents)
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
            Vector3Int chestPosWorld = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(chestPosLocal, roomNode.RoomPosition, roomNode.RoomDirection);


            // Calculate the final rotation of the chest based on the room rotation.
            Directions chestDirection = Directions.North;
            chestDirection = chestDirection.DirectionFromRotation(chestRotation);            

            Directions chestFinalDirection = chestDirection.AddRotationDirection(roomNode.RoomDirection);
            chestFinalDirection = MiscellaneousUtils.CorrectObjectRotationDirection(chestDirection, chestFinalDirection);

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
        private static GameObject SpawnObject_Chest(DungeonGraphNode roomNode, ChestTypes chestType, ItemData[] chestContents, Vector3Int position, Quaternion rotation)
        {
            // Select chest type.
            GameObject chestPrefab = null;
            if (chestType == ChestTypes.Key || chestType == ChestTypes.Key_Multipart || chestType == ChestTypes.RandomTreasure)
                chestPrefab = _Prefab_Object_Chest;
            else if (chestType == ChestTypes.Key_Goal)
                chestPrefab = _Prefab_Object_ChestGoal;

            
            GameObject chest = GameObject.Instantiate(chestPrefab, _ItemOffsetVector + position, rotation, _Objects_Chests_Parent.transform);


            // Fill the chest with the specified items.
            InventoryOld inventory = chest.GetComponent<InventoryOld>();
            inventory.InsertItems(chestContents);


            // Setup the chest's sprite properties.
            Object_Chest objChest = chest.GetComponent<Object_Chest>();
            RoomSets roomSet = roomNode.RoomBlueprint.RoomSet;
            if (chestType == ChestTypes.Key || chestType == ChestTypes.Key_Multipart || chestType == ChestTypes.RandomTreasure)
            {
                objChest.ClosedSprite = SpriteLoader.GetObjectSprite("Object_Chest_Closed", roomSet);
                objChest.OpenSprite = SpriteLoader.GetObjectSprite("Object_Chest_Open", roomSet);
            }
            else
            {
                objChest.ClosedSprite = SpriteLoader.GetObjectSprite("Object_ChestGoal_Closed", roomSet);
                objChest.OpenSprite = SpriteLoader.GetObjectSprite("Object_ChestGoal_Open", roomSet);
            }

            objChest.GetComponent<SpriteRenderer>().sprite = objChest.ClosedSprite;
            objChest.ParentRoom = roomNode;


            return chest;
        }

        private static void SpawnObject_Door(DungeonDoor doorToSpawn, DoorLockTypes lockType)
        {
            Vector3 offset;
            Quaternion rotation;
            Directions doorDirection = doorToSpawn.ThisRoom_DoorAdjustedDirection;

            if (doorDirection == Directions.North ||
                doorDirection == Directions.South)
            {
                offset = doorDirection == Directions.North ? new Vector3(1.0f, 0.0f) : 
                                                             new Vector3(1.0f, 1.0f);
            }
            else
            {
                offset = doorDirection == Directions.East ? new Vector3(0.0f, 0.0f) :
                                                            new Vector3(1.0f, 0.0f);
            }


            Directions correctedDirection = MiscellaneousUtils.CorrectObjectRotationDirection(doorToSpawn.ThisRoom_Node.RoomBlueprint.DoorsList[(int)doorToSpawn.ThisRoom_DoorIndex].DoorDirection, 
                                                                                              doorToSpawn.ThisRoom_DoorAdjustedDirection);
            rotation = correctedDirection.DirectionToRotation();


            // Calculate the center point of the door.
            Vector3 centerPoint = MiscellaneousUtils.GetUpperLeftMostTile(doorToSpawn.ThisRoom_DoorTile1WorldPosition, doorToSpawn.ThisRoom_DoorTile2WorldPosition);
            centerPoint += offset;


            // Spawn a door object and configure it.
            GameObject door = GameObject.Instantiate(_Prefab_Object_Door, centerPoint, rotation, _Objects_Doors_Parent.transform);
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
            doorComponent.ClosedSprite = SpriteLoader.GetObjectSprite("Object_Door_Closed", roomSet);
            doorComponent.LockedSprite = SpriteLoader.GetObjectSprite("Object_Door_Locked", roomSet);
            doorComponent.LockedMultipartSprite = SpriteLoader.GetObjectSprite("Object_Door_Locked_Multipart", roomSet);
            doorComponent.LockedGoalSprite = SpriteLoader.GetObjectSprite("Object_Door_Locked_Goal", roomSet);

            doorComponent.ToggleState();

        }

        private static void SpawnObject_Door_BombableWall(DungeonDoor doorToSpawn)
        {
            Vector3 offset;
            Quaternion rotation;
            Directions doorDirection = doorToSpawn.ThisRoom_DoorAdjustedDirection;

            if (doorDirection == Directions.North ||
                doorDirection == Directions.South)
            {
                offset = doorDirection == Directions.North ? new Vector3(1.0f, 0.5f) :
                                                             new Vector3(1.0f, 0.5f);
            }
            else
            {
                offset = doorDirection == Directions.East ? new Vector3(0.5f, 0.0f) :
                                                            new Vector3(0.5f, 0.0f);
            }


            Directions correctedDirection = MiscellaneousUtils.CorrectObjectRotationDirection(doorToSpawn.ThisRoom_Node.RoomBlueprint.DoorsList[(int)doorToSpawn.ThisRoom_DoorIndex].DoorDirection,
                                                                                              doorToSpawn.ThisRoom_DoorAdjustedDirection);
            rotation = correctedDirection.DirectionToRotation();


            // Calculate the center point of the door.
            Vector3 centerPoint = MiscellaneousUtils.GetUpperLeftMostTile(doorToSpawn.ThisRoom_DoorTile1WorldPosition, doorToSpawn.ThisRoom_DoorTile2WorldPosition);
            centerPoint += offset;


            // Spawn a door object and configure it.
            GameObject door = GameObject.Instantiate(_Prefab_Object_Door_BombableWall, centerPoint, rotation, _Objects_Doors_BombableWalls_Parent.transform);

            // Give the new object a reference to the doorway it represents.
            door.GetComponent<Object_Door_BombableWall>().Doorway = doorToSpawn;
        }

        private static void SpawnObject_Spikes(Vector3Int position, DungeonGraphNode roomNode)
        {
            // Calculate the position of the spikes.
            Vector3 centerPoint = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(position, roomNode.RoomPosition, roomNode.RoomDirection);

            // Spawn a spikes object and configure it.
            GameObject spikes = GameObject.Instantiate(_Prefab_Object_Spikes, 
                                                       centerPoint + _ObjectOffsetVector, 
                                                       Quaternion.identity, 
                                                       _Objects_Spikes_Parent.transform);


            RoomSets roomSet = roomNode.RoomBlueprint.RoomSet;
            Object_Spikes spikesComponent = spikes.GetComponent<Object_Spikes>();
            spikesComponent.GetComponent<SpriteRenderer>().sprite = SpriteLoader.GetObjectSprite("Object_Spikes", roomSet);
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


            // Calculate the world coordinates of the key position.
            Vector3Int keyPosWorld = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(keyPosLocal, roomNode.RoomPosition, roomNode.RoomDirection);


            // Spawn a chest containing a key.
            GameObject chest = SpawnObject_Chest(roomNode, chestType, new ItemData[0], keyPosWorld, keyRotation);

           
            // Get the Inventory component and add it to our dictionary to track it for a later pass to setup the key/lock pairs.
            InventoryOld chestInventory = chest.GetComponent<InventoryOld>();
            _KeyChestsDictionary.Add(roomNode.MissionStructureNode, chestInventory);

        }

        private static void FinalizeLockedDoors()
        {
            foreach (KeyValuePair<MissionStructureGraphNode, InventoryOld> pair in _KeyChestsDictionary)
            {
                foreach (MissionStructureChildNodeData childNodeData in pair.Key.ChildNodesData)
                {
                    GrammarSymbols symbol = childNodeData.ChildNode.GrammarSymbol;
                    if (symbol == GrammarSymbols.T_Lock || symbol == GrammarSymbols.T_Lock_Multi || symbol == GrammarSymbols.T_Lock_Goal)
                    {
                        Object_Door lockedDoor;
                        _LockedDoorsDictionary.TryGetValue(childNodeData.ChildNode, out lockedDoor);
                        if (lockedDoor == null)
                            continue;

                        KeyTypes keyType = KeyTypes.Key;
                        if (lockedDoor.LockType == DoorLockTypes.Lock)
                            keyType = KeyTypes.Key;
                        else if (lockedDoor.LockType == DoorLockTypes.Lock_Multipart)
                        {
                            keyType = KeyTypes.Key_Multipart;
                            lockedDoor.MultipartKeyCount++;
                        }
                        else if (lockedDoor.LockType == DoorLockTypes.Lock_Goal)
                            keyType = KeyTypes.Key_Goal;


                        // Insert an appropriate key into the chest's inventory.
                        pair.Value.InsertItem(new ItemData() { ItemType = Item_Key.KeyTypeFromItemType(keyType), ItemCount = 1, GroupID = (int)lockedDoor.Key_ID });


                        break;

                    } // end if child node is lock room


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
