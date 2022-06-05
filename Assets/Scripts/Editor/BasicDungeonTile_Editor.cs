using System;

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.TileMaps;
using ProceduralDungeon.TileMaps.TileTypes;


namespace ProceduralDungeon.EditorScripts
{
    [CustomEditor(typeof(BasicDungeonTile))]
    //[CanEditMultipleObjects]
    public class BasicDungeonTile_Editor : Editor
    {
        SerializedProperty _RotateWithRoom;
        SerializedProperty _TileType;



        /// <summary>
        /// The tile being edited
        /// </summary>
        public BasicDungeonTile _DungeonTile;



        void OnEnable()
        {
            _DungeonTile = (BasicDungeonTile)target;

            _RotateWithRoom = serializedObject.FindProperty("RotateWithRoom");
            _TileType = serializedObject.FindProperty("TileType");
        }



        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ShowSpritePreview(_DungeonTile.sprite, "Preview");
            /*
            //Texture2D texture = _Tile.sprite.texture; 
            //Texture2D texture = RenderStaticPreview(AssetDatabase.GetAssetPath(tile), null, 64, 64);
            Texture2D texture = AssetPreview.GetAssetPreview(_DungeonTile.sprite);
            if (texture != null)
            {
                Texture2D preview = ScaleTexture(texture, 64, 64);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Preview");
                GUI.color = _DungeonTile.color;
                EditorGUILayout.LabelField(new GUIContent(preview), new GUILayoutOption[] { GUILayout.Width(32), GUILayout.Height(32) });
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Preview");
                EditorGUILayout.LabelField("");
                EditorGUILayout.EndHorizontal();
            }
            */

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel("Sprite");
            EditorGUILayout.BeginVertical();

            _DungeonTile.sprite = (Sprite)EditorGUILayout.ObjectField(_DungeonTile.sprite, typeof(Sprite), true);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Adding a FlexibleSpace before the button within the same horizontal group causes the button to be aligned to the right side of its parent group.

            if (GUILayout.Button("Sprite Editor...", new GUILayoutOption[] { GUILayout.Width(100) }))
            {
                Type type = System.Type.GetType("UnityEditor.U2D.Sprites.SpriteEditorWindow,Unity.2D.Sprite.Editor");

                // Select the sprite this tile is using, so the Sprite Editor window will open it.
                Selection.activeObject = _DungeonTile.sprite; //AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(tile.sprite));

                // Open the Sprite Editor window.
                EditorApplication.ExecuteMenuItem("Window/2D/Sprite Editor");
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();


            _DungeonTile.color = EditorGUILayout.ColorField("Color", _DungeonTile.color);
            _DungeonTile.colliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup("Collider Type", _DungeonTile.colliderType);


            EditorGUILayout.LabelField("BasicDungeonTile Settings", GUI.skin.GetStyle("BoldLabel"));


            _DungeonTile.RotateWithRoom = EditorGUILayout.ToggleLeft("Rotate With Room", _DungeonTile.RotateWithRoom);
            _DungeonTile.TileType = (DungeonTileTypes)EditorGUILayout.EnumPopup("Tile Type", _DungeonTile.TileType);

            // These two lines got changed to the pair of lines above, because they caused null reference exceptions when a subclass called this editor's OnInspectorGUI() method.
            //EditorGUILayout.PropertyField(_RotateWithRoom);
            //EditorGUILayout.PropertyField(_TileType);


            // We have to do this if statement since this custom inspector allows you to edit some properties that are inherited from
            // the Tile object's base class. This tells Unity that some data needs to be saved. This is not necessary when only
            // using EditorGUILayout.PropertyField() (to display fields non-inherited fields).
            if (GUI.changed)
                EditorUtility.SetDirty(_DungeonTile);

            serializedObject.ApplyModifiedProperties();
        }


        protected void ShowSpritePreview(Sprite sprite, string labelText = "Preview")
        {
            //Texture2D texture = _Tile.sprite.texture; 
            //Texture2D texture = RenderStaticPreview(AssetDatabase.GetAssetPath(tile), null, 64, 64);
            Texture2D texture = AssetPreview.GetAssetPreview(sprite);

            if (texture != null)
            {
                Texture2D preview = ScaleTexture(texture, 64, 64);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(labelText);
                GUI.color = _DungeonTile.color;
                EditorGUILayout.LabelField(new GUIContent(preview), new GUILayoutOption[] { GUILayout.Width(32), GUILayout.Height(32) });
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(labelText);
                EditorGUILayout.LabelField("");
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Scales a Texture2D.
        /// </summary>
        /// <remarks>
        /// If found this function here: http://jon-martin.com/?p=114
        /// </remarks>
        /// <param name="source">The source texture.</param>
        /// <param name="targetWidth">The width to scale it to.</param>
        /// <param name="targetHeight">The height to scale it to.</param>
        /// <returns>The scaled texture.</returns>
        protected Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
            Color[] rpixels = result.GetPixels(0);
            float incX = (1.0f / (float)targetWidth);
            float incY = (1.0f / (float)targetHeight);
            for (int px = 0; px < rpixels.Length; px++)
            {
                //rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth), incY * ((float)Mathf.Floor(px / targetWidth)));
                int x = (int)(incX * ((float)px % targetWidth) * source.width);
                int y = (int)(incY * ((float)Mathf.Floor(px / targetWidth)) * source.height);
                rpixels[px] = source.GetPixel(x, y);

            }
            result.SetPixels(rpixels, 0);
            result.Apply();
            return result;
        }



        /*
        /// <summary>
        /// Renders a static preview Texture2D for a RuleTile asset
        /// </summary>
        /// <param name="assetPath">Asset path of the RuleTile</param>
        /// <param name="subAssets">Arrays of assets from the given Asset path</param>
        /// <param name="width">Width of the static preview</param>
        /// <param name="height">Height of the static preview </param>
        /// <returns>Texture2D containing static preview for the RuleTile asset</returns>
        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            if (tile.sprite != null)
            {
                Type t = RoomTileEditor.GetType("UnityEditor.SpriteUtility");
                if (t != null)
                {
                    MethodInfo method = t.GetMethod("RenderStaticPreview", new Type[] { typeof(Sprite), typeof(Color), typeof(int), typeof(int) });
                    if (method != null)
                    {
                        object ret = method.Invoke("RenderStaticPreview", new object[] { tile.sprite, tile.color, width, height });
                        if (ret is Texture2D)
                            return ret as Texture2D;
                    }
                }
            }
            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }
        */

        /*
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (tile == null || tile.sprite)
                return null;

            // example.PreviewIcon must be a supported format: ARGB32, RGBA32, RGB24,
            // Alpha8 or one of float formats
            Texture2D tex = new Texture2D(width, height);
            EditorUtility.CopySerialized(AssetPreview.GetAssetPreview(tile.sprite), tex);

            return tex;
        }
        */

        /*
        private static Type GetType(string TypeName)
        {
            var type = Type.GetType(TypeName);
            if (type != null)
                return type;

            var currentAssembly = Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach (var assemblyName in referencedAssemblies)
            {
                var assembly = Assembly.Load(assemblyName);
                if (assembly != null)
                {
                    type = assembly.GetType(TypeName);
                    if (type != null)
                        return type;
                }
            }
            return null;
        }
        */


    }

}