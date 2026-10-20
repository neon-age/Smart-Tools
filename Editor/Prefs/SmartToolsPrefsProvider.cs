
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;

namespace Neonage.SmartTools
{
    internal partial class SmartToolsPrefsProvider : CustomSettingsProvider
    {
        SmartToolsPrefsProvider(string path, SettingsScope scope):base(path, scope){}

        [SettingsProvider]
        static SettingsProvider Create() => new SmartToolsPrefsProvider("Preferences/Smart Tools", SettingsScope.User)
        {
            label = "Smart Tools",
            keywords = new HashSet<string>(new[] { "Smart", "Tools", "Hierarchy", "Inspector", "Scene View" })
        };

        public override void OnActivate(string searchContext, VisualElement windowRoot)
        {
            var root = OnCreateUI(SmartToolsPrefs.Instance, searchContext, windowRoot);

            root.styleSheets.Add(Resources.Load<StyleSheet>("StyleSheets/SmartToolsPrefsProvider"));
        }

        public override void OnSerializedObjectChanged(SerializedObject so)
        {
            SmartToolsPrefs.Save();

            foreach (var (window, windowData) in SmartHierarchyUI.RegisteredViews)
            {
                windowData.markForRebuild = true;
                window.Repaint();
            }
        }
    }
}