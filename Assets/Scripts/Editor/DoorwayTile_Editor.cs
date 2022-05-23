using System;

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.TileMaps;
using ProceduralDungeon.TileMaps.TileTypes;


namespace ProceduralDungeon.EditorScripts
{
    [CustomEditor(typeof(DoorwayTile), false)]
    [CanEditMultipleObjects]
    public class DoorwayTile_Editor : BasicDungeonTile_Editor
    {
        SerializedProperty _ReplacementWallTile;



        void OnEnable()
        {
            _DungeonTile = (BasicDungeonTile)target;

            _ReplacementWallTile = serializedObject.FindProperty("ReplacementWallTile");
        }



        public override void OnInspectorGUI()
        {
            // Draw the default editor from the base clas.
            base.OnInspectorGUI();



            serializedObject.Update();


            EditorGUILayout.LabelField("DoorwayTile Settings", GUI.skin.GetStyle("BoldLabel"));

            ShowSpritePreview((_DungeonTile as DoorwayTile).ReplacementWallTile.sprite, "Replacement Wall Tile Preview");
            EditorGUILayout.PropertyField(_ReplacementWallTile);


            serializedObject.ApplyModifiedProperties();
        }





    }

}