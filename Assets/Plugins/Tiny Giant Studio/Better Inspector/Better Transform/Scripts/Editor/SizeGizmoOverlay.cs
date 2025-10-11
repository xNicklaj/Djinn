using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TinyGiantStudio.BetterInspector
{
    [InitializeOnLoad]
    public static class SizeGizmoOverlay
    {
        static BetterTransformSettings _editorSettings;

        static SizeGizmoOverlay()
        {
            EditorApplication.delayCall += () =>
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                SceneView.duringSceneGui += OnSceneGUI;
            };
        }


        static void OnSceneGUI(SceneView sceneView)
        {
            if (_editorSettings == null) _editorSettings = BetterTransformSettings.instance;

            DrawSize();
        }

        static void DrawSize()
        {
            if (Selection.transforms.Length != 1) return;


            if (_editorSettings.ShowSizeLabelGizmo && _handleLabelStyle == null)
            {
                _handleLabelStyle = new(EditorStyles.largeLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = _editorSettings.SizeGizmoLabelSize
                };
            }

            float unitSizeMultiplier = ScalesManager.instance.CurrentUnitValue();
            int selectedUnit = ScalesManager.instance.SelectedUnit;
            string[] availableUnits = ScalesManager.instance.GetAvailableUnits();
            string unit;
            if (availableUnits.Length > selectedUnit)
                unit = availableUnits[selectedUnit];
            else
                return;

            if (_editorSettings.ShowSizeLabelGizmo)
                UpdateLabelStyles();

            Type transformInspectorType = typeof(BetterTransformEditor);
            ActiveEditorTracker editorTracker = ActiveEditorTracker.sharedTracker;
            Editor[] editors = editorTracker.activeEditors;

            foreach (Editor editor in editors)
                if (editor.GetType() == transformInspectorType)
                {
                    BetterTransformEditor transformInspector = editor as BetterTransformEditor;
                    if (transformInspector == null) return;
                    Transform transform = transformInspector.transform;
                    if (transform == null) return;

                    //Get proper bounds
                    Bounds gizmoBounds = transformInspector.currentBound;
                    gizmoBounds.center = Divide(gizmoBounds.center, transform.lossyScale);
                    gizmoBounds.size = Divide(gizmoBounds.size, transform.lossyScale);

                    //Get transform matrix : position rotation and scale
                    Handles.matrix = Matrix4x4.TRS(transform.position,
                        _editorSettings.CurrentWorkSpace == BetterTransformSettings.WorkSpace.World
                            ? Quaternion.identity
                            : transform.rotation, transform.lossyScale); //New

                    if (_editorSettings.ShowSizeLabelGizmo)
                    {
                        if (gizmoBounds.extents.x != 0)
                            DrawXLabel(transformInspector.currentBound, gizmoBounds, unitSizeMultiplier, unit,
                                _editorSettings.GizmoMaximumDecimalPoints, transform);

                        if (gizmoBounds.extents.y != 0)
                            DrawYLabel(transformInspector.currentBound, gizmoBounds, unitSizeMultiplier, unit,
                                _editorSettings.GizmoMaximumDecimalPoints, transform);

                        if (gizmoBounds.extents.z != 0)
                            DrawZLabel(transformInspector.currentBound, gizmoBounds, unitSizeMultiplier, unit,
                                _editorSettings.GizmoMaximumDecimalPoints, transform);
                    }

                    if (_editorSettings.ShowSizeOutlineGizmo)
                        DrawAxisColoredWireCube(gizmoBounds);
                }
        }

        static void UpdateLabelStyles()
        {
            if (_labelBackgroundTextureX == null)
            {
                _labelBackgroundTextureColorX = _editorSettings.SizeGizmoLabelBackgroundColorX;
                _labelBackgroundTextureX = MakeFlatTexture(_labelBackgroundTextureColorX);
            }

            if (_labelBackgroundTextureColorX != _editorSettings.SizeGizmoLabelBackgroundColorX)
            {
                if (_labelBackgroundTextureX)
                    Object.DestroyImmediate(_labelBackgroundTextureX);
                _labelBackgroundTextureColorX = _editorSettings.SizeGizmoLabelBackgroundColorX;
                _labelBackgroundTextureX = MakeFlatTexture(_labelBackgroundTextureColorX);
            }

            if (_labelBackgroundTextureY == null)
            {
                _labelBackgroundTextureColorY = _editorSettings.SizeGizmoLabelBackgroundColorY;
                _labelBackgroundTextureY = MakeFlatTexture(_labelBackgroundTextureColorY);
            }

            if (_labelBackgroundTextureColorY != _editorSettings.SizeGizmoLabelBackgroundColorY)
            {
                if (_labelBackgroundTextureY)
                    Object.DestroyImmediate(_labelBackgroundTextureY);
                _labelBackgroundTextureColorY = _editorSettings.SizeGizmoLabelBackgroundColorY;
                _labelBackgroundTextureY = MakeFlatTexture(_labelBackgroundTextureColorY);
            }

            if (_labelBackgroundTextureZ == null)
            {
                _labelBackgroundTextureColorZ = _editorSettings.SizeGizmoLabelBackgroundColorZ;
                _labelBackgroundTextureZ = MakeFlatTexture(_labelBackgroundTextureColorZ);
            }

            if (_labelBackgroundTextureColorZ == _editorSettings.SizeGizmoLabelBackgroundColorZ) return;
            if (_labelBackgroundTextureZ)
                Object.DestroyImmediate(_labelBackgroundTextureZ);
            _labelBackgroundTextureColorZ = _editorSettings.SizeGizmoLabelBackgroundColorZ;
            _labelBackgroundTextureZ = MakeFlatTexture(_labelBackgroundTextureColorZ);
        }

        #region Label

        static GUIStyle _handleLabelStyle;
        static Texture2D _labelBackgroundTextureX;
        static Color _labelBackgroundTextureColorX;
        static Texture2D _labelBackgroundTextureY;
        static Color _labelBackgroundTextureColorY;
        static Texture2D _labelBackgroundTextureZ;
        static Color _labelBackgroundTextureColorZ;

        static void DrawXLabel(Bounds currentBound, Bounds gizmoBounds, float unitSizeMultiplier, string unitName,
            int gizmoMaximumDecimalPoints, Transform transform)
        {
            _handleLabelStyle.normal.textColor = _editorSettings.SizeGizmoLabelColorX;
            _handleLabelStyle.normal.background = _labelBackgroundTextureX;

            float size = currentBound.size.x * unitSizeMultiplier;

            string xLabel = "";
            if (_editorSettings.ShowAxisOnLabel)
                xLabel += "X: ";

            if (size is > 0 and < 0.000001f)
                xLabel += " Almost 0";
            else
                xLabel += (float)Math.Round(size, gizmoMaximumDecimalPoints);

            if (_editorSettings.ShowUnitOnLabel)
                xLabel += " " + unitName;

            GUIContent label = new(xLabel);
            Vector3 position1;
            if (_editorSettings.PositionLabelAtCenter)
                position1 = gizmoBounds.center + new Vector3(0, 0, gizmoBounds.extents.z);
            else
                position1 = gizmoBounds.center + new Vector3(0, -gizmoBounds.extents.y - _editorSettings.LabelOffset,
                    gizmoBounds.extents.z);

            Vector3 position2;
            if (_editorSettings.PositionLabelAtCenter)
                position2 = gizmoBounds.center + new Vector3(0, 0, -gizmoBounds.extents.z);
            else
                position2 = gizmoBounds.center + new Vector3(0, gizmoBounds.extents.y + _editorSettings.LabelOffset,
                    -gizmoBounds.extents.z);

            if (_editorSettings.ShowSizeGizmoLabelOnBothSide &&
                currentBound.size.x >= _editorSettings.MinimumSizeForDoubleSidedLabel)
            {
                Handles.Label(position1, label, _handleLabelStyle);
                Handles.Label(position2, label, _handleLabelStyle);
            }
            else
            {
                if (!_editorSettings.PositionLabelAtCenter)
                {
                    Vector3 position3 = gizmoBounds.center + new Vector3(0,
                        gizmoBounds.extents.y + _editorSettings.LabelOffset,
                        gizmoBounds.extents.z);
                    Vector3 position4 = gizmoBounds.center + new Vector3(0,
                        -gizmoBounds.extents.y - _editorSettings.LabelOffset, -gizmoBounds.extents.z);

                    Handles.Label(
                        _editorSettings.PositionLabelAtCornerAxis
                            ? GetClosestToSceneCamera(new[] { position1, position4 }, transform)
                            : GetClosestToSceneCamera(new[] { position1, position2, position3, position4 }, transform),
                        label,
                        _handleLabelStyle);
                }
                else
                {
                    Handles.Label(GetClosestToSceneCamera(new[] { position1, position2 }, transform), label,
                        _handleLabelStyle);
                }
            }
        }

        static void DrawYLabel(Bounds currentBound, Bounds gizmoBounds, float unitSizeMultiplier, string unitName,
            int gizmoMaximumDecimalPoints, Transform transform)
        {
            _handleLabelStyle.normal.textColor = _editorSettings.SizeGizmoLabelColorY;
            _handleLabelStyle.normal.background = _labelBackgroundTextureY;

            float size = currentBound.size.y * unitSizeMultiplier;

            string labelString = "";
            if (_editorSettings.ShowAxisOnLabel)
                labelString += "Y: ";

            if (size is > 0 and < 0.000001f)
                labelString += " Almost 0";
            else
                labelString += (float)Math.Round(size, gizmoMaximumDecimalPoints);

            if (_editorSettings.ShowUnitOnLabel)
                labelString += " " + unitName;

            GUIContent label = new(labelString);

            Vector3 position1;
            if (_editorSettings.PositionLabelAtCenter)
                position1 = gizmoBounds.center + new Vector3(0, gizmoBounds.extents.y, 0);
            else
                position1 = gizmoBounds.center + new Vector3(gizmoBounds.extents.x + _editorSettings.LabelOffset, 0,
                    gizmoBounds.extents.z);

            Vector3 position2;
            if (_editorSettings.PositionLabelAtCenter)
                position2 = gizmoBounds.center + new Vector3(0, -gizmoBounds.extents.y, 0);
            else
                position2 = gizmoBounds.center + new Vector3(-gizmoBounds.extents.x - _editorSettings.LabelOffset, 0,
                    -gizmoBounds.extents.z);

            if (_editorSettings.ShowSizeGizmoLabelOnBothSide &&
                currentBound.size.y >= _editorSettings.MinimumSizeForDoubleSidedLabel)
            {
                Handles.Label(position1, label, _handleLabelStyle);
                Handles.Label(position2, label, _handleLabelStyle);
            }
            else
            {
                if (!_editorSettings.PositionLabelAtCenter)
                {
                    Vector3 position3 = gizmoBounds.center + new Vector3(
                        -gizmoBounds.extents.x - _editorSettings.LabelOffset,
                        0, gizmoBounds.extents.z);
                    Vector3 position4 = gizmoBounds.center + new Vector3(
                        gizmoBounds.extents.x + _editorSettings.LabelOffset, 0,
                        -gizmoBounds.extents.z);

                    Handles.Label(
                        _editorSettings.PositionLabelAtCornerAxis
                            ? GetLeftmostFromSceneCamera(new[] { position1, position2, position3, position4 },
                                transform)
                            : GetClosestToSceneCamera(new[] { position1, position2, position3, position4 }, transform),
                        label, _handleLabelStyle);
                }
                else
                {
                    Handles.Label(GetClosestToSceneCamera(new[] { position1, position2 }, transform), label,
                        _handleLabelStyle);
                }
            }
        }

        static void DrawZLabel(Bounds currentBound, Bounds gizmoBounds, float unitSizeMultiplier, string unit,
            int gizmoMaximumDecimalPoints, Transform transform)
        {
            _handleLabelStyle.normal.textColor = _editorSettings.SizeGizmoLabelColorZ;
            _handleLabelStyle.normal.background = _labelBackgroundTextureZ;

            float size = currentBound.size.z * unitSizeMultiplier;

            string zLabel = "";
            if (_editorSettings.ShowAxisOnLabel)
                zLabel += "Z: ";

            if (size is > 0 and < 0.000001f)
                zLabel += " Almost 0";
            else
                zLabel += (float)Math.Round(size, gizmoMaximumDecimalPoints);

            if (_editorSettings.ShowUnitOnLabel)
                zLabel += " " + unit;

            GUIContent label = new(zLabel);

            Vector3 position1;
            if (_editorSettings.PositionLabelAtCenter)
                position1 = gizmoBounds.center + new Vector3(gizmoBounds.extents.x, 0, 0);
            else
                position1 = gizmoBounds.center + new Vector3(gizmoBounds.extents.x,
                    -gizmoBounds.extents.y - _editorSettings.LabelOffset, 0);

            Vector3 position2;
            if (_editorSettings.PositionLabelAtCenter)
                position2 = gizmoBounds.center + new Vector3(-gizmoBounds.extents.x, 0, 0);
            else
                position2 = gizmoBounds.center + new Vector3(-gizmoBounds.extents.x,
                    gizmoBounds.extents.y + _editorSettings.LabelOffset, 0);

            if (_editorSettings.ShowSizeGizmoLabelOnBothSide &&
                currentBound.size.x >= _editorSettings.MinimumSizeForDoubleSidedLabel)
            {
                Handles.Label(position1, label, _handleLabelStyle);
                Handles.Label(position2, label, _handleLabelStyle);
            }
            else
            {
                if (!_editorSettings.PositionLabelAtCenter)
                {
                    Vector3 position3 = gizmoBounds.center + new Vector3(gizmoBounds.extents.x,
                        gizmoBounds.extents.y + _editorSettings.LabelOffset, 0);
                    Vector3 position4 = gizmoBounds.center + new Vector3(-gizmoBounds.extents.x,
                        -gizmoBounds.extents.y - _editorSettings.LabelOffset, 0);

                    Handles.Label(
                        _editorSettings.PositionLabelAtCornerAxis
                            ? GetClosestToSceneCamera(new[] { position1, position4 }, transform)
                            : GetClosestToSceneCamera(new[] { position1, position2, position3, position4 }, transform),
                        label,
                        _handleLabelStyle);
                }
                else
                {
                    Handles.Label(GetClosestToSceneCamera(new[] { position1, position2 }, transform), label,
                        _handleLabelStyle);
                }
            }
        }

        static Vector3 GetClosestToSceneCamera(Vector3[] positions, Transform transform)
        {
            Camera cam = SceneView.lastActiveSceneView?.camera;
            if (cam == null || positions == null || positions.Length == 0)
                return Vector3.zero;

            int index = 0;
            float maxZ = cam.worldToCameraMatrix.MultiplyPoint(transform.TransformPoint(positions[0])).z;

            for (int i = 1; i < positions.Length; i++)
            {
                float z = cam.worldToCameraMatrix.MultiplyPoint(transform.TransformPoint(positions[i])).z;
                if (!(z > maxZ)) continue; // higher z in camera space = visually closer
                maxZ = z;
                index = i;
            }

            return positions[index];
        }

        static Vector3 GetLeftmostFromSceneCamera(Vector3[] positions, Transform transform)
        {
            Camera cam = SceneView.lastActiveSceneView?.camera;
            if (cam == null || positions == null || positions.Length == 0)
                return Vector3.zero;

            int index = 0;
            float minX = cam.worldToCameraMatrix.MultiplyPoint(transform.TransformPoint(positions[0])).x;

            for (int i = 1; i < positions.Length; i++)
            {
                float x = cam.worldToCameraMatrix.MultiplyPoint(transform.TransformPoint(positions[i])).x;
                if (!(x < minX)) continue;
                minX = x;
                index = i;
            }

            return positions[index];
        }

        static Texture2D MakeFlatTexture(Color color)
        {
            Texture2D tex = new(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        static Vector3 Divide(Vector3 first, Vector3 second)
        {
            return new(NanFixed(first.x / second.x), NanFixed(first.y / second.y), NanFixed(first.z / second.z));
        }

        static float NanFixed(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 1;

            return value;
        }

        #endregion Size

        #region Outline

        static void DrawAxisColoredWireCube(Bounds bounds)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;

            // Calculate all 8 corners of the cube
            Vector3[] corners = new Vector3[8];
            corners[0] = c + new Vector3(-e.x, -e.y, -e.z);
            corners[1] = c + new Vector3(e.x, -e.y, -e.z);
            corners[2] = c + new Vector3(e.x, -e.y, e.z);
            corners[3] = c + new Vector3(-e.x, -e.y, e.z);
            corners[4] = c + new Vector3(-e.x, e.y, -e.z);
            corners[5] = c + new Vector3(e.x, e.y, -e.z);
            corners[6] = c + new Vector3(e.x, e.y, e.z);
            corners[7] = c + new Vector3(-e.x, e.y, e.z);

            // Draw bottom square (Y is same)
            DrawColoredLine(corners[0], corners[1], _editorSettings.SizeGizmoOutlineColorX); // X axis
            DrawColoredLine(corners[1], corners[2], _editorSettings.SizeGizmoOutlineColorZ); // Z axis
            DrawColoredLine(corners[2], corners[3], _editorSettings.SizeGizmoOutlineColorX); // X axis
            DrawColoredLine(corners[3], corners[0], _editorSettings.SizeGizmoOutlineColorZ); // Z axis

            // Draw top square (Y is same)
            DrawColoredLine(corners[4], corners[5], _editorSettings.SizeGizmoOutlineColorX); // X axis
            DrawColoredLine(corners[5], corners[6], _editorSettings.SizeGizmoOutlineColorZ); // Z axis
            DrawColoredLine(corners[6], corners[7], _editorSettings.SizeGizmoOutlineColorX); // X axis
            DrawColoredLine(corners[7], corners[4], _editorSettings.SizeGizmoOutlineColorZ); // Z axis

            // Draw vertical lines (Y axis)
            DrawColoredLine(corners[0], corners[4], _editorSettings.SizeGizmoOutlineColorY);
            DrawColoredLine(corners[1], corners[5], _editorSettings.SizeGizmoOutlineColorY);
            DrawColoredLine(corners[2], corners[6], _editorSettings.SizeGizmoOutlineColorY);
            DrawColoredLine(corners[3], corners[7], _editorSettings.SizeGizmoOutlineColorY);
        }

        static void DrawColoredLine(Vector3 start, Vector3 end, Color color)
        {
            Handles.color = color;
            Handles.DrawLine(start, end, _editorSettings.SizeGizmoOutlineThickness);
        }

        #endregion
    }
}