using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

using ProceduralDungeon.TileMaps;

using Object = UnityEngine.Object;


namespace ProceduralDungeon.EditorScripts
{
    [CustomEditor(typeof(RoomTile))]
    //[CanEditMultipleObjects]
    public class RoomTileEditor : Editor
    {
        SerializedProperty _RotateWithRoom;
        SerializedProperty _TileType;



        /// <summary>
        /// The RuleTile being edited
        /// </summary>
        public Tile _Tile;



        void OnEnable()
        {
            _Tile = (Tile)target;

            _RotateWithRoom = serializedObject.FindProperty("RotateWithRoom");
            _TileType = serializedObject.FindProperty("TileType");
        }



        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //base.OnInspectorGUI();


            //Texture2D texture = tile.sprite.texture; 
            //Texture2D texture = RenderStaticPreview(AssetDatabase.GetAssetPath(tile), null, 64, 64);
            Texture2D texture = AssetPreview.GetAssetPreview(_Tile.sprite);
            if (texture != null)
            {
                Texture2D preview = ScaleTexture(texture, 64, 64);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Preview");
                GUI.color = _Tile.color;
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


            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel("Sprite");
            EditorGUILayout.BeginVertical();
            _Tile.sprite = (Sprite)EditorGUILayout.ObjectField(_Tile.sprite, typeof(Sprite), true);


            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Adding a FlexibleSpace before the button within the same horizontal group causes the button to be aligned to the right side of its parent group.

            if (GUILayout.Button("Sprite Editor...", new GUILayoutOption[] { GUILayout.Width(100) }))
            {
                Type type = System.Type.GetType("UnityEditor.U2D.Sprites.SpriteEditorWindow,Unity.2D.Sprite.Editor");

                // Select the sprite this tile is using, so the Sprite Editor window will open it.
                Selection.activeObject = _Tile.sprite; //AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(tile.sprite));

                // Open the Sprite Editor window.
                EditorApplication.ExecuteMenuItem("Window/2D/Sprite Editor");
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();


            _Tile.color = EditorGUILayout.ColorField("Color", _Tile.color);
            _Tile.colliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup("Collider Type", _Tile.colliderType);


            EditorGUILayout.LabelField("RoomTile Settings", GUI.skin.GetStyle("BoldLabel"));

            EditorGUILayout.PropertyField(_RotateWithRoom);
            EditorGUILayout.PropertyField(_TileType);



            serializedObject.ApplyModifiedProperties();
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
        private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
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