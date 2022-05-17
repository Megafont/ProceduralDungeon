using System;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;


namespace ProceduralDungeon.DungeonGeneration.MissionStructureGeneration
{
    /// <summary>
    /// This class represents the grammar used to generate our mission graphs, as well as the replacement
    /// rules used to generate those graphs.
    /// </summary>
    public static class GenerativeGrammar
    {

        // ****************************************************************************************************
        // * ALWAYS UPDATE THIS VALUE IF YOU ADD OR REMOVE ANY NON-TERMINAL VALUES FROM THE ENUM BELOW!       *
        // ****************************************************************************************************

        // This item MUST always point to the last non-terminal symbol's index.
        private const int LAST_NON_TERMINAL_INDEX = (int)Symbols.NT_Hook;



        /// <summary>
        /// This enumeration defines the alphabet of the generative grammer we use to generate our mission graphs.
        /// </summary>
        /// <remarks>
        /// This generative grammar idea is based off of the work of Joris Dormins. He wrote a paper on cyclic dungeon generation using generative grammars,
        /// and is one of the developers behind the procedurally generated Unexplored games. He has some videos on YouTube as well, but I found his paper here in .PDF form:
        /// https://www.are.na/block/2378426
        /// 
        /// See the diagrams in that .pdf file to understand how this generative dungeon grammar works.
        /// </remarks>
        public enum Symbols
        {
            // Non-Terminal Symbols (these get replaced with one or more terminal symbols according to the grammar rules to generate random dungeon graphs);
            NT_Start = 0,
            NT_Chain,
            NT_Chain_DungeonConclusion,
            NT_Chain_Linear,
            NT_Chain_Parallel,
            NT_Fork,
            NT_Gate,
            NT_Hook,




            // Terminal Symbols (ones that cannot be replaced).
            T_Boss_Mini, // A mini boss.
            T_Boss_Main, // The main boss.
            T_Exploration, // An empty room that just adds to exploration in the dungeon.
            T_Entrance, // The entrance room.
            T_Goal, // The goal room.
            T_Lock, // A lock opened by a regular key.
            T_Lock_Goal, // The lock blocking the final goal of the area (like a boss room).
            T_Lock_Multi, // A lock opened by a multipart key.
            T_Treasure_Bonus, // Something like a chest with money or a health item, a mineable resource node, etc.
            T_Treasure_Key, // A regular key.
            T_Treasure_Key_Goal, // The master key that unlocks the final challenge in the area (like a boss key in Zelda games).
            T_Treasure_Key_Multipart, // A key that is broken into several parts that the player must find and collect.
            T_Treasure_MainDungeonItem, // The main treasure in the area (like the boomerang, bombs, or hookshot in Zelda games).
            T_Test, // This is a room that is randomly either of type T_Test_PreviousItem or T_Test_Combat.
            T_Test_Combat, // A room that tests the player in combat (kill all enemies).
            T_Test_MainDungeonItem, // A puzzle room that tests the player's ability to use the main item gained in the area to proceed.
            T_Test_PreviousItem, // A puzzle room that tests the player's ability to use any item acquired so far.
            T_Test_Secret, // Same as T_Test, but this one's test unlocks a secret.
        }



        public static bool IsNonTerminalSymbol(Symbols symbol)
        {
            if ((int)symbol <= LAST_NON_TERMINAL_INDEX)
                return true;
            else
                return false;
        }

        public static bool IsTerminalSymbol(Symbols symbol)
        {
            if ((int)symbol > LAST_NON_TERMINAL_INDEX)
                return true;
            else
                return false;
        }


    }

}