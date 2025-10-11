// Ignore Spelling: Deserialize

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TinyGiantStudio.BetterInspector
{
    /// <summary>
    /// Notes are cleaned up on awake.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class SceneNotesManager : MonoBehaviour
    {
        public static SceneNotesManager Instance;

        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public Dictionary<Transform, Note> Notes = new();

        [SerializeField] List<SerializedNoteReference> serializedNoteReferences = new();

        /// <summary>
        /// This will be removed at a later date //todo //Added 0n 22nd June 2025
        /// </summary>
        [SerializeField] List<Transform> keys = new();

        /// <summary>
        /// This will be removed at a later date //todo //Added 0n 22nd June 2025
        /// </summary>
        [SerializeField] List<Note> values = new();

        void Awake()
        {
            Instance = this;

            gameObject.tag = "EditorOnly"; //Making sure

            UpdateNoteReferencesFromOldVersion();

            PrepareTheDictionary();
        }

        void OnEnable()
        {
            Instance = this;

            // const bool hideInEditor = true;
            // if (hideInEditor)
            gameObject.hideFlags = HideFlags.HideInHierarchy;
            // else
            //     gameObject.hideFlags = HideFlags.None;

            PrepareTheDictionary();
        }

        void OnDisable()
        {
            Instance = null;
        }

        void OnDestroy()
        {
            Instance = null;
        }

        public Note MyNote(Transform target)
        {
            Notes.TryGetValue(target, out Note note);
            return note;
        }


        public void SetNote(Transform target, Note note)
        {
            if (Notes.ContainsKey(target))
            {
                Notes[target].note = note.note;
                Notes[target].noteType = note.noteType;
                Notes[target].textColor = note.textColor;
                Notes[target].backgroundColor = note.backgroundColor;
                Notes[target].showInSceneView = note.showInSceneView;
                Notes[target].showInPrefabInstances = note.showInPrefabInstances;
            }
            else
            {
                Notes.Add(target,
                    new(note.note, note.noteType, note.textColor, note.backgroundColor, note.showInSceneView));
            }

            UpdatePersistentValues();
            EditorUtility.SetDirty(this);
        }

        public void SetNote(Transform target, string note)
        {
            if (Notes.ContainsKey(target))
            {
                if (string.IsNullOrEmpty(note))
                {
                    Notes.Remove(target);
                }
                else
                {
                    Notes[target].note = note;
                }
            }
            else
                Notes.Add(target, new(note, NoteType.Tooltip, Color.white, Color.gray, true));

            UpdatePersistentValues();
            EditorUtility.SetDirty(this);
        }

        public void SetNote(Transform target, Color textColor, Color backgroundColor)
        {
            if (Notes.ContainsKey(target))
            {
                Notes[target].textColor = textColor;
                Notes[target].backgroundColor = backgroundColor;
            }

            UpdatePersistentValues();
            EditorUtility.SetDirty(this);
        }

        public void SetNote(Transform target, bool showInScene)
        {
            if (Notes.TryGetValue(target, out Note note))
            {
                note.showInSceneView = showInScene;
            }

            UpdatePersistentValues();
            EditorUtility.SetDirty(this);
        }

        public void SetNote(Transform target, NoteType noteType)
        {
            if (Notes.TryGetValue(target, out Note note))
            {
                note.noteType = noteType;
            }
            else
                Notes.Add(target, new("", noteType, Color.white, Color.gray, true));

            UpdatePersistentValues();
            EditorUtility.SetDirty(this);
        }

        public void DeleteNote(Transform target)
        {
            Undo.RecordObject(this, "Delete Note on " + target.name);
            if (!Notes.Remove(target)) return;
            UpdatePersistentValues();
            EditorUtility.SetDirty(this);
        }

        void UpdatePersistentValues()
        {
            serializedNoteReferences.Clear();
            foreach (KeyValuePair<Transform, Note> pair in Notes)
            {
                serializedNoteReferences.Add(new(pair.Key, pair.Value));
            }
        }

        void PrepareTheDictionary()
        {
            Notes.Clear();

            List<SerializedNoteReference> toRemove = new();

            foreach (SerializedNoteReference t in serializedNoteReferences)
            {
                if (t.target == null) //The object got deleted
                {
                    toRemove.Add(t);
                    continue;
                }

                if (Notes.ContainsKey(t.target)) //Should never happen, but making sure
                {
                    toRemove.Add(t);
                    continue;
                }

                Notes.Add(t.target, t.note);
            }

            foreach (SerializedNoteReference t in toRemove)
            {
                serializedNoteReferences.Remove(t);
            }
        }

        /// <summary>
        /// This will be removed at a later date //todo //Added 0n 22nd June 2025
        /// </summary>
        void UpdateNoteReferencesFromOldVersion()
        {
            if (keys.Count != values.Count)
                return;

            for (int i = 0; i < values.Count; i++)
            {
                if (Notes.ContainsKey(keys[i])) continue;

                serializedNoteReferences.Add(new(keys[i], values[i]));
            }

            keys.Clear();
            values.Clear();
        }
    }

    [System.Serializable]
    public class SerializedNoteReference
    {
        //SerializeField should be unnecessary here but weirdly doesn't work in some cases. Need to recheck later
        [SerializeField] public Transform target;

        [SerializeField] public Note note;

        public SerializedNoteReference(Transform reference, Note note)
        {
            target = reference;
            this.note = note;
        }
    }
}

#endif