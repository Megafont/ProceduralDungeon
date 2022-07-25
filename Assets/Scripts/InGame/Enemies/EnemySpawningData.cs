using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Enemies.SpawningData
{
    [CreateAssetMenu(fileName = "New Enemy Spawning Data Object", menuName = "Enemies/Enemy Spawning Data")]
    public class EnemySpawningData : ScriptableObject
    {
        // This holds a list of enemies that can spawn in each enemy spawning tier.
        [Tooltip("Specifies a list of enemies for each spawning tier.")]
        public EnemySpawningTier[] EnemySpawningTiers = new EnemySpawningTier[0];



        [Serializable]
        public struct EnemySpawningTier
        {
            public List<GameObject> EnemyList;
        }


    }

}
