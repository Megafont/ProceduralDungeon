using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.InGame.Objects;


public class Object_BossBattleTrigger : MonoBehaviour
{
    private bool _BossFightStarted;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player" && !_BossFightStarted)
        {
            Vector3 playerPos = collision.gameObject.transform.position;

            DungeonGraphNode roomNode = DungeonGenerator.LookupRoomFromTile(Vector3Int.FloorToInt(playerPos));

            ActivateBossFight(roomNode);
        }

    }



    private void ActivateBossFight(DungeonGraphNode roomNode)
    {
        _BossFightStarted = true;


        // Close the boss room doors.
        foreach (Object_Door door in roomNode.PuzzleActivatedDoors)
        {
            door.SetOpen(false);
        }
        

        // Start the boss fight.

    }

}
