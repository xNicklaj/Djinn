//This is not in the editor folder because there is a editor only mono-behavior that requires this
#if UNITY_EDITOR

using UnityEngine;

namespace TinyGiantStudio.BetterInspector
{
    [System.Serializable]
    public class Note
    {
        //Serializefield should be unnecessary here but weirdly doesn't work in some cases. Need to recheck later
        [SerializeField] public string id; //used by prefabs to identify who this belongs to
        [SerializeField] public string note;
        [SerializeField] public NoteType noteType;
        [SerializeField] public Color textColor;
        [SerializeField] public Color backgroundColor;
        [SerializeField] public bool showInSceneView;
        [SerializeField] public bool showInPrefabInstances = true;


        public Note(string note, NoteType noteType, Color textColor, Color backgroundColor, bool showInSceneView)
        {
            this.note = note;
            this.noteType = noteType;
            this.textColor = textColor;
            this.backgroundColor = backgroundColor;
            this.showInSceneView = showInSceneView;
        }

        public Note(string id, string note, NoteType noteType, Color textColor, Color backgroundColor, bool showInSceneView)
        {
            this.id = id;
            this.note = note;
            this.noteType = noteType;
            this.textColor = textColor;
            this.backgroundColor = backgroundColor;
            this.showInSceneView = showInSceneView;
        }
    }
}

#endif