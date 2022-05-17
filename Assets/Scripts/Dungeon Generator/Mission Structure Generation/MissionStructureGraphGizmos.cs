using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;


namespace ProceduralDungeon.DungeonGeneration.MissionStructureGeneration
{
    public static class MissionStructureGraphGizmos
    {
        // This overrides all other constants below.
        public const bool ENABLE_MISSION_STRUCTURE_GRAPH_GIZMOS = true;


        private static List<Vector3> _CirclePoints;

        private static Dictionary<GenerativeGrammar.Symbols, Color32> _Colors;



        private const float _ArrowHeadLength = 0.25f; // How long the arrow heads on lines are.
        private const float _ArrowHeadWidth = 0.25f; // How wide the arrow heads on lines are.

        private const uint _CirclePointCount = 16; // The number of points used to draw circles.

        private static float _NodeDoubleLinesGap = 0.3f; // How big the gap between double lines is.
        private static Vector2 _NodeGap = new Vector2(5, 3); // Spacing of the drawn nodes on the x and y dimensions. This defines the size of the gaps between nodes.
        private static Vector3 _NodeOffset = new Vector3(0, 0, 0); // Defines how much the entire mission structure node graph is offset when drawn.
        private static Vector3 _NodeTextOffset = new Vector3(-0.5f, -0.75f); // Defines how much the node text is offset from the center of the node.
        private static Color32 _NodeTextColor = Color.white;
        private static Color32 _NonTerminalNodeColor = new Color32(255, 191, 0, 255); // Light Orange
        private static Color32 _TerminalNodeColor = new Color32(255, 128, 0, 255); // Orange




        public static void DrawDungeonGraphGizmos()
        {
            if (ENABLE_MISSION_STRUCTURE_GRAPH_GIZMOS)
            {
                if ((!DungeonGenerator.IsInitialized) || DungeonGenerator.MissionStructureGraph == null || DungeonGenerator.MissionStructureGraph.Nodes.Count < 1)
                    return;


                if (_CirclePoints == null)
                {
                    InitCirclePoints();
                    InitColors();
                }


                DrawNodeGizmos();
            }

        }

        private static void DrawNodeGizmos()
        {
            foreach (MissionStructureGraphNode node in DungeonGenerator.MissionStructureGraph.Nodes)
            {
                Vector3 nodePos = new Vector3(node.Position.x * _NodeGap.x, node.Position.y * _NodeGap.y) + _NodeOffset;


                Color32 color = GetColor(node.GrammarSymbol);


                // Draw the nodes.
                // *************************************************************************************************************************************************
                // * TWEAK pos LIKE YOU DID WITH DOORS BASED ON THE ROOM'S ROTATION DIRECTION, WHICH SHOULD FIX IT SO THE GIZMOS ARE CENTERED IN THE ROOMS?        *
                // *************************************************************************************************************************************************
                if (GenerativeGrammar.IsTerminalSymbol(node.GrammarSymbol))
                {
                    DrawCircle(nodePos, color);
                }
                else
                {
                    DrawBox(nodePos, color);
                }

                DrawText(nodePos + _NodeTextOffset,
                         color,
                         "Node:  " + Enum.GetName(typeof(GenerativeGrammar.Symbols), node.GrammarSymbol));


                // Draw the lines between the nodes.
                Vector3 doubleLinesOffset = Vector3.up * (_NodeDoubleLinesGap * 0.5f);

                foreach (MissionStructureGraphNode childNode in node.ChildNodes)
                {
                    Vector3 childNodePos = new Vector3(childNode.Position.x * _NodeGap.x, childNode.Position.y * _NodeGap.y) + _NodeOffset;

                    if (!childNode.IsTightlyCoupled)
                    {
                        // The child node is not tightly coupled to its parent, so draw a single line connecting them.
                        DrawArrowLine(nodePos + Vector3.right * 0.5f,
                                      childNodePos + Vector3.left * 0.5f,
                                      color);
                    }
                    else
                    {
                        // If the child node is tightly coupled to its parent, then draw double lines connecting them rather than a single line.
                        // This is just like the diagrams in the generative grammars for dungeon generation paper linked in GenerativeGrammar.cs.
                        DrawArrowLine((nodePos + Vector3.right * 0.5f) + doubleLinesOffset,
                                      (childNodePos + Vector3.left * 0.5f) + doubleLinesOffset,
                                      color);

                        DrawArrowLine((nodePos + Vector3.right * 0.5f) - doubleLinesOffset,
                                      (childNodePos + Vector3.left * 0.5f) - doubleLinesOffset,
                                      color);
                    }

                } // end foreach childNode


            } // end foreach node

        }

        private static void DrawBox(Vector3 pos, Color32 color)
        {
            Gizmos.color = color;

            Vector3 upperLeft = new Vector3(pos.x - 0.5f, pos.y + 0.5f);
            Vector4 lowerRight = new Vector3(pos.x + 0.5f, pos.y - 0.5f);

            Gizmos.DrawLine(new Vector3(upperLeft.x, upperLeft.y), new Vector3(lowerRight.x, upperLeft.y));   // Top side
            Gizmos.DrawLine(new Vector3(upperLeft.x, lowerRight.y), new Vector3(lowerRight.x, lowerRight.y)); // Bottom side
            Gizmos.DrawLine(new Vector3(upperLeft.x, upperLeft.y), new Vector3(upperLeft.x, lowerRight.y));    // Left side
            Gizmos.DrawLine(new Vector3(lowerRight.x, upperLeft.y), new Vector3(lowerRight.x, lowerRight.y));  // Right side
        }

        private static void DrawText(Vector3 pos, Color32 color, string text)
        {

#if UNITY_EDITOR
            //UnityEditor.Handles.color = color;
            GUI.color = color;
            UnityEditor.Handles.Label(pos, text);
#endif
        }

        private static void DrawCircle(Vector3 pos, Color32 color)
        {
            Vector3 lineStart = Vector3.zero;
            Vector3 lineEnd = Vector3.zero;


            Gizmos.color = color;

            for (int i = 0; i < _CirclePointCount; i++)
            {
                lineStart = pos + _CirclePoints[i];

                if (i < _CirclePointCount - 1)
                    lineEnd = pos + _CirclePoints[i + 1];
                else
                    lineEnd = pos + _CirclePoints[0]; // The last line segment connects back to the first point of the circle.


                Gizmos.DrawLine(lineStart, lineEnd);

            } // end for i

        }

        private static void DrawArrowLine(Vector3 startPos, Vector3 endPos, Color32 color)
        {
            Gizmos.color = color;

            Gizmos.DrawLine(startPos, endPos);

            // Calculate arrow head end points.
            Vector3 headLengthwiseVector = endPos - startPos;
            headLengthwiseVector.Normalize();

            Vector3 headWidthwiseVector = Quaternion.Euler(0f, 0f, 90f) * headLengthwiseVector;
            headWidthwiseVector *= _ArrowHeadWidth * 0.5f; // We cut the head width in half since this vector represents half of the arrow head's base.

            headLengthwiseVector *= _ArrowHeadLength; // We scale this after calculating the head widthwise vector, because otherwise this calculation would screw up that one.

            // Calculate the center point of the base of the arrow head.
            Vector3 arrowHeadCenterPoint = endPos - headLengthwiseVector;

            // Calculate the two corner points of the arrow head's base.
            Vector3 arrowHeadPoint1 = arrowHeadCenterPoint + headWidthwiseVector;
            Vector3 arrowHeadPoint2 = arrowHeadCenterPoint - headWidthwiseVector;


            Gizmos.DrawLine(arrowHeadCenterPoint, endPos);
            Gizmos.DrawLine(arrowHeadPoint1, endPos);
            Gizmos.DrawLine(arrowHeadPoint2, endPos);
            Gizmos.DrawLine(arrowHeadPoint1, arrowHeadPoint2);

        }


        private static Color32 GetColor(GenerativeGrammar.Symbols symbol)
        {
            Color32 color = Color.white;

            if (!_Colors.TryGetValue(symbol, out color))
            {
                if (GenerativeGrammar.IsTerminalSymbol(symbol))
                    color = _TerminalNodeColor;
                else
                    color = _NonTerminalNodeColor;
            }

            return color;
        }

        private static void InitColors()
        {
            _Colors = new Dictionary<GenerativeGrammar.Symbols, Color32>();

            _Colors.Add(GenerativeGrammar.Symbols.T_Entrance, Color.green);
            _Colors.Add(GenerativeGrammar.Symbols.T_Goal, Color.green);
            _Colors.Add(GenerativeGrammar.Symbols.T_Boss_Main, Color.red);
            _Colors.Add(GenerativeGrammar.Symbols.T_Boss_Mini, new Color32(179, 0, 0, 255));
            _Colors.Add(GenerativeGrammar.Symbols.T_Lock, new Color32(0, 128, 32, 255));
            _Colors.Add(GenerativeGrammar.Symbols.T_Lock_Goal, new Color32(102, 0, 0, 255));
            _Colors.Add(GenerativeGrammar.Symbols.T_Lock_Multi, new Color32(0, 128, 32, 255));
            _Colors.Add(GenerativeGrammar.Symbols.T_Treasure_Bonus, Color.yellow);
            _Colors.Add(GenerativeGrammar.Symbols.T_Treasure_Key_Goal, Color.cyan);
            _Colors.Add(GenerativeGrammar.Symbols.T_Treasure_Key, new Color32(0, 153, 204, 255));
            _Colors.Add(GenerativeGrammar.Symbols.T_Treasure_Key_Multipart, new Color32(0, 153, 204, 255));
            _Colors.Add(GenerativeGrammar.Symbols.T_Treasure_MainDungeonItem, Color.blue);
            _Colors.Add(GenerativeGrammar.Symbols.T_Test, new Color32(77, 0, 153, 255));
            _Colors.Add(GenerativeGrammar.Symbols.T_Test_Combat, new Color32(77, 0, 153, 255));
            _Colors.Add(GenerativeGrammar.Symbols.T_Test_MainDungeonItem, new Color32(0, 0, 153, 255));
            _Colors.Add(GenerativeGrammar.Symbols.T_Test_PreviousItem, new Color32(77, 0, 153, 255));
            _Colors.Add(GenerativeGrammar.Symbols.T_Test_Secret, new Color32(77, 0, 153, 255));



        }

        private static void InitCirclePoints()
        {
            _CirclePoints = new List<Vector3>();

            Vector3 vector = Vector3.right * 0.5f;
            float rotationIncrement = 360f / _CirclePointCount;

            for (int i = 0; i < _CirclePointCount; i++)
            {
                // Calculate the next point by rotating the starting vector around the origin.
                Vector3 point = Quaternion.Euler(0f, 0f, rotationIncrement * i) * vector;

                _CirclePoints.Add(point);

            } // end for i

        }


    }
}