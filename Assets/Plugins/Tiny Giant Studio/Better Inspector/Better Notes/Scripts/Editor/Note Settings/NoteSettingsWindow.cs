using System.Collections.Generic;
using TinyGiantStudio.BetterEditor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TinyGiantStudio.BetterInspector
{
    public class NoteSettingsWindow : EditorWindow
    {
        [SerializeField] VisualTreeAsset visualTreeAsset;

        const string VisualTreeAssetFileLocation =
            "Assets/Plugins/Tiny Giant Studio/Better Inspector/Better Notes/Scripts/Editor/Note Settings/Note Settings.uxml";

        const string VisualTreeAssetGuid = "a323c507e14825a4ca386df5f84b2b0b";

        [SerializeField] VisualTreeAsset notesListItemVisualTreeAsset;

        const string NotesListItemVisualTreeAssetFileLocation =
            "Assets/Plugins/Tiny Giant Studio/Better Inspector/Better Notes/Scripts/Editor/Note Settings/Notes List Item.uxml";

        const string NotesListItemVisualTreeAssetGuid = "293dd9c6cac8b45449593f772cc326ec";

        SceneNotesManager _sceneNotesManager;
        NoteSettings _noteSettings;
        PrefabNotes _prefabNotes;

        VisualElement _root;
        GroupBox _prefabNotesGroupBox;
        Toggle _prefabNotesToggle;
        GroupBox _sceneNotesGroupBox;
        Toggle _sceneNotesToggle;

        void OnEnable()
        {
#if UNITY_2022_2_OR_NEWER
            Undo.undoRedoEvent -= OnUndoRedoEvent;
            Undo.undoRedoEvent += OnUndoRedoEvent;
#else
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
#endif
        }

        void OnDisable()
        {
#if UNITY_2022_2_OR_NEWER
            Undo.undoRedoEvent -= OnUndoRedoEvent;
#else
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
#endif
        }
#if UNITY_2022_2_OR_NEWER
        void OnUndoRedoEvent(in UndoRedoInfo undo)=> HandleUndoRedo();
#else
        void OnUndoRedoPerformed() => HandleUndoRedo();
#endif
        void HandleUndoRedo()
        {
            UpdatePrefabNotes();
            UpdateSceneNotes();
        }

        [MenuItem("Tools/Tiny Giant Studio/Notes Settings", false, 10000)]
        // ReSharper disable once Unity.IncorrectMethodSignature
        public static NoteSettingsWindow ShowEditor()
        {
            NoteSettingsWindow noteEditorWindow = GetWindow<NoteSettingsWindow>();
            noteEditorWindow.titleContent = new("Note Settings");
            noteEditorWindow.minSize = new(300, 300);
            return noteEditorWindow;
        }

        public void CreateGUI()
        {
            if (visualTreeAsset == null)
                visualTreeAsset = Utility.GetVisualTreeAsset(VisualTreeAssetFileLocation, VisualTreeAssetGuid);

            _root = visualTreeAsset.Instantiate();
            rootVisualElement.Add(_root);
            rootVisualElement.hierarchy[0].style.flexGrow = 1;

            _noteSettings = NoteSettings.instance;
            _prefabNotes = PrefabNotes.instance;
            _sceneNotesManager = SceneNotesManager.Instance;

            Toggle notesToggle = _root.Q<Toggle>("NotesEnabled");
            notesToggle.SetValueWithoutNotify(_noteSettings.showNotes);
            notesToggle.schedule.Execute(() =>
            {
                notesToggle.RegisterValueChangedCallback(ev =>
                {
                    _noteSettings.showNotes = ev.newValue;
                    _noteSettings.Save();
                    NotesBetterTransformIntegration notesBetterTransformIntegration =
                        CreateInstance<NotesBetterTransformIntegration>();
                    notesBetterTransformIntegration.UpdateSelection();
                    _root.Q<GroupBox>("AllNoteSettings").SetEnabled(_noteSettings.showNotes);

                    SceneView.RepaintAll();
                });
            }).ExecuteLater(100);
            _root.Q<GroupBox>("AllNoteSettings").SetEnabled(_noteSettings.showNotes);

            Toggle notesGizmoToggle = _root.Q<Toggle>("NotesGizmoEnabled");
            notesGizmoToggle.SetValueWithoutNotify(_noteSettings.showNotesGizmo);
            notesGizmoToggle.schedule.Execute(() =>
            {
                notesGizmoToggle.RegisterValueChangedCallback(ev =>
                {
                    _noteSettings.showNotesGizmo = ev.newValue;
                    _noteSettings.Save();
                    _root.Q<GroupBox>("AllNoteGizmoSettings").SetEnabled(_noteSettings.showNotesGizmo);

                    SceneView.RepaintAll();
                });
            }).ExecuteLater(200);
            _root.Q<GroupBox>("AllNoteGizmoSettings").SetEnabled(_noteSettings.showNotesGizmo);

            Toggle noGizmoDuringPlaymode = _root.Q<Toggle>("NoGizmoDuringPlaymode");
            noGizmoDuringPlaymode.SetValueWithoutNotify(!_noteSettings.showNotesGizmoDuringPlayMode);
            noGizmoDuringPlaymode.schedule.Execute(() =>
            {
                noGizmoDuringPlaymode.RegisterValueChangedCallback(ev =>
                {
                    _noteSettings.showNotesGizmoDuringPlayMode = !ev.newValue;
                    _noteSettings.Save();
                    SceneView.RepaintAll();
                });
            }).ExecuteLater(200);

            Vector2Field notesGizmoOffset = _root.Q<Vector2Field>("NotesGizmoOffset");
            notesGizmoOffset.SetValueWithoutNotify(_noteSettings.notesGizmoOffset);
            notesGizmoOffset.schedule.Execute(() =>
            {
                notesGizmoOffset.RegisterValueChangedCallback(ev =>
                {
                    _noteSettings.notesGizmoOffset = ev.newValue;
                    _noteSettings.Save();
                    SceneView.RepaintAll();
                });
            }).ExecuteLater(300);

            Button resetGizmoOffset = _root.Q<Button>("ResetGizmoOffset");
            resetGizmoOffset.clicked += () =>
            {
                _noteSettings.notesGizmoOffset = Vector2.zero;
                _noteSettings.Save();
                notesGizmoOffset.SetValueWithoutNotify(_noteSettings.notesGizmoOffset);
                SceneView.RepaintAll();
            };

            EnumField onClickAction = _root.Q<EnumField>("OnClickAction");
            onClickAction.value = _noteSettings.noteClickActions;
            onClickAction.RegisterValueChangedCallback(e =>
            {
                _noteSettings.noteClickActions = (NoteSettings.NoteClickActions)e.newValue;
                _noteSettings.Save();
                SceneView.RepaintAll();
            });


            _prefabNotesGroupBox = _root.Q<GroupBox>("PrefabNotes");
            CustomFoldout.SetupFoldout(_prefabNotesGroupBox);
            _prefabNotesToggle = _prefabNotesGroupBox.Q<Toggle>("FoldoutToggle");
            UpdatePrefabNotes();


            _sceneNotesGroupBox = _root.Q<GroupBox>("SceneNotes");
            CustomFoldout.SetupFoldout(_sceneNotesGroupBox);
            _sceneNotesToggle = _sceneNotesGroupBox.Q<Toggle>("FoldoutToggle");
            UpdateSceneNotes();


            //Button deleteAllNoteButton = inspectorSettingsFoldout.Q<Button>("DeleteAllNotesButton");
            //deleteAllNoteButton.clicked += () =>
            //{
            //    if (EditorUtility.DisplayDialog("Note permanent deletion", "This will permanently delete all notes. Are you sure?", "Yes", "No"))
            //    {
            //        transformEditorSettings.DeleteAllNotes();
            //        notesCountLabel.text = transformEditorSettings.NoteCount().ToString();
            //    }
            //};

            //Button cleanupNotesButton = inspectorSettingsFoldout.Q<Button>("CleanupNotesButton");
            //cleanupNotesButton.clicked += () =>
            //{
            //    int option = EditorUtility.DisplayDialogComplex("Clean-up Notes",
            //"This will attempt to remove all unused notes. This isn't always accurate, and you can't undo it. Are you sure you want to proceed?",
            //"Yes",
            //"Yes, but debug.log the removed notes",
            //"Cancel");

            //    switch (option)
            //    {
            //        // Yes.
            //        case 0:
            //            transformEditorSettings.CleanupNotes();
            //            break;

            //        // Yes with log.
            //        case 1:
            //            transformEditorSettings.CleanupNotes(true);
            //            break;

            //        // Cancel.
            //        case 2:
            //            Debug.Log("Note cleanup canceled.");
            //            break;

            //        default:
            //            Debug.LogError("Unrecognized option.");
            //            break;
            //    }
            //};
        }


        void UpdatePrefabNotes()
        {
            _prefabNotesToggle.text = "Prefab Notes : " + _prefabNotes.notes.Count;
            ScrollView content = _prefabNotesGroupBox.Q<ScrollView>();
            content.Clear();
            if (_prefabNotes.notes.Count == 0) return;

            for (int i = 0; i < _prefabNotes.notes.Count; i++)
            {
                TemplateContainer v = notesListItemVisualTreeAsset.Instantiate();
                content.Add(v);


                Button id = v.Q<Button>("ID");
                id.text = _prefabNotes.notes[i].id;
                int i1 = i;
                id.clicked += () =>
                {
                    string path = AssetDatabase.GUIDToAssetPath(_prefabNotes.notes[i1].id);
                    if (path == null) return;
                    Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                };

                id.schedule.Execute(() =>
                {
                    ObjectField field = v.Q<ObjectField>();
                    field.style.display = DisplayStyle.Flex;
                    field.SetEnabled(false);
                    string path = AssetDatabase.GUIDToAssetPath(_prefabNotes.notes[i1].id);
                    if (path == null) return;
                    Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    field.value = obj;
                }).ExecuteLater(200 * i1);

                v.Q<Label>("Note").text = _prefabNotes.notes[i].note;

                Button delete = v.Q<Button>("Delete");
                delete.clicked += () =>
                {
                    _prefabNotes.DeleteNote(_prefabNotes.notes[i1].id);
                    _prefabNotes.Save();
                    id.schedule.Execute(UpdatePrefabNotes).ExecuteLater(100);
                };
            }
        }

        void UpdateSceneNotes()
        {
            if (_sceneNotesManager == null) _sceneNotesManager = SceneNotesManager.Instance;

            if (_sceneNotesManager != null)
                _sceneNotesToggle.text = "Notes saved in this Scene : " + _sceneNotesManager.Notes.Count;
            else
                _sceneNotesToggle.text = "Couldn't load scene notes.";

            ScrollView content = _sceneNotesGroupBox.Q<ScrollView>();
            content.Clear();

            if (_sceneNotesManager == null) return;

            if (_sceneNotesManager.Notes == null) return;

            if (_sceneNotesManager.Notes.Count == 0) return;

            if (notesListItemVisualTreeAsset == null)
                notesListItemVisualTreeAsset = Utility.GetVisualTreeAsset(NotesListItemVisualTreeAssetFileLocation, NotesListItemVisualTreeAssetGuid);

            foreach (KeyValuePair<Transform, Note> note in _sceneNotesManager.Notes)
            {
                TemplateContainer v = notesListItemVisualTreeAsset.Instantiate();
                content.Add(v);
                Button id = v.Q<Button>("ID");
                id.style.display = DisplayStyle.None;

                ObjectField field = v.Q<ObjectField>();
                field.style.display = DisplayStyle.Flex;
                field.SetEnabled(false);
                field.value = note.Key;

                v.Q<Label>("Note").text = note.Value.note;

                Button delete = v.Q<Button>("Delete");
                delete.clicked += () =>
                {
                    _sceneNotesManager.DeleteNote(note.Key);
                    id.schedule.Execute(UpdateSceneNotes).ExecuteLater(100);
                };
            }
        }
    }
}