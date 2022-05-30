using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

using ToolboxLib_Shared.Math;

using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.InGame.Items;
using ProceduralDungeon.InGame.Objects;


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;


namespace ProceduralDungeon.DungeonGeneration.DungeonConstruction
{

    public static class DungeonPopulator
    {
        private static GameObject _ItemsParent;
        private static GameObject _ObjectsParent;

        private static GameObject _Prefab_Door;
        private static GameObject _Prefab_Key;
        private static GameObject _Prefab_KeyMultipart;
        private static GameObject _Prefab_KeyGoal;
        
        private static List<Item_Key> _MultipartKeysList;

        private static Vector3 _ItemOffsetVector = new Vector3(0.5f, 0.5f);

        private static uint _NextKeyID;
        private static uint _NextKeyMultipartID;
        private static uint _NextKeyGoalID;




        public static void PopulateDungeon(DungeonGraph dungeonGraph, NoiseRNG rng)
        {
            Assert.IsNotNull(dungeonGraph, "DungeonPopulator.PopulateDungeon() - The passed in dungeon graph is null!");
            Assert.IsNotNull(rng, "DungeonPopulator.PopulateDungeon() - The passed in random number generator is null!");


            // Initialize the multipart key list.
            _MultipartKeysList = new List<Item_Key>();


            // Reset the key ID counters.
            _NextKeyID = 0;
            _NextKeyMultipartID = 10000;
            _NextKeyGoalID = 20000;


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
                        SpawnDoor(GetDoorFromParentRoomToThisRoom(roomNode), DoorStates.Locked, DoorLockTypes.Lock);
                        break;

                    case GrammarSymbols.T_Lock_Multi:
                        SpawnDoor(GetDoorFromParentRoomToThisRoom(roomNode), DoorStates.Locked, DoorLockTypes.Lock_Multipart);
                        break;

                    case GrammarSymbols.T_Lock_Goal:
                        SpawnDoor(GetDoorFromBossRoomToThisRoom(roomNode), DoorStates.Locked, DoorLockTypes.Lock_Goal);
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

        }        

        private static void ClearAnyPreviousSpawnedPrefabs()
        {

            // Destroy any previously spawned items.
            DestroyAllChildGameObjects(_ItemsParent);

            // Destroy any previously spawned objects.
            DestroyAllChildGameObjects(_ObjectsParent);

        }


        private static void SpawnDoor(DungeonDoor doorToSpawn, DoorStates doorState, DoorLockTypes lockType)
        {
            Vector3 offset = Vector3.zero;
            Quaternion rotation;
            if (doorToSpawn.ThisRoom_DoorAdjustedDirection == Directions.North ||
                doorToSpawn.ThisRoom_DoorAdjustedDirection == Directions.South)
            {
                offset = new Vector3(1.0f, 0.5f);
                rotation = doorToSpawn.ThisRoom_DoorAdjustedDirection.DirectionToRotation(); // We don't flip the direction here like we do for east/west doors. This is because the door object faces south by default, so we don't need to flip the door to make it face north.
            }
            else
            {
                offset = new Vector3(0.5f, 0.0f);
                rotation = doorToSpawn.ThisRoom_DoorAdjustedDirection.FlipDirection().DirectionToRotation();
            }


            // Calculate the center point of the door.
            Vector3 centerPoint = MiscellaneousUtils.GetUpperLeftMostTile(doorToSpawn.ThisRoom_DoorTile1WorldPosition, doorToSpawn.ThisRoom_DoorTile2WorldPosition);
            centerPoint += offset;


            // Get the key prefab.
            if (_Prefab_Door == null)
                _Prefab_Door = (GameObject)Resources.Load("Prefabs/Objects/Object_Door");


            // Spawn a door object and configure it.
            GameObject door = GameObject.Instantiate(_Prefab_Door, centerPoint, rotation, _ObjectsParent.transform);
            Object_Door doorComponent = door.GetComponent<Object_Door>();

            uint keyID = 0;
            if (lockType == DoorLockTypes.Lock)
                keyID = _NextKeyID - 1;
            else if (lockType == DoorLockTypes.Lock_Multipart)
            {
                _NextKeyMultipartID++; // Spawning a door with a multipart key lock tells us there are no more multi-part keys in the current set, so we can increment that counter now.
                keyID = _NextKeyMultipartID - 1;
            }
            else if (lockType == DoorLockTypes.Lock_Goal)
                keyID = _NextKeyGoalID - 1;

            doorComponent.Key_ID = keyID;
            
            doorComponent.DoorState = DoorStates.Locked;
            doorComponent.LockType = lockType;
            doorComponent.MultipartKeyCount = GetMultipartKeysCount(keyID);
            doorComponent.ToggleState();

        }

        private static void SpawnKey(DungeonGraphNode roomNode, NoiseRNG rng, KeyTypes keyType)
        {
            // Randomly select a key placeholder position.
            int index = rng.RollRandomIntInRange(0, roomNode.RoomBlueprint.KeyPositions.Count - 1);

            // Get the local room coordinates of the selected key spawn point.
            if (roomNode.RoomBlueprint.KeyPositions.Count < 1)
                throw new System.Exception($"DungeonPopulator.SpawnKey() - The room \"{roomNode.RoomBlueprint.RoomName}\" at {roomNode.RoomCenterPoint} does not contain any key spawn points!");
            Vector3Int keyPosLocal = roomNode.RoomBlueprint.KeyPositions[index];

            // Calculate the world coordinates of the key position.
            Vector3Int keyPosWorld = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(keyPosLocal, roomNode.RoomPosition, roomNode.RoomDirection);

            // Get a key object and configure it.
            if (_Prefab_Key == null)
                _Prefab_Key = (GameObject) Resources.Load("Prefabs/Items/Item_Key");

            // Spawn a key object and configure it.
            GameObject key = GameObject.Instantiate(_Prefab_Key, _ItemOffsetVector + keyPosWorld, Quaternion.identity, _ItemsParent.transform);
            Item_Key keyComponent = key.GetComponent<Item_Key>();


            uint keyID = 0;
            if (keyType == KeyTypes.Key)
            {
                keyID = _NextKeyID;
                _NextKeyID++;
            }
            else if (keyType == KeyTypes.Key_Multipart)
            {
                keyID = _NextKeyMultipartID;
                _MultipartKeysList.Add(keyComponent);
            }
            else if (keyType == KeyTypes.Key_Goal)
            {
                keyID = _NextKeyGoalID;
                _NextKeyGoalID++;
            }


            keyComponent.KeyID = keyID;
            keyComponent.KeyType = keyType;
            keyComponent.UpdateSprite();

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
                if (door.OtherRoom_Node.MissionStructureNode.GrammarSymbol == GrammarSymbols.T_Boss_Main)
                    return door;
            }

            return null;
        }

        private static uint GetMultipartKeysCount(uint keyID)
        {
            uint count = 0;

            foreach (Item_Key key in _MultipartKeysList)
                if (key.KeyID == keyID) { count++; }

            return count;
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
