using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.TileMaps
{
    /// <summary>
    /// Stores the start and ending tile IDs for each category of room tiles.
    /// </summary>
    public enum DungeonTileCategoryRanges
    {

        FLOORS_START = 0,
        FLOORS_END = 9999,

        WALLS_START = 10000,
        WALLS_END = 99999,

        PLACEHOLDERS_OBJECTS_START = 100000,
        PLACEHOLDERS_OBJECTS_END = 119999,

        PLACEHOLDERS_ITEMS_START = 120000,
        PLACEHOLDERS_ITEMS_END = 139999,

        PLACEHOLDERS_ENEMIES_START = 140000,
        PLACEHOLDERS_ENEMIES_END = 159999,
    }



    /// <summary>
    /// Enumerates the tile types.
    /// </summary>
    /// <remarks>
    /// NOTE: Each value in this enum should be given an explicit value.
    ///       Otherwise you may have issues with saving and loading having certain tiles
    ///       changed to inappropriate types since C# will automatically give each enum
    ///       value a number. If you remove or add values in the list, the indices will get
    ///       shifted, causing the problem if you save tiles using just the number.
    /// </remarks>
    public enum DungeonTileTypes
    {

        None = -1,


        // ************************************************************************************************************************
        // *  NOTE: DON'T FORGET TO UPDATE THE RoomTileCategoryRanges ENUM ABOVE IF NECESSARY WHEN YOU MODIFY the enum!.          *
        // ************************************************************************************************************************


        // FLOORTILES
        // ====================================================================================================

        Floors_Basement = 0,
        Floors_1stFloor = 1,
        Floors_2ndFloor = 2,
        Floors_Stairs = 3,
        Floors_EntranceFloor = 4,
        Floors_ExitFloor = 5,



        // WALL TILES
        // ====================================================================================================

        // Straight Wall Tiles
        // --------------------
        Walls = 10000,
        Walls_Corner = 10001,
        Walls_DoorFrame_Left = 10002,
        Walls_DoorFrame_Right = 10003,
        Walls_Doorway_Top = 10004,
        Walls_Top = 10005,
        Walls_Top_Corner = 10006,



        // PLACEHOLDER TILES
        // ====================================================================================================

        // Placeholder Tiles - Objects
        Placeholders_Objects_Doors_Basement = 100000,
        Placeholders_Objects_Doors_1stFloor = 100001,
        Placeholders_Objects_Doors_2ndFloor = 100002,
        Placeholders_Objects_Doors_EntryOrExit = 100003,

        Placeholders_Objects_Spikes = 100020,
        Placeholders_Objects_IceBlock = 100021,
        Placeholders_Objects_StoneBlock = 100022,
        Placeholders_Objects_Button = 100023,

        // Placeholder Tiles - Items
        Placeholders_Items_Key = 120000,
        Placeholders_Items_Key_Multipart = 120001,
        Placeholders_Items_Key_Goal = 120002,
        Placeholders_Items_RandomTreasure = 120003,


        // Placeholder Tiles - Enemies


    }

}