using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;

using ProceduralDungeon.InGame.Objects;
using ProceduralDungeon.InGame.Enemies.SpawningData;


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;


namespace ProceduralDungeon.InGame.Objects
{

    public class Object_BossBattleTrigger : MonoBehaviour
    {
        private bool _BossFightStarted;



        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag("Player") && !_BossFightStarted)
            {
                Vector3 playerPos = collision.gameObject.transform.position;

                DungeonGraphNode roomNode = DungeonGenerator.LookupRoomFromTile(Vector3Int.FloorToInt(playerPos));

                ActivateBossFight(roomNode);
            }

        }



        private void ActivateBossFight(DungeonGraphNode roomNode)
        {
            _BossFightStarted = true;


            // Close the boss room doors.
            foreach (Object_Door door in roomNode.PuzzleActivatedDoors)
            {
                door.SetOpen(false);
            }


            // Start the boss fight.
            SpawnBoss();
        }


        private void SpawnBoss()
        {           
            DungeonGraphNode parentRoom = DungeonGenerator.LookupRoomFromTile(Vector3Int.FloorToInt(transform.position));

            BossSpawningData bossList;

            GameObject parent;



            parent = GameObject.Find("SpawnedEnemies");
            if (parent == null)
                throw new Exception("Object_BossBattleTrigger.SelectBossToSpawn() - The \"SpawnedEnemies\" container game object is missing!");



            if (parentRoom.MissionStructureNode.GrammarSymbol == GrammarSymbols.T_Boss_Mini)
            {
                bossList = DungeonGenerator.DungeonTilemapManager.MiniBossSpawningData;
                if (bossList.Bosses.Length < 1)
                    throw new Exception("Object_BossBattleTrigger.SelectBossToSpawn() - Attempted to spawn a miniboss, but the list of minibosses is empty!");
            }
            else if (parentRoom.MissionStructureNode.GrammarSymbol == GrammarSymbols.T_Boss_Main)
            {
                bossList = DungeonGenerator.DungeonTilemapManager.BossSpawningData;
                if (bossList.Bosses.Length < 1)
                    throw new Exception("Object_BossBattleTrigger.SelectBossToSpawn() - Attempted to spawn a boss, but the list of bosses is empty!");

            }
            else
            {
                throw new Exception("Object_BossBattleTrigger.SelectBossToSpawn() - Attempted to spawn a boss in a room that is not of type T_Boss_Mini or T_Boss_Main!");
            }



            int bossIndex = DungeonGenerator.RNG_InGame.RollRandomIntInRange(0, bossList.Bosses.Length - 1);

            Instantiate(bossList.Bosses[bossIndex], parentRoom.RoomCenterPoint, Quaternion.identity, parent.transform);
        }


    }

}
