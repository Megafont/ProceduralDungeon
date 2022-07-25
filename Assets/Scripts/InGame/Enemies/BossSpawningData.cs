using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralDungeon.InGame.Enemies.SpawningData
{

    [CreateAssetMenu(fileName = "New Boss Spawning Data Object", menuName = "Enemies/Boss Spawning Data")]
    public class BossSpawningData : ScriptableObject
    {
        // This holds a list of boss and/or minibosses that can spawn.
        [Tooltip("Specifies a list of bosses and/or minibosses that can spawn.")]
        public GameObject[] Bosses = new GameObject[0];

    }

}
