using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.Utilities;
using ProceduralDungeon.DungeonGeneration.Utilities.PlaceholderUtilities;


namespace ProceduralDungeon.DungeonGeneration
{

    public class DungeonGraph
    {
        List<DungeonGraphNode> _Nodes;
        DungeonGraphNode _StartRoom;
        DungeonGraphNode _GoalRoom;



        public DungeonGraphNode GoalNode { get { return _GoalRoom; } }
        public List<DungeonGraphNode> Nodes { get { return _Nodes; } }
        public DungeonGraphNode StartNode { get { return _StartRoom; } }



        public DungeonGraph(DungeonGraphNode startNode)
        {
            _Nodes = new List<DungeonGraphNode>();

            _Nodes.Add(startNode);
            _StartRoom = startNode;
        }



        public void ClearAllNodes()
        {
            _Nodes.Clear();
        }

        public DungeonGraphNode AddNode(DungeonGraphNode newNode, DungeonGraphNode parent)
        {
            _Nodes.Add(newNode);

            parent.AddNeighbor(newNode);
            newNode.AddNeighbor(parent);

            return newNode;
        }

        /// <summary>
        /// Creates a new room using the specified RoomData (blueprint) and connects it to the specified door on a previous room.
        /// </summary>
        /// <param name="previousRoom">The room to connect the new room to.</param>
        /// <param name="previousRoomDoor">The door on this room to connect the new room to.</param>
        /// <param name="roomToConnect">A RoomData object containing the blueprint of the new room.</param>
        /// <param name="room2Door">The door on the new room to connect to the specified door on the previous room.</param>
        /// <returns>A DungeonGraphNode object for the newly generated room.</returns>
        public DungeonGraphNode GenerateNewRoomAndConnectToPrevious(DungeonGraphNode previousRoom, DoorData previousRoomDoor, RoomData roomToConnect, DoorData room2Door)
        {
            // Get the direction of the door on room 1 and adjust it to take into account that room's rotation direction.
            Directions room1Door_AdjustedDirection = MiscellaneousUtils.AddRotationDirectionsTogether(previousRoomDoor.DoorDirection, previousRoom.RoomDirection);

            // Get the coordinates of both tiles of the previous room's door and adjust them to take into account the room's rotation direction.
            Vector3Int room1Door_Tile1AdjustedLocalPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(previousRoomDoor.Tile1Position, previousRoom.RoomPosition, previousRoom.RoomDirection);
            Vector3Int room1Door_Tile2AdjustedLocalPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(previousRoomDoor.Tile2Position, previousRoom.RoomPosition, previousRoom.RoomDirection);

            // Get the upper-left-most of the two adjusted tile positions.
            Vector3Int room1Door_AdjustedLocalPos = MiscellaneousUtils.GetUpperLeftMostTile(room1Door_Tile1AdjustedLocalPos, room1Door_Tile2AdjustedLocalPos);



            // Get the direction the new room's door needs to face to be able to connect to the specified door on the first room.
            Directions room2DoorTargetDirection = MiscellaneousUtils.FlipDirection(room1Door_AdjustedDirection);

            // Figure out the rotation of the new room based on the direction the door being connected needs to face to connect properly.
            Directions room2Direction = DungeonConstructionUtils.CalculateRoomRotationFromDoorRotation(room2Door.DoorDirection, room2DoorTargetDirection);



            // Get the coordinates of both tiles of the new room's door and adjust them to take into account the room's rotation direction.
            Vector3Int room2Door_Tile1AdjustedLocalPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(room2Door.Tile1Position, Vector3Int.zero, room2Direction); // We use Vector3Int.zero here since we just want to adjust the door position with no translation since we don't know the second room's position yet.
            Vector3Int room2Door_Tile2AdjustedLocalPos = DungeonConstructionUtils.AdjustTileCoordsForRoomPositionAndRotation(room2Door.Tile2Position, Vector3Int.zero, room2Direction);

            // Get the upper-left-most of the two adjusted tile positions.
            Vector3Int room2Door_AdjustedLocalPos = MiscellaneousUtils.GetUpperLeftMostTile(room2Door_Tile1AdjustedLocalPos, room2Door_Tile2AdjustedLocalPos);



            // Calculate the position of the new room's door based on the position of the door it is connecting to.
            Vector3Int room2Door_WorldPos = PlaceholderUtils_Doors.CalculateDoorPositionFromConnectedDoor(room1Door_AdjustedLocalPos + previousRoom.RoomPosition, room1Door_AdjustedDirection);

            // Calculate the position of the new room based on the 1st room's door.
            Vector3Int room2Pos = room2Door_WorldPos + -room2Door_AdjustedLocalPos;

            // Create a DungeonGraphNode for the new room and add it to the dungeon graph.
            DungeonGraphNode newNode = new DungeonGraphNode(roomToConnect, room2Pos, room2Direction, previousRoom.DistanceFromStart + 1);
            AddNode(newNode, previousRoom);


            // Return the new node to the calling code.
            return newNode;

        }


    }

}
