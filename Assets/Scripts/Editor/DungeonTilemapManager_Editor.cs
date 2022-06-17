using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.InGame;
using ProceduralDungeon.InGame.Items;
using ProceduralDungeon.RoomCreator;
using ProceduralDungeon.TileMaps;


namespace ProceduralDungeon.EditorScripts
{
    [CustomEditor(typeof(DungeonTilemapManager))]
    public class DungeonTilemapManager_Editor : Editor
    {
        DungeonTilemapManager _DungeonTilemapManager = null;


        SerializedProperty _FloorsMap;
        SerializedProperty _WallsMap;
        SerializedProperty _Placeholders_Objects_Map;
        SerializedProperty _Placeholders_Items_Map;
        SerializedProperty _Placeholders_Enemies_Map;

        SerializedProperty _ItemDatabase;
        SerializedProperty _Player;
        SerializedProperty _RoomSet;



        void OnEnable()
        {
            _DungeonTilemapManager = (DungeonTilemapManager)target;


            _FloorsMap = serializedObject.FindProperty("_FloorsMap");
            _WallsMap = serializedObject.FindProperty("_WallsMap");
            _Placeholders_Objects_Map = serializedObject.FindProperty("_Placeholders_Objects_Map");
            _Placeholders_Items_Map = serializedObject.FindProperty("_Placeholders_Items_Map");
            _Placeholders_Enemies_Map = serializedObject.FindProperty("_Placeholders_Enemies_Map");

            _ItemDatabase = serializedObject.FindProperty("_ItemDatabase");
            _Player = serializedObject.FindProperty("_Player");
            _RoomSet = serializedObject.FindProperty("_RoomSet");
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
            EditorGUILayout.PropertyField(_Placeholders_Objects_Map);
            EditorGUILayout.PropertyField(_Placeholders_Items_Map);
            EditorGUILayout.PropertyField(_Placeholders_Enemies_Map);



            // Dungeon Generation Parameters
            // ----------------------------------------------------------------------------------------------------

            EditorGUILayout.LabelField("Dungeon Generation Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_ItemDatabase);
            EditorGUILayout.PropertyField(_Player);
            EditorGUILayout.PropertyField(_RoomSet);



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
                _DungeonTilemapManager.DungeonMap.CompressBoundsOfAllTileMaps();
            }


            GUIStyle styleButton = GUI.skin.GetStyle("Button");
            if (EditorGUILayout.DropdownButton(new GUIContent("Clear Room Tile Map..."), FocusType.Passive, styleButton))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Clear Floors"), false, HandleClearTilemapMenuSelection, "Floors");
                menu.AddItem(new GUIContent("Clear Walls"), false, HandleClearTilemapMenuSelection, "Walls");
                menu.AddItem(new GUIContent("Clear Objects"), false, HandleClearTilemapMenuSelection, "Placeholders_Objects");
                menu.AddItem(new GUIContent("Clear Items"), false, HandleClearTilemapMenuSelection, "Placeholders_Items");
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
                case "Placeholders_Objects":
                    _DungeonTilemapManager.DungeonMap.ClearTileMap(TileMapTypes.Placeholders_Objects);
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