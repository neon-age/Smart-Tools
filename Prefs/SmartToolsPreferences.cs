#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Neonage.SmartTools
{
    internal partial class SmartHierarchyPrefsProvider : CustomSettingsProvider
    {
        SmartHierarchyPrefsProvider(string path, SettingsScope scope):base(path, scope){}

        [SettingsProvider]
        static SettingsProvider Create() => new SmartHierarchyPrefsProvider("Preferences/Smart Tools/Smart Hierarchy", SettingsScope.User)
        {
            label = "Smart Hierarchy",
            keywords = new HashSet<string>(new[] { "Smart", "Tools", "Hierarchy", "Inspector" })
        };

        public override void OnActivate(string searchContext, VisualElement root)
        {
            OnCreateUI(SmartHierarchyPrefs.Instance, searchContext, root);
        }

        public override void OnSerializedObjectChanged(SerializedObject so)
        {
            SmartHierarchyPrefs.Save();

            foreach (var (window, windowData) in SmartHierarchyUI.RegisteredViews)
            {
                windowData.markForRebuild = true;
                window.Repaint();
            }
        }
    }
}
#endif