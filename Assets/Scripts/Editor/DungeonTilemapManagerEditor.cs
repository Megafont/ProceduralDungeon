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


        SerializedProperty _EnemiesMap;
        SerializedProperty _FloorsMap;
        SerializedProperty _ItemsMap;
        SerializedProperty _PlaceholdersMap;
        SerializedProperty _WallsMap;



        void OnEnable()
        {
            _DungeonTilemapManager = (DungeonTilemapManager)target;


            _EnemiesMap = serializedObject.FindProperty("_EnemiesMap");
            _FloorsMap = serializedObject.FindProperty("_FloorsMap");
            _ItemsMap = serializedObject.FindProperty("_ItemsMap");
            _PlaceholdersMap = serializedObject.FindProperty("_PlaceholdersMap");
            _WallsMap = serializedObject.FindProperty("_WallsMap");
        }



        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //DrawDefaultInspector();



            // Unity Tilemap references
            // ----------------------------------------------------------------------------------------------------

            EditorGUILayout.LabelField("Tilemap References", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_EnemiesMap);
            EditorGUILayout.PropertyField(_FloorsMap);
            EditorGUILayout.PropertyField(_ItemsMap);
            EditorGUILayout.PropertyField(_PlaceholdersMap);
            EditorGUILayout.PropertyField(_WallsMap);



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

                menu.AddItem(new GUIContent("Clear Enemies"), false, HandleClearTilemapMenuSelection, "Enemies");
                menu.AddItem(new GUIContent("Clear Floors"), false, HandleClearTilemapMenuSelection, "Floors");
                menu.AddItem(new GUIContent("Clear Items"), false, HandleClearTilemapMenuSelection, "Items");
                menu.AddItem(new GUIContent("Clear Placeholders"), false, HandleClearTilemapMenuSelection, "Placeholders");
                menu.AddItem(new GUIContent("Clear Walls"), false, HandleClearTilemapMenuSelection, "Walls");
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
                case "Enemies":
                    _DungeonTilemapManager.DungeonMap.ClearTileMap(TileMapTypes.Enemies);
                    break;
                case "Floors":
                    _DungeonTilemapManager.DungeonMap.ClearTileMap(TileMapTypes.Floors);
                    break;
                case "Items":
                    _DungeonTilemapManager.DungeonMap.ClearTileMap(TileMapTypes.Items);
                    break;
                case "Placeholders":
                    _DungeonTilemapManager.DungeonMap.ClearTileMap(TileMapTypes.Placeholders);
                    break;
                case "Walls":
                    _DungeonTilemapManager.DungeonMap.ClearTileMap(TileMapTypes.Walls);
                    break;

                default:
                    throw new System.ArgumentException($"DungeonTilemapManagerEditor.HandleClearTilemapMenuSelection() - Received an invalid parameter: \"{parameter}\"");
            }


            Debug.Log($"RoomTileMapEditor: Cleared {map} of the dungeon.");
        }


    }

}