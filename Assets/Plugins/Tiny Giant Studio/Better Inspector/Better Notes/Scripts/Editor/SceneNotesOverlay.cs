using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGiantStudio.BetterInspector
{
    /// <summary>
    /// This draws all the notes gizmo in the scene and also handles adding support for Better Transform.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneNotesOverlay
    {
        static BetterInspectorEditorSettings _commonEditorSettings;
        static NoteSettings _noteSettings;
        static SceneNotesManager _notesManager;
        static GUIStyle _noteStyle;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        static Dictionary<Transform, NoteWithBound> _visibleNotes = new();
        static float _lastUpdatedOn;

        static SceneNotesOverlay()
        {
            EditorApplication.delayCall += () =>
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                SceneView.duringSceneGui += OnSceneGUI;

                if (_commonEditorSettings == null) _commonEditorSettings = BetterInspectorEditorSettings.instance;
                if (_commonEditorSettings == null) return;
                _commonEditorSettings.Register("Open Note Editor", () => NoteEditor.ShowEditor());

                NotesBetterTransformIntegration notesBetterTransformIntegration = ScriptableObject.CreateInstance<NotesBetterTransformIntegration>();
                _commonEditorSettings.BetterTransformSelectionUpdate += notesBetterTransformIntegration.UpdateSelection;
            };
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            if (!ShouldDraw(sceneView))
                return;

            UpdateCache();
            foreach (KeyValuePair<Transform, NoteWithBound> note in _visibleNotes.Where(note => note.Key != null))
            {
                DrawNote(note.Key, note.Value);
            }
        }

        static void UpdateCache()
        {
            if (_lastUpdatedOn > Time.realtimeSinceStartup)
                return;

            //Debug.Log("Updated cache");

            _lastUpdatedOn = Time.realtimeSinceStartup + Random.Range(0.25f, 2f);

            _visibleNotes.Clear();
            foreach (KeyValuePair<Transform, Note> note in _notesManager.Notes)
            {
                if (!note.Key) continue;
                if (!note.Key.gameObject.activeInHierarchy) continue;
                if (note.Value == null) continue;
                if (!note.Value.showInSceneView) continue;
                if (string.IsNullOrEmpty(note.Value.note)) continue;

                Renderer renderer = note.Key.GetComponent<Renderer>();
                Bounds bounds = renderer != null
                    ? renderer.bounds
                    : new(note.Key.transform.position, Vector3.zero);

                if (!IsVisible(bounds)) continue;

                _visibleNotes.Add(note.Key, new(note.Value, bounds));
            }
        }

        static bool ShouldDraw(SceneView sceneView)
        {
            if (sceneView == null) return false;

            if (_noteSettings == null) _noteSettings = NoteSettings.instance;
            if (_noteSettings == null) return false;
            if (!_noteSettings.showNotes) return false;
            if (!_noteSettings.showNotesGizmo) return false;
            if (Application.isPlaying && !_noteSettings.showNotesGizmoDuringPlayMode) return false;

            if (_notesManager == null) _notesManager = SceneNotesManager.Instance;
            return _notesManager != null;
        }

        static void DrawNote(Transform target, NoteWithBound note)
        {
            if (target == null) return;

            if (!target.gameObject.activeInHierarchy) return;

            if (note == null) return;

            UpdateStyle();

            Vector3 worldPos = target.position + new Vector3(0, note.bounds.extents.y, 0);
            Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos) + _noteSettings.notesGizmoOffset;

            DrawNoteWithBackground(guiPos, note.note.note, note.note.textColor, note.note.backgroundColor);
        }

        //private static void DrawNote(Transform target, Note note)
        //{
        //    if (!note.showInSceneView) return;

        //    if (target == null) return;

        //    if (!target.gameObject.activeInHierarchy) return;

        //    if (note == null) return;

        //    if (string.IsNullOrEmpty(note.note)) return;

        //    var renderer = target.GetComponent<Renderer>();
        //    Bounds bounds = renderer != null
        //         ? renderer.bounds
        //         : new Bounds(target.transform.position, Vector3.zero);

        //    if (!IsVisible(target, bounds)) return;

        //    UpdateStyle();

        //    Vector3 worldPos = renderer ? target.position + new Vector3(0, bounds.extents.y, 0) : target.position;
        //    Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos) + noteSettings.notesGizmoOffset;

        //    DrawNoteWithBackground(guiPos, note.note, note.textColor, note.backgroundColor);
        //}

        static void UpdateStyle()
        {
            if (_noteStyle != null) return;

            //noteStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            _noteStyle = new(EditorStyles.largeLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = Color.black
                },
                fontSize = 12
            };
        }

        static void DrawNoteWithBackground(Vector2 screenPosition, string text, Color textColor,
            Color backgroundColor, Vector2 padding = default)
        {
            if (_noteStyle == null) return;
            if (padding == default) padding = new(2, 2);

            Handles.BeginGUI();

            GUIContent content = new(text);
            Vector2 textSize = _noteStyle.CalcSize(content);
            Vector2 totalSize = textSize + padding * 2;

            //Rect bgRect = new Rect(screenPosition, textSize + padding * 2);
            Rect bgRect = new(screenPosition.x - totalSize.x / 2f, screenPosition.y - totalSize.y / 2f, totalSize.x,
                totalSize.y);

            EditorGUI.DrawRect(bgRect, backgroundColor);
            _noteStyle.normal.textColor = textColor;
            GUI.Label(
                new(bgRect.x + padding.x, bgRect.y + padding.y, textSize.x, textSize.y),
                content,
                _noteStyle);

            Handles.EndGUI();
        }

        static bool IsVisible(Bounds bounds)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
                return false;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(sceneView.camera);
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }

        [System.Serializable] //Is this necessary?
        class NoteWithBound
        {
            public Note note;
            public Bounds bounds;

            public NoteWithBound(Note note, Bounds bounds)
            {
                this.note = note;
                this.bounds = bounds;
            }
        }
    }
}