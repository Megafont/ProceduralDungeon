using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;

using ProceduralDungeon.InGame.Objects;


namespace ProceduralDungeon.InGame
{

    public static class InGameUtils
    {
        /// <summary>
        /// Specifies how many rooms behind the player that room resets happen at. The idea is to not reset the room directly next
        /// to the one the player is in to hopefully avoid the player seeing anything reset, such as ice blocks.
        /// </summary>
        const uint ROOM_RESET_DELAY = 2;


        private static Queue<DungeonGraphNode> _RecentlyVisitedRooms = new Queue<DungeonGraphNode>();
        private static DungeonGraphNode _MostRecentlyVisitedRoom;



        public static void CheckRoomPuzzleState(DungeonGraphNode roomNode)
        {
            bool state = CheckButtonsStates(roomNode);

            TogglePuzzleDoors(roomNode, state);
        }

        private static bool CheckButtonsStates(DungeonGraphNode roomNode)
        {
            bool result = roomNode.Buttons.Count > 0 ? true : false;
            
            foreach (Object_Button btn in roomNode.Buttons)
            {
                if (!btn.IsPressed)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        public static void TogglePuzzleDoors(DungeonGraphNode roomNode, bool state)
        {
            if (roomNode == null)
                return;


            foreach (Object_Door door in roomNode.ClosedPuzzleDoors)
            {
                // We use the ternary operator here to make it so that if this code is triggered again, it toggles the state
                // of the puzzle doors. That way when the player steps on a button, he can see that it opens the door, but only while it is pressed.
                door.DoorState = state ? DoorStates.Open : DoorStates.Closed;
                door.ToggleState();
            }

        }

        public static void PlayerVisitedRoom(DungeonGraphNode roomNode)
        {
            if (roomNode == null)
                return;

            if (roomNode != _MostRecentlyVisitedRoom)
            {
                _RecentlyVisitedRooms.Enqueue(roomNode);
                _MostRecentlyVisitedRoom = roomNode;
            }


            while (_RecentlyVisitedRooms.Count > ROOM_RESET_DELAY &&
                   _MostRecentlyVisitedRoom != _RecentlyVisitedRooms.Peek())
            {
                DungeonGraphNode room = _RecentlyVisitedRooms.Dequeue();
                ResetRoom(room);
            }

        }

        /// <summary>
        /// Resets certain elements of the specified room during gameplay.
        /// </summary>
        /// <param name="roomNode">The room to reset.</param>
        private static void ResetRoom(DungeonGraphNode roomNode)
        {
            LayerMask layerMask = LayerMask.NameToLayer("Objects");

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(layerMask);            
            filter.useLayerMask = true;


            bool needsReset = false;
            foreach (Object_Button button in roomNode.Buttons)
            {
                if (!button.IsPressed)
                {
                    // This ice block is not on a button, so reset it to its spawn position.
                    // That way players can leave a room and return if an ice block gets stuck in an unmovable position.
                    needsReset = true;           
                }

            } // end foreach iceBlock


            if (needsReset)
            {
                foreach (GameObject iceBlock in roomNode.IceBlocks)
                    iceBlock.GetComponent<Object_IceBlock>().ResetToSpawnPosition();
            }

        }


    }


}
