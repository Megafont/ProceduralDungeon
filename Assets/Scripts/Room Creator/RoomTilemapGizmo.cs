using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;


[ExecuteInEditMode]

public class RoomTilemapGizmo : MonoBehaviour
{
    public static bool EnableRoomTilemapGizmos = true;



    [SerializeField] Color BoundsColor = Color.red;


    private Tilemap _Map;



    void Awake()
    {
        _Map = GetComponent<Tilemap>();
    }


    void OnDrawGizmos()
    {

        if ((!EnableRoomTilemapGizmos) || _Map == null)
        {
            return;
        }


        Vector3 min = _Map.cellBounds.min;
        Vector3 max = _Map.cellBounds.max;

        // Draw the bounding box of the Tilemap.
        Gizmos.color = BoundsColor;
        Gizmos.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z)); // Top
        Gizmos.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z)); // Right
        Gizmos.DrawLine(new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z)); // Bottom
        Gizmos.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(min.x, max.y, min.z)); // Left

    }

}
