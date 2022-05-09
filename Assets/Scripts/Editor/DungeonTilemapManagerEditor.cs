using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.RoomCreator;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.EditorScripts
{
    [CustomEditor(typeof(DungeonTilemapManager))]
    public class DungeonTilemapManagerEditor : Editor
    {
        DungeonTilemapManager _DungeonTilemapManager = null;


        SerializedProperty _Placeholders_Enemies_Map;
        SerializedProperty _FloorsMap;
        SerializedProperty _Placeholders_Items_Map;
        SerializedProperty _Placeholders_General_Map;
        SerializedProperty _WallsMap;



        void OnEnable()
        {
            _DungeonTilemapManager = (DungeonTilemapManager)target;


            _FloorsMap = serializedObject.FindProperty("_FloorsMap");
            _WallsMap = serializedObject.FindProperty("_WallsMap");
            _Placeholders_General_Map = serializedObject.FindProperty("_Placeholders_General_Map");
            _Placeholders_Items_Map = serializedObject.FindProperty("_Placeholders_Items_Map");
            _Placeholders_Enemies_Map = serializedObject.FindProperty("_Placeholders_Enemies_Map");
        }



        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //DrawDefaultInspector();



            // Unity Tilemap references
            // ----------------------------------------------------------------------------------------------------

            EditorGUILayout.LabelField("Tilemap References", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_FloorsMap);
            EditorGUILayout.PropertyField(_WallsMap);
            EditorGUILayout.PropertyField(_Placeholders_General_Map);
            EditorGUILayout.PropertyField(_Placeholders_Items_Map);
            EditorGUILayout.PropertyField(_Placeholders_Enemies_Map);



            // Commands section
            // ----------------------------------------------------------------------------------------------------

            EditorGUILayout.LabelField("Commands", EditorStyles.boldLabel);


            if (GUILayout.Button("Generate Test Dungeon"))
            {
                DungeonGenerator.Init(_DungeonTilemapManager);
                DungeonGenerator.GenerateDungeon();
            }


            if (GUILayout.Button("Compress Bounds to Fit on All Tile Maps"))
            {
                _DungeonTilemapManager.DungeonMap.ShrinkAllTileMapBoundsToFit();
            }


            GUIStyle styleButton = GUI.skin.GetStyle("Button");
            if (EditorGUILayout.DropdownButton(new GUIContent("Clear Room Tile Map..."), FocusType.Passive, styleButton))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Clear Floors"), false, HandleClearTilemapMenuSelection, "Floors");
                menu.AddItem(new GUIContent("Clear Walls"), false, HandleClearTilemapMenuSelection, "Walls");
                menu.AddItem(new GUIContent("Clear Items"), false, HandleClearTilemapMenuSelection, "Placeholders_Items");
                menu.AddItem(new GUIContent("Clear Placeholders"), false, HandleClearTilemapMenuSelection, "Placeholders_General");
                menu.AddItem(new GUIContent("Clear Enemies"), false, HandleClearTilemapMenuSelection, "Placeholders_Enemies");
                menu.AddItem(new GUIContent("Clear ALL Tile Maps"), false, HandleClearTilemapMenuSelection, "ALL");

                menu.ShowAsContext();
            }


            serializedObject.ApplyModifiedProperties();
        }



        private void HandleClearTilemapMenuSelection(object parameter)
        {
            string map = (parameter.ToString() == "ALL") ? "all tile maps" : $"the \"{parameter}\" tile map";

            if (!EditorUtility.DisplayDialog("Clear Dungeon Tile Map", $"Are you sure you want to clear \"{map}\" for this room?", "Yes", "No"))
                return;


            switch (parameter)
            {
                case "ALL":
                    _DungeonTilemapManager.DungeonMap.ClearAllTileMaps();
                    break;
                case "Floors":
                    _DungeonTilemapManager.DungeonMap.ClearTileMap(TileMapTypes.Floors);
                    break;
                case "Walls":
                    _DungeonTilemapManager.DungeonMap.ClearTileMap(TileMapTypes.Walls);
                    break;
                case "Placeholders_General":
                    _DungeonTilemapManager.DungeonMap.ClearTileMap(TileMapTypes.Placeholders_General);
                    break;
                case "Placeholders_Items":
                    _DungeonTilemapManager.DungeonMap.ClearTileMap(TileMapTypes.Placeholders_Items);
                    break;
                case "Placeholders_Enemies":
                    _DungeonTilemapManager.DungeonMap.ClearTileMap(TileMapTypes.Placeholders_Enemies);
                    break;

                default:
                    throw new System.ArgumentException($"DungeonTilemapManagerEditor.HandleClearTilemapMenuSelection() - Received an invalid parameter: \"{parameter}\"");
            }


            Debug.Log($"RoomTileMapEditor: Cleared {map} of the dungeon.");
        }


    }

}