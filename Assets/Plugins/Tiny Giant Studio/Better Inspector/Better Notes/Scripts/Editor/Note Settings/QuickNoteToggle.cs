using UnityEditor;

namespace TinyGiantStudio.BetterInspector
{
    public static class QuickNoteToggle
    {
        static bool Show
        {
            get
            {
                NoteSettings noteSettings = NoteSettings.instance;
                if (noteSettings == null) return false;
                return noteSettings.showNotes && noteSettings.showNotesGizmo;
            }
            set
            {
                NoteSettings noteSettings = NoteSettings.instance;
                if (noteSettings == null) return;
                if (!value)
                {
                    noteSettings.showNotesGizmo = false;
                }
                else
                {
                    noteSettings.showNotes = true;
                    noteSettings.showNotesGizmo = true;
                }

                noteSettings.Save();
            }
        }

        [MenuItem("Tools/Tiny Giant Studio/Toggle Notes Overlay %#y", priority = 0)]
        static void Toggle()
        {
            Show = !Show;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/Tiny Giant Studio/Toggle Notes Overlay %#y", true, 0)]
        static bool ToggleValidate()
        {
            Menu.SetChecked("Tools/Tiny Giant Studio/Toggle Notes Overlay %#y", Show);
            return true;
        }
    }
}