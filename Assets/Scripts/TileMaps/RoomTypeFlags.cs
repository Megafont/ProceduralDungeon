using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.TileMaps
{
    /// <summary>
    /// Emuerates the room sets. A room set is just a subfolder of the Resources\Rooms\ folder.
    /// </summary>
    public enum RoomSets
    {
        Test,
    }



    /// <summary>
    /// Enumerates the floors a room can be on.
    /// </summary>
    public enum RoomLevels
    {
        Level_Basement,
        Level_1stFloor,
        Level_2ndFloor
    }



    /// <summary>
    /// Enumerates the room types.
    /// </summary>
    /// <remarks>
    /// NOTE: Each value in this enum should be given an explicit value.
    ///       Otherwise you may have issues with saving and loading having certain room types
    ///       changed to inappropriate types since C# will automatically give each enum
    ///       value a number. If you remove or add values in the list, the indices will get
    ///       shifted, causing the problem if you save the room type using just the number.
    /// </remarks>
    [Flags]
    public enum RoomTypeFlags
    {
        Start = 1,
        Goal = 2,
        Treasure = 4,


        Miniboss = 1024,
        Boss = 2048,
    }

}