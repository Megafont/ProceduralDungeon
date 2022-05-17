
using UnityEngine;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.DungeonGeneration.DungeonConstruction.PlaceholderUtilities
{
    public struct DoorData
    {
        public Directions DoorDirection; // The direction this door is facing.
        public RoomLevels DoorLevel; // The level this door is on.



        private Vector3Int _TilePosition1;
        private Vector3Int _TilePosition2;



        // These properties get the positions of the two tiles of this door. 
        // The first property always returns the upper-left-most of the two tiles, and the second one returns the other tile.
        public Vector3Int Tile1Position
        {
            get
            {
                if (_TilePosition1.x < _TilePosition2.x || _TilePosition1.y > _TilePosition2.y)
                    return _TilePosition1;
                else
                    return _TilePosition2;
            }
        }
        public Vector3Int Tile2Position
        {
            get
            {
                if (_TilePosition1.x < _TilePosition2.x || _TilePosition1.y > _TilePosition2.y)
                    return _TilePosition2;
                else
                    return _TilePosition1;
            }
        }



        public void SetDoorTilePositions(Vector3Int tilePosition1, Vector3Int tilePosition2)
        {
            _TilePosition1 = tilePosition1;
            _TilePosition2 = tilePosition2;
        }


    }

}