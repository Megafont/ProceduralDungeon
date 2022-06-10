using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;

using ProceduralDungeon.DungeonGeneration.DungeonConstruction.PlaceholderUtilities;
using ProceduralDungeon.RoomCreator;
using ProceduralDungeon.TileMaps;

using LoadRoomReturnCodes = ProceduralDungeon.RoomCreator.RoomTilemapManager.LoadRoomReturnCodes;
using SaveRoomReturnCodes = ProceduralDungeon.RoomCreator.RoomTilemapManager.SaveRoomReturnCodes;


namespace ProceduralDungeon.EditorScripts
{
    [CustomEditor(typeof(RoomTilemapManager))]
    public class RoomTilemapManager_Editor : Editor
    {
        RoomTilemapManager _RoomTilemapManager = null;


        SerializedProperty _FloorsMap;
        SerializedProperty _WallsMap;
        SerializedProperty _Placeholders_Objects_Map;
        SerializedProperty _Placeholders_Items_Map;
        SerializedProperty _Placeholders_Enemies_Map;


        SerializedProperty _RoomName;
        SerializedProperty _RoomSet;
        SerializedProperty _RoomLevel;
        SerializedProperty _RoomTypeFlags;



        void OnEnable()
        {
            _RoomTilemapManager = (RoomTilemapManager)target;


            _FloorsMap = serializedObject.FindProperty("_FloorsMap");
            _WallsMap = serializedObject.FindProperty("_WallsMap");
            _Placeholders_Objects_Map = serializedObject.FindProperty("_Placeholders_Objects_Map");
            _Placeholders_Items_Map = serializedObject.FindProperty("_Placeholders_Items_Map");
            _Placeholders_Enemies_Map = serializedObject.FindProperty("_Placeholders_Enemies_Map");

            _RoomName = serializedObject.FindProperty("_RoomName");
            _RoomSet = serializedObject.FindProperty("_RoomSet");
            _RoomLevel = serializedObject.FindProperty("_RoomLevel");
            _RoomTypeFlags = serializedObject.FindProperty("_RoomTypeFlags");

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



            // Room settings section
            // ----------------------------------------------------------------------------------------------------

            EditorGUILayout.LabelField("Room Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_RoomName);
            EditorGUILayout.PropertyField(_RoomSet);
            EditorGUILayout.PropertyField(_RoomLevel);
            EditorGUILayout.PropertyField(_RoomTypeFlags);



            // Commands section
            // ----------------------------------------------------------------------------------------------------

            EditorGUILayout.LabelField("Commands", EditorStyles.boldLabel);


            if (GUILayout.Button("Compress Bounds to Fit on All Tile Maps"))
            {
                _RoomTilemapManager.RoomMap.CompressBoundsOfAllTileMaps();
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


            if (GUILayout.Button("Save Room Asset"))
            {
                string filePath = ScriptableRoomUtilities.GetRoomFilePath(_RoomTilemapManager.RoomName, _RoomTilemapManager.RoomSet);
                if (File.Exists(filePath))
                {
                    if (!EditorUtility.DisplayDialog("Save Room Asset", $"The room file \"{filePath}\" already exists. Do you want to overwrite it? If not, you can change the name of the room and try again.", "Yes", "No"))
                        return;
                }


                SaveRoomReturnCodes result = _RoomTilemapManager.SaveRoom();
                if (result != SaveRoomReturnCodes.Success)
                {
                    if (result == SaveRoomReturnCodes.Error_InvalidPlaceholders)
                        EditorUtility.DisplayDialog("Save Room Asset", $"The room \"{_RoomTilemapManager.RoomName}\" has one or more invalid door placeholders. See the Unity debug console for specific errors.", "Ok");
                    else if (result == SaveRoomReturnCodes.Error_InvalidTiles)
                        EditorUtility.DisplayDialog("Save Room Asset Error", $"Could not save the room, because at least one tile map was found to have an invalid tile type in it while attempting to save the room asset {_RoomTilemapManager.RoomName}! See the Unity console for the specific tiles.", "Ok");

                    return;
                };


                Debug.Log($"RoomTileMapEditor: Saved room asset \"{filePath}\".");
            }


            if (EditorGUILayout.DropdownButton(new GUIContent("Load Room Asset..."), FocusType.Passive, styleButton))
            {
                GenericMenu menu = new GenericMenu();

                foreach (string file in Directory.GetFiles(ScriptableRoomUtilities.GetRoomSetPath(_RoomTilemapManager.RoomSet)))
                {
                    // We need to check if the file has the .asset extension so we don't add the .meta files for each asset into the menu.
                    if (file.EndsWith(".asset"))
                    {
                        string fileWithoutExt = Path.GetFileNameWithoutExtension(file);
                        menu.AddItem(new GUIContent(fileWithoutExt), false, HandleLoadMenuSelection, fileWithoutExt);
                    }
                }

                menu.ShowAsContext();
            }


            if (EditorGUILayout.DropdownButton(new GUIContent("Delete Room Asset..."), FocusType.Passive, styleButton))
            {
                GenericMenu menu = new GenericMenu();

                foreach (string file in Directory.GetFiles(ScriptableRoomUtilities.GetRoomSetPath(_RoomTilemapManager.RoomSet)))
                {
                    // We need to check if the file has the .asset extension so we don't add the .meta files for each asset into the menu.
                    if (file.EndsWith(".asset"))
                    {
                        string fileWithoutExt = Path.GetFileNameWithoutExtension(file);
                        menu.AddItem(new GUIContent(fileWithoutExt), false, HandleDeleteMenuSelection, fileWithoutExt);
                    }
                }

                menu.ShowAsContext();
            }


            serializedObject.ApplyModifiedProperties();
        }


        private void HandleLoadMenuSelection(object parameter)
        {
            string roomName = Path.GetFileNameWithoutExtension((string)parameter);
            string filePath = ScriptableRoomUtilities.GetRoomFilePath(roomName, _RoomTilemapManager.RoomSet);

            if (!File.Exists(filePath))
            {
                Debug.Log($"The room file \"{filePath}\" does not exist!");
                return;
            }

            if (!EditorUtility.DisplayDialog("Load Room Asset", $"If you load the room asset \"{filePath}\", any unsaved changes to the current room will be lost! Do you want to load the room specified in the Inspector anyway?", "Yes", "No"))
                return;


            LoadRoomReturnCodes result = _RoomTilemapManager.LoadRoom(roomName);
            if (result != LoadRoomReturnCodes.Success)
            {
                if (result == LoadRoomReturnCodes.Error_FileNotFound)
                    EditorUtility.DisplayDialog("Load Room Asset Error", $"Failed to load the room asset {_RoomTilemapManager.RoomName}! The file was not found or something else went wrong.", "Ok");
                else if (result == LoadRoomReturnCodes.Error_InvalidTiles)
                    EditorUtility.DisplayDialog("Load Room Asset Error", $"At least one tile map was found to have an invalid tile type in it while attempting to load the room asset {roomName}! See the Unity console for the specific tiles.", "Ok");

                return;
            };


            Debug.Log($"RoomTileMapEditor: Loaded room asset \"{filePath}\".");
        }

        private void HandleClearTilemapMenuSelection(object parameter)
        {
            string map = (parameter.ToString() == "ALL") ? "all tile maps" : $"the \"{parameter}\" tile map";

            if (!EditorUtility.DisplayDialog("Clear Room Tile Map", $"Are you sure you want to clear \"{map}\" for this room?", "Yes", "No"))
                return;


            switch (parameter)
            {
                case "ALL":
                    _RoomTilemapManager.RoomMap.ClearAllTileMaps();
                    break;
                case "Walls":
                    _RoomTilemapManager.RoomMap.ClearTileMap(TileMapTypes.Walls);
                    break;
                case "Floors":
                    _RoomTilemapManager.RoomMap.ClearTileMap(TileMapTypes.Floors);
                    break;
                case "Placeholders_Objects":
                    _RoomTilemapManager.RoomMap.ClearTileMap(TileMapTypes.Placeholders_Objects);
                    break;
                case "Placeholders_Items":
                    _RoomTilemapManager.RoomMap.ClearTileMap(TileMapTypes.Placeholders_Items);
                    break;
                case "Placeholders_Enemies":
                    _RoomTilemapManager.RoomMap.ClearTileMap(TileMapTypes.Placeholders_Enemies);
                    break;

                default:
                    throw new System.ArgumentException($"RoomTilemapManagerEditor.HandleClearTilemapMenuSelection() - Received an invalid parameter: \"{parameter}\"");
            }


            Debug.Log($"RoomTileMapEditor: Cleared {map} for room \"{_RoomTilemapManager.RoomName}\".");
        }

        private void HandleDeleteMenuSelection(object parameter)
        {
            string roomName = Path.GetFileNameWithoutExtension((string)parameter);
            string filePath = ScriptableRoomUtilities.GetRoomFilePath(roomName, _RoomTilemapManager.RoomSet);

            if (!File.Exists(filePath))
            {
                Debug.Log($"The room file \"{filePath}\" does not exist!");
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete Room Asset", $"If you delete the room asset \"{filePath}\", it will be gone for good! Do you want to delete it anyway?", "Yes", "No"))
                return;


            File.Delete(filePath);
            File.Delete(filePath + ".meta");

            Debug.Log($"RoomTileMapEditor: Deleted room asset \"{filePath}\".");
        }


        public static void SaveRoomAsset(ScriptableRoom room, string roomSet)
        {
            AssetDatabase.CreateAsset(room, ScriptableRoomUtilities.GetRoomFilePath(room.RoomName, roomSet));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


    }

}