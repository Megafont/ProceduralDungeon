using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ToolboxLib_Shared.Math;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.InGame.Enemies;
using ProceduralDungeon.TileMaps;


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;


namespace ProceduralDungeon.DungeonGeneration.DungeonConstruction
{
    internal struct MinMax
    {
        public int Min;
        public int Max;
    }



    public static class DungeonEnemySpawner
    {
        const uint MIN_ENEMIES_PER_ROOM = 1;
        const uint MAX_ENEMIES_PER_ROON = 5;


        private static List<GameObject> _EnemyPrefabs;
        
        private static GameObject _EnemiesParent;

        // The min and max distances of the rooms in each spawn tier. The distances are from the dungeon's start room to each room in the tier.
        private static List<MinMax> _EnemyTierDistanceRanges;

        private static EnemySpawningData _EnemySpawningData;
        private static NoiseRNG _RNG;


        public static void SpawnEnemiesInDungeon()
        {
            Init();


            List<SavedTile> availableSpawnPositions = new List<SavedTile>();
            foreach (MissionStructureGraphNode node in DungeonGenerator.MissionStructureGraph.Nodes)
            {
                availableSpawnPositions.Clear();
                availableSpawnPositions.AddRange(node.DungeonRoomNode.RoomBlueprint.Enemy_Placeholders);


                // Try to get the enemy list for the spawning tier this room is in.
                EnemySpawningData.EnemySpawningTier enemiesOnThisTier;
                if (node.LockCount >= _EnemySpawningData.EnemySpawningTiers.Length)
                {
                    Debug.LogError($"DungeonEnemySpawner.SpawnEnemiesInDungeon() - The current room is in enemy spawning tier {node.LockCount}, but the spawning data only specifies enemies up to tier index {_EnemySpawningData.EnemySpawningTiers.Length - 1}.");
                    continue;
                }

                enemiesOnThisTier = _EnemySpawningData.EnemySpawningTiers[node.LockCount];

                int roomTargetSpawnCount = CalculateEnemySpawnCountForRoom(node);
                int roomSpawnCount = 0;

                //Debug.LogError($"Room: {node.GrammarSymbol}    SpawnPositions: {availableSpawnPositions.Count}    TargetSpawnCount: {roomTargetSpawnCount}");

                // Spawn enemies in the current room.
                while (true)
                {                   
                    // If there are no more available spawn positions, then just exit this loop and begin working on the next one.
                    if (availableSpawnPositions.Count == 0 || roomSpawnCount >= roomTargetSpawnCount)
                        break;


                    // Select a random spawner.
                    int spawnerIndex = _RNG.RollRandomIntInRange(0, availableSpawnPositions.Count - 1);

                    // Select an enemy from the spawning tier.
                    int enemyIndex = _RNG.RollRandomIntInRange(0, enemiesOnThisTier.EnemyList.Count - 1);

                    // Spawn the enemy.
                    SavedTile spawnPlaceholder = availableSpawnPositions[spawnerIndex];
                    Vector3 spawnPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(spawnPlaceholder.Position, 
                                                                                                           node.DungeonRoomNode.RoomPosition, 
                                                                                                           node.DungeonRoomNode.RoomFinalDirection);
                    GameObject enemy = GameObject.Instantiate(enemiesOnThisTier.EnemyList[enemyIndex], 
                                                              spawnPos, 
                                                              Quaternion.identity, 
                                                              _EnemiesParent.transform);


                    // Add the enemy to the parent room's enemies list.
                    node.DungeonRoomNode.Enemies.Add(enemy);

                    // Remove the spawn position we just used from our temporary list.
                    availableSpawnPositions.Remove(spawnPlaceholder);

                    roomSpawnCount++;

                } // foreach spawn position

            } // foreach MissionStructureGraphNode

        }
       
        private static void Init()
        {
            // Find the parent game object of spawned enemies.
            _EnemiesParent = GameObject.Find("SpawnedEnemies");

            // Get the random number generator.
            _RNG = DungeonGenerator.RNG_InGame;

            // Get the distance ranges for each enemy range (how far the closest and farthest rooms in that tier are from the dungeon's starting room).
            GetEnemySpawnTierDistanceRanges();

            // Get the enemy spawning data.
            _EnemySpawningData = DungeonGenerator.DungeonTilemapManager.EnemySpawningData;
        }

        private static int CalculateEnemySpawnCountForRoom(MissionStructureGraphNode roomNode)
        {
            if (IsRoomTypeThatIsNotAllowedToSpawnEnemies(roomNode))
                return 0;


            MinMax tierSpawnRange = _EnemyTierDistanceRanges[(int) roomNode.LockCount];

            // Calculate the midpoint of the distances from start for the rooms in this enemy spawning tier.
            int distanceRangeForTier = (tierSpawnRange.Max - tierSpawnRange.Min) / 2;

            int baseSpawnCount = (int) roomNode.LockCount + 2;

            // Calculate half of the base spawn rate for this room.
            float margin = baseSpawnCount / 2;

            // Calculate the room's position within the spawning count range for its spawning tier.
            float percent = (roomNode.DungeonRoomNode.DistanceFromStart - (float) tierSpawnRange.Min) / (tierSpawnRange.Max - tierSpawnRange.Min);

            // Apply this to the margin.
            margin *= percent;

            // Add a little randomness to the spawn count for this room.
            float perturbAmount = _RNG.RollRandomFloat_NegOneToOne() * (margin * 2);
            perturbAmount -= margin; // Make it so half of the values are negative.

            // Apply the randomness to the base spawn amount for this room.
            int finalSpawnCount = (int) Mathf.Round(baseSpawnCount + perturbAmount);
            finalSpawnCount = Mathf.Max(0, finalSpawnCount);

            // Make it so most rooms in tier 0 have a minimum of 1 enemy.
            /*if (finalSpawnCount < 1)
            {
                if (_RNG.RollRandomFloat_ZeroToOne() <= 0.8f)
                    finalSpawnCount = 1;
            }
            */

            return finalSpawnCount;
        }

        private static bool IsRoomTypeThatIsNotAllowedToSpawnEnemies(MissionStructureGraphNode roomNode)
        {
            if (roomNode.GrammarSymbol == GrammarSymbols.T_Entrance || roomNode.GrammarSymbol == GrammarSymbols.T_Goal ||
                roomNode.GrammarSymbol == GrammarSymbols.T_Boss_Mini || roomNode.GrammarSymbol == GrammarSymbols.T_Boss_Main || 
                roomNode.GrammarSymbol == GrammarSymbols.T_Secret_Room)
            {
                return true;
            }


            return false;
        }

        private static void GetEnemySpawnTierDistanceRanges()
        {
            int lockRoomCount = DungeonGenerator.MissionStructureGraph.GetLockRoomCount();


            // Initialize the dinstance ranges list with default values.
            _EnemyTierDistanceRanges = new List<MinMax>();
            for (int i = 0; i <= lockRoomCount; i++)
            {
                _EnemyTierDistanceRanges.Add(new MinMax() { Min = int.MaxValue, Max = int.MinValue } );
            }


            // Find each enemy spawning tier's rooms that are closest and farthest from the start room of the dungeon.
            foreach (MissionStructureGraphNode node in DungeonGenerator.MissionStructureGraph.Nodes)
            {
                int distance = (int) node.DungeonRoomNode.DistanceFromStart;
                int lockCount = (int)node.LockCount;

                MinMax minMax = _EnemyTierDistanceRanges[lockCount];


                if (distance < minMax.Min)
                    minMax.Min = distance;
                else if (distance > minMax.Max)
                    minMax.Max = distance;

                _EnemyTierDistanceRanges[lockCount] = minMax;

            } // end foreach MissionStructureGraphNode

        }


    }

}
