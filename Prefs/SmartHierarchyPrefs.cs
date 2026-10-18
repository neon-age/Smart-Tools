
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using System;

namespace Neonage.SmartTools
{
    internal class SmartHierarchyPrefs : SingletonPrefs<SmartHierarchyPrefs>
    {
        public override string PrefsKey => $"Neonage.SmartTools.{AssetName}";
        public override string AssetName => nameof(SmartHierarchyPrefs);

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
}
#endif