
using UnityEngine;
using System;

namespace Neonage.SmartTools
{
    internal class SmartToolsPrefs : SingletonPrefs<SmartToolsPrefs>
    {
        public override string AssetName => "SmartToolsPrefs";
        public override string PrefsKey => $"Neonage.SmartTools";

        public Hierarchy hierarchy = new();
        public Inspector inspector = new();
        public SceneView sceneView = new();

        [Serializable]
        public class Hierarchy
        {
            public enum ComponentsBarVisibility
            {
                Disabled,
                WhenHoveringOrSelected,
                AlwaysVisible,
            }

            public bool useCompactToolbar = true;
            public bool hideColumnsHeader = false;
            public bool unlockColumnsMinWidth = true;
            [Space]
            public bool inlineActiveToggle = true;
            public ComponentsBarVisibility componentsBarVisibility = ComponentsBarVisibility.WhenHoveringOrSelected;
            [Space]
            [Range(15, 21)]
            public int itemHeight = 16;
            [Range(8, 14)]
            public int horizontalSpacing = 14;
        }

        [Serializable]
        public class Inspector
        {
            public bool hideScriptField = true;
        }

        [Serializable]
        public class SceneView
        {
            [Tooltip("Switch between Local and Global mode with one key press, ignoring Grid or any other mode.")]
            public bool classicLocalGlobalShortcut = true;
        }
    }
}