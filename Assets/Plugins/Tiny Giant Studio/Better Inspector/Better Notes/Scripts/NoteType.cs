//This is not in the editor folder because there is a editor only mono-behavior that requires this
#if UNITY_EDITOR

namespace TinyGiantStudio.BetterInspector
{
    [System.Serializable]
    public enum NoteType
    {
        Tooltip,
        Bottom,
        Top,
        Hidden
    }
}
#endif