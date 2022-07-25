using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame
{
    /// <summary>
    /// Enumerates the various types of damage.
    /// </summary>
    /// <remarks>
    /// NOTE: All values in this enum have explicity numbers to keep settings from getting messed up every time you
    ///       modify this enum. Otherwise, adding or removing a value from the list will screw up the value stored
    ///       in any GameObject that has a script with a DamageType property. That also means that if the numbers
    ///       assigned to values in this enum are changed, some or all GameObjects with a script with a DamageType
    ///       property will need to have that property reset to the proper value in the Unity inspector.
    /// </remarks>
    public enum DamageTypes
    {
        Normal = 0,
        BombBlast  = 10000,
        BossContact = 20000,
        MinibossContact = 30000,
        EnemyContact = 40000,
        Projectile = 50000,
        Spikes = 60000,
        Weapon = 70000,
    }


}
