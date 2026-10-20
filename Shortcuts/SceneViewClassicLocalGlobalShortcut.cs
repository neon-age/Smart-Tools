#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Neonage.SmartTools
{
    [InitializeOnLoad]
    static class SceneViewClassicLocalGlobalShortcut
    {
        static SceneViewClassicLocalGlobalShortcut()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            if (!SmartToolsPrefs.Instance.sceneView.classicLocalGlobalShortcut)
                return;
            var evt = Event.current;
            var binding = ShortcutManager.instance.GetShortcutBinding("Tools/Toggle Pivot Orientation");
        
            foreach (var combo in binding.keyCombinationSequence)
            {
                if (evt.keyCode != combo.keyCode)
                    continue;
                var matchAction = (combo.modifiers & ShortcutModifiers.Action) != 0 == (evt.control || evt.command);
                var matchAlt    = (combo.modifiers & ShortcutModifiers.Alt)    != 0 == evt.alt;
                var matchShift  = (combo.modifiers & ShortcutModifiers.Shift)  != 0 == evt.shift;

                if (matchAction && matchAlt && matchShift && evt.type == EventType.KeyDown)
                {
                    evt.Use();

                    if (Tools.pivotRotation == PivotRotation.Local)
                        Tools.pivotRotation = PivotRotation.Global;
                    else
                        Tools.pivotRotation = PivotRotation.Local;
                }
            }
        }
    }
}
#endif