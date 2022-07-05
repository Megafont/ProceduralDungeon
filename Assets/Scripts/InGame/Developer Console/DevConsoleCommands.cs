using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ToolboxLib_Shared.DeveloperConsole;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonConstruction;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.DungeonGeneration.MissionStructureGeneration;
using ProceduralDungeon.TileMaps;


using GrammarSymbols = ProceduralDungeon.DungeonGeneration.MissionStructureGeneration.GenerativeGrammar.Symbols;


namespace ProceduralDungeon.InGame.DevConsoleCommands
{

    public static class DevConsoleCommands
    {
        private static GameObject _Player;



        public static void InitDevConsoleCommands()
        {
            DevConsole.RegisterConsoleCommand("Warp", 
                                              "Warp roomTypeIndex [roomIndex] | Warp roomTypeName [roomIndex]", 
                                              new string[] { "Warps the player to the specified room index.",
                                                             "If multiple rooms of the specified type exist in the dungeon, an optional second parameter specifies the index of the one to warp to.",
                                                             "For example, an index of 2 will send the player to the 3rd room of that type. If there are fewer instances of the room type than that in the dungeon,",
                                                             "then it will warp the player to the last room of that type.",
                                                             "NOTE: This optional parameter is automatically clamped to the valid range. For example, if it is too large, then the player is warped to the last room",
                                                             "of the specified type in the dungeon." },
                                                             new DevConsoleCommandExecutionFunction(ExecCmd_Warp),
                                                             DevConsoleAccessLevels.Public,
                                                             new byte[] { 1, 2 });
        }

        

        public static bool ExecCmd_Warp(string[] tokensList)
        {
            if (_Player == null)
                _Player = GameObject.Find("Player");


            GrammarSymbols roomType;
            if (!TryParseRoomType(tokensList[1], out roomType))
                return false;


            // Get all dungeon rooms of the specified type.
            List<DungeonGraphNode> rooms = GetRoomsOfType(roomType);
            if (rooms.Count < 1)
            {
                DevConsole.PostMessage("No rooms of the specified type were found to warp to!", "Error");
                return false;
            }

            int index = -1;
            if (tokensList.Length == 3 && !int.TryParse(tokensList[2], out index))
            {
                DevConsole.PostMessage("The index parameter is invalid!", "Error");
                return false;
            }

            //DevConsole.PostMessage($"Index: {index}", "Debug");

            // Clamp index to the valid range of indices.
            if (index <= 0)
                index = 0;
            else if (index >= rooms.Count - 1)
                index = rooms.Count - 1;


            // Get the room at the specified index.
            DungeonGraphNode roomNode = rooms[index];


            // Get all spawnable positions within the specified room.
            List<Vector3Int> spawnablePositions = GetSpawnablePositionsInRoom(roomNode);

            if (spawnablePositions.Count < 1)
            {
                DevConsole.PostMessage("Warp failed! No available spawn positions were found in that room.", "Error");
                return false;
            }



            // Randomly choose a spawnable space in the room.
            int randIndex = DungeonGenerator.RNG_InGame.RollRandomIntInRange(0, spawnablePositions.Count - 1);
            Vector3 warpPos = spawnablePositions[randIndex] + new Vector3(0.5f, 0.5f, 0.0f); // We add 0.5f to both X and Y so the player spawns centered on the chosen tile.

            // Warp the player
            _Player.transform.position = warpPos;
            DevConsole.PostMessage($"Warped player to position {warpPos}.");

            return true;

        }


        private static List<DungeonGraphNode> GetRoomsOfType(GrammarSymbols roomType)
        {
            List<DungeonGraphNode> rooms = new List<DungeonGraphNode>();

            foreach (DungeonGraphNode roomNode in DungeonGenerator.DungeonGraph.Nodes)
            {
                if (roomNode.MissionStructureNode.GrammarSymbol == roomType)
                    rooms.Add(roomNode);
            }

            return rooms;
        }

        private static List<Vector3Int> GetSpawnablePositionsInRoom(DungeonGraphNode roomNode)
        {
            List<Vector3Int> positionsInRoom = DungeonGenerator.GetTilePositionsInRoom(roomNode);
            List<Vector3Int> spawnablePositions = new List<Vector3Int>();

            DungeonMap dungeonMap = DungeonGenerator.DungeonTilemapManager.DungeonMap;

            LayerMask layerMask = (1 << LayerMask.NameToLayer("Walls")) |
                                  (1 << LayerMask.NameToLayer("Enemies")) |
                                  (1 << LayerMask.NameToLayer("Objects"));


            foreach (Vector3Int pos in positionsInRoom)
            {
                if (dungeonMap.WallsMap.GetTile(pos) == null &&
                    dungeonMap.Placeholders_Objects_Map.GetTile(pos) == null &&
                    dungeonMap.Placeholders_Enemies_Map.GetTile(pos) == null)                
                {
                    // We use a radius less than one to ensure our circle doesn't touch the collider of anything that may be on the neighboring tile.
                    Collider2D[] colliders = Physics2D.OverlapCircleAll(new Vector2(pos.x, pos.z), 0.8f, layerMask);
                    if (colliders.Length < 1 && !DungeonGenerator.IsFakeTile(pos))
                        spawnablePositions.Add(pos);

                    //Debug.LogError($"POS: {pos}    COUNT: {colliders.Length}");
                    
                }

            } // end foreach pos


            //Debug.LogError($"Spawnable positions: {spawnablePositions.Count}");

            return spawnablePositions;
        }

        private static bool TryParseRoomType(string roomTypeParam, out GrammarSymbols roomType)
        {
            // This is defaulted to -1 so that it is invalid by default to ensure we catch the error if it never gets set to a valid value.
            roomType = (GrammarSymbols)(-1);


            int value;
            if (int.TryParse(roomTypeParam, out value))
            {
                roomType = (GrammarSymbols)value;
                if (Enum.IsDefined(typeof(GrammarSymbols), roomType))                
                    return true;
            }


            try
            {
                if (Enum.TryParse<GrammarSymbols>(roomTypeParam.Trim(), out roomType)) // This can throw an exception.               
                {
                    if (Enum.IsDefined(typeof(GrammarSymbols), roomType))
                    {
                        //DevConsole.PostMessage($"RoomType: \"{roomType}\"", "Debug");
                        return true;
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                }
                else
                    throw new ArgumentException();
            }
            catch (Exception)
            {
                DevConsole.PostMessage("The specified room type is invalid!", "Error");
                // We don't return false here on purpose so that the code below can run to see if the value is a valid integer.
            }


            return false;

        }


    }

}