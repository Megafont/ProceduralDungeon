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

        private static GameObject _Prefab_Object_Chest;
        private static GameObject _Prefab_Object_ChestGoal;
        private static GameObject _Prefab_Object_Door;

        private static GameObject _Prefab_Item_Key;
        
        private static Vector3 _ItemOffsetVector = new Vector3(0.5f, 0.5f);

        private static Dictionary<string, Dictionary<string, Sprite>> _SpritesDictionary;

        private static Dictionary<MissionStructureGraphNode, Object_Door> _LockedDoorsDictionary;
        private static Dictionary<MissionStructureGraphNode, Inventory> _KeyChestsDictionary;
        private static uint _NextKeyID;




        public static void PopulateDungeon(DungeonGraph dungeonGraph, NoiseRNG rng, string roomSet)
        {
            Assert.IsNotNull(dungeonGraph, "DungeonPopulator.PopulateDungeon() - The passed in dungeon graph is null!");
            Assert.IsNotNull(rng, "DungeonPopulator.PopulateDungeon() - The passed in random number generator is null!");


            _CurrentRoomSet = roomSet;


            LoadPrefabs();


            _LockedDoorsDictionary = new Dictionary<MissionStructureGraphNode, Object_Door>();
            _KeyChestsDictionary = new Dictionary<MissionStructureGraphNode, Inventory>();
            _NextKeyID = 0;

            
            _SpritesDictionary = new Dictionary<string, Dictionary<string, Sprite>>();


            // Find the parent game objects of spawned dungeon items/objects.
            _ItemsParent = GameObject.Find("SpawnedItems");
            _ObjectsParent = GameObject.Find("SpawnedObjects");
            
            // Clear out any previously spawned dungeon items/objects.
            ClearAnyPreviousSpawnedPrefabs();


            // Populate each room in the dungeon.
            foreach (DungeonGraphNode roomNode in dungeonGraph.Nodes)
            {
                switch (roomNode.MissionStructureNode.GrammarSymbol)
                {
                    case GrammarSymbols.T_Lock:
                        SpawnDoor(GetDoorFromParentRoomToThisRoom(roomNode), DoorLockTypes.Lock);
                        break;

                    case GrammarSymbols.T_Lock_Multi:
                        SpawnDoor(GetDoorFromParentRoomToThisRoom(roomNode), DoorLockTypes.Lock_Multipart);
                        break;

                    case GrammarSymbols.T_Lock_Goal:
                        SpawnDoor(GetDoorFromBossRoomToThisRoom(roomNode), DoorLockTypes.Lock_Goal);
                        break;

                    case GrammarSymbols.T_Treasure_Key:
                        SpawnKey(roomNode, rng, KeyTypes.Key);
                        break;

                    case GrammarSymbols.T_Treasure_Key_Multipart:
                        SpawnKey(roomNode, rng, KeyTypes.Key_Multipart);
                        break;

                    case GrammarSymbols.T_Treasure_Key_Goal:
                        SpawnKey(roomNode, rng, KeyTypes.Key_Goal);
                        break;


                } // end switch

            } // end foreach room node

            FinalizeLockedDoors();

        }        

        private static void ClearAnyPreviousSpawnedPrefabs()
        {

            // Destroy any previously spawned items.
            DestroyAllChildGameObjects(_ItemsParent);

            // Destroy any previously spawned objects.
            DestroyAllChildGameObjects(_ObjectsParent);

        }

        private static void LoadPrefabs()        
        {
            if (_Prefab_Object_Chest == null)
                _Prefab_Object_Chest = (GameObject)Resources.Load("Prefabs/Objects/Object_Chest");

            if (_Prefab_Object_ChestGoal == null)
                _Prefab_Object_ChestGoal = (GameObject)Resources.Load("Prefabs/Objects/Object_ChestGoal");

            if (_Prefab_Object_Door == null)
                _Prefab_Object_Door = (GameObject)Resources.Load("Prefabs/Objects/Object_Door");



            if (_Prefab_Item_Key == null)
                _Prefab_Item_Key = (GameObject)Resources.Load("Prefabs/Items/Item_Key");

        }

        private static void SpawnDoor(DungeonDoor doorToSpawn, DoorLockTypes lockType)
        {
            Vector3 offset;
            Quaternion rotation;
            Directions doorDirection = doorToSpawn.ThisRoom_DoorAdjustedDirection;

            if (doorDirection == Directions.North ||
                doorDirection == Directions.South)
            {
                offset = doorDirection == Directions.North ? new Vector3(1.0f, 0.0f) : 
                                                             new Vector3(1.0f, 1.0f);
                rotation = doorToSpawn.ThisRoom_DoorAdjustedDirection.DirectionToRotation(); // We don't flip the direction here like we do for east/west doors. This is because the door object faces south by default, so we don't need to flip the door to make it face north.
            }
            else
            {
                offset = doorDirection == Directions.East ? new Vector3(0.0f, 0.0f) :
                                                            new Vector3(1.0f, 0.0f);
                rotation = doorToSpawn.ThisRoom_DoorAdjustedDirection.FlipDirection().DirectionToRotation();
            }


            // Calculate the center point of the door.
            Vector3 centerPoint = MiscellaneousUtils.GetUpperLeftMostTile(doorToSpawn.ThisRoom_DoorTile1WorldPosition, doorToSpawn.ThisRoom_DoorTile2WorldPosition);
            centerPoint += offset;


            // Spawn a door object and configure it.
            GameObject door = GameObject.Instantiate(_Prefab_Object_Door, centerPoint, rotation, _ObjectsParent.transform);
            Object_Door doorComponent = door.GetComponent<Object_Door>();

            
            // We use the other room node for non-goal locked doors, because these locked doors spawn in the room next to the lock room,
            // thus preventing access to the room.
            if (lockType != DoorLockTypes.Lock_Goal)
                _LockedDoorsDictionary.Add(doorToSpawn.OtherRoom_Node.MissionStructureNode, doorComponent);
            else
                _LockedDoorsDictionary.Add(doorToSpawn.ThisRoom_Node.MissionStructureNode, doorComponent);
            


            doorComponent.Key_ID = _NextKeyID;
            _NextKeyID++;
            
            doorComponent.DoorState = DoorStates.Locked;
            doorComponent.LockType = lockType;

            doorComponent.ClosedSprite = GetObjectSprite("Objects_Door_Closed", _CurrentRoomSet);
            doorComponent.LockedSprite = GetObjectSprite("Objects_Door_Locked", _CurrentRoomSet);
            doorComponent.LockedMultipartSprite = GetObjectSprite("Objects_Door_Locked_Multipart", _CurrentRoomSet);
            doorComponent.LockedGoalSprite = GetObjectSprite("Objects_Door_Locked_Goal", _CurrentRoomSet);

            doorComponent.ToggleState();

        }

        private static void SpawnKey(DungeonGraphNode roomNode, NoiseRNG rng, KeyTypes keyType)
        {
            // Randomly select a key placeholder position.
            int index = rng.RollRandomIntInRange(0, roomNode.RoomBlueprint.KeyPositions.Count - 1);

            // Get the local room coordinates of the selected key spawn point.
            if (roomNode.RoomBlueprint.KeyPositions.Count < 1)
                throw new System.Exception($"DungeonPopulator.SpawnKey() - The room \"{roomNode.RoomBlueprint.RoomName}\" at {roomNode.RoomCenterPoint} does not contain any key spawn points!");

            // Get the key's local position within the parent room.
            Vector3Int keyPosLocal = roomNode.RoomBlueprint.KeyPositions[index];

            // Calculate the world coordinates of the key position.
            Vector3Int keyPosWorld = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(keyPosLocal, roomNode.RoomPosition, roomNode.RoomDirection);


            // Select chest type.
            GameObject chestPrefab;
            if (keyType != KeyTypes.Key_Goal)
                chestPrefab = _Prefab_Object_Chest;
            else
                chestPrefab = _Prefab_Object_ChestGoal;

            // Spawn a chest containing a key.
            GameObject chest = GameObject.Instantiate(chestPrefab, _ItemOffsetVector + keyPosWorld, Quaternion.identity, _ObjectsParent.transform);
            
            // Get the Inventory component and add it to our dictionary to track it for a later pass to setup the key/lock pairs.
            Inventory chestInventory = chest.GetComponent<Inventory>();
            _KeyChestsDictionary.Add(roomNode.MissionStructureNode, chestInventory);

            // Setup the chest's sprite properties.
            Object_Chest objChest = chest.GetComponent<Object_Chest>();
            if (keyType != KeyTypes.Key_Goal)
            {
                objChest.ClosedSprite = GetObjectSprite("Objects_Chest_Closed", _CurrentRoomSet);
                objChest.OpenSprite = GetObjectSprite("Objects_Chest_Open", _CurrentRoomSet);
            }
            else
            {
                objChest.ClosedSprite = GetObjectSprite("Objects_ChestGoal_Closed", _CurrentRoomSet);
                objChest.OpenSprite = GetObjectSprite("Objects_ChestGoal_Open", _CurrentRoomSet);
            }

            objChest.GetComponent<SpriteRenderer>().sprite = objChest.ClosedSprite;

        }

        private static void FinalizeLockedDoors()
        {
            foreach (KeyValuePair<MissionStructureGraphNode, Inventory> pair in _KeyChestsDictionary)
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

                        lockedDoor.MultipartKeyCount++;

                        KeyTypes keyType = KeyTypes.Key;
                        if (lockedDoor.LockType == DoorLockTypes.Lock)
                            keyType = KeyTypes.Key;
                        else if (lockedDoor.LockType == DoorLockTypes.Lock_Multipart)
                            keyType = KeyTypes.Key_Multipart;
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

        private static DungeonDoor GetDoorFromBossRoomToThisRoom(DungeonGraphNode roomNode)
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

        private static Sprite GetItemSprite(string spriteName, string roomSet)
        {
            return GetSprite(spriteName, roomSet, "Items");
        }

        private static Sprite GetObjectSprite(string spriteName, string roomSet)
        {
            return GetSprite(spriteName, roomSet, "Objects");
        }

        private static Sprite GetSprite(string spriteName, string roomSet, string type)
        {
            Sprite sprite;
            Dictionary<string, Sprite> dict;
            

            _SpritesDictionary.TryGetValue(roomSet, out dict);
            if (dict != null)
            {
                dict.TryGetValue(spriteName, out sprite);

                if (sprite != null)
                    return sprite;
            }
            else
            {
                dict = new Dictionary<string, Sprite>();
                _SpritesDictionary.Add(roomSet, dict);
            }


            string spritesPath = ScriptableRoomUtilities.GetRoomSetSpritesPath(roomSet);
            sprite = Resources.Load<Sprite>($"{spritesPath}/{type}/{spriteName}");
            dict.Add(spriteName, sprite);

            return sprite;
        }


    }

}
