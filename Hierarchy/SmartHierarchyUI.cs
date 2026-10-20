#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neonage.SmartTools
{
    [InitializeOnLoad]
    [NoAutoStaticsCleanup]
    internal static class SmartHierarchyUI
    {
        internal class HierarchyViewCustomData
        {
            public HierarchyWindow window;
            public VisualElement root;
            public Toolbar toolbar;
            public VisualElement columnsHeader;
            public R.HierarchyView.Wrapper hierarchyView;
            public R.CollectionView.Wrapper collectionView;
            public bool markForRebuild;

            public void RebuildIfNeeded()
            {
                if (markForRebuild)
                {
                    markForRebuild = false;
                    collectionView.RefreshItems();
                    window.Repaint();
                }
            }
        }
        internal static Dictionary<HierarchyWindow, HierarchyViewCustomData> RegisteredViews = new();
        static StyleSheet CustomStyleSheet;
        static List<Component> ComponentsTempList = new();

        static SmartHierarchyUI()
        {
            RegisteredViews.Clear();
            HierarchyWindow.BindViewItem += OnBindViewItem;
            CustomStyleSheet = Resources.Load<StyleSheet>("StyleSheets/SmartHierarchyUI");
        }

        static void OnBindViewItem(HierarchyWindow window, HierarchyView hierarchyView, HierarchyViewItem item)
        {
            var prefs = SmartToolsPrefs.Instance.hierarchy;

            if (!RegisteredViews.TryGetValue(window, out var viewData))
            {
                RegisteredViews.Add(window, viewData = new()
                {
                    window = window,
                    root = hierarchyView.parent,
                    toolbar = hierarchyView.parent.Q<Toolbar>(className: "hierarchy-toolbar"),
                    columnsHeader = hierarchyView.Q("unity-multi-column-view__header-container"),
                    hierarchyView = R.HierarchyView.Wrap(hierarchyView),
                    collectionView = R.CollectionView.Wrap(hierarchyView.Q(className: "unity-collection-view")),
                });
                
                if (CustomStyleSheet != null)
                    viewData.root.styleSheets.Add(CustomStyleSheet);

                viewData.root.schedule.Execute(viewData.RebuildIfNeeded).Every(30);

                AddSettingsButton(viewData.toolbar);
            }

            var notDisabledItemContainer = item.parent; // parent of HierarchyViewItem is not marked with .unity-disabled when object is disabled
            
            viewData.toolbar.EnableInClassList("compact-toolbar", prefs.useCompactToolbar);
            viewData.columnsHeader.EnableInClassList("hide-columns", prefs.hideColumnsHeader);

            foreach (var _c in viewData.hierarchyView.ListViewLayoutConfiguration.columns)
            {
                if (_c is not HierarchyViewColumn column || column.title == "Name")
                    continue;
                if (prefs.unlockColumnsMinWidth)
                    column.minWidth = 14;

                //var id = column.Descriptor.Id;
                //if (id == "Visibility" || id == "Picking")
                //    column.width = 14;
            }
            
            var itemHeight = prefs.itemHeight;
            if (viewData.collectionView.fixedItemHeight != itemHeight)
                viewData.collectionView.fixedItemHeight = itemHeight;

            SetHorizontalSpacing(item, prefs.horizontalSpacing);

            var setActiveToggle = notDisabledItemContainer.Q<SetActiveToggle>();
            var componentsBar = item.RightCustomContainer.Q<ComponentsBar>();

            if (setActiveToggle != null)
                setActiveToggle.style.display = DisplayStyle.None; 
            if (componentsBar != null)
                componentsBar.style.display = DisplayStyle.None;
            
            if (item.Handler is not HierarchyGameObjectHandler handler) 
                return;
            var go = handler.GetGameObject(item.Node);
            bool hasGO = go;
            if (!hasGO)
                return;
            
            if (prefs.componentsBarVisibility != SmartToolsPrefs.Hierarchy.ComponentsBarVisibility.Disabled)
            {
                componentsBar ??= new ComponentsBar();
                item.RightCustomContainer.Add(componentsBar);

                ComponentsTempList.Clear();
                go.GetComponents(ComponentsTempList);
                componentsBar.Bind(ComponentsTempList);
                componentsBar.style.display = DisplayStyle.Flex;
                componentsBar.EnableInClassList("components-bar-always-visible", prefs.componentsBarVisibility == SmartToolsPrefs.Hierarchy.ComponentsBarVisibility.AlwaysVisible);
            }

            if (prefs.inlineActiveToggle)
            {
                setActiveToggle ??= new SetActiveToggle() { style = { position = Position.Absolute, left = 0 }};
                notDisabledItemContainer.Add(setActiveToggle);

                setActiveToggle.Bind(go);
                setActiveToggle.style.display = DisplayStyle.Flex;
            }
        }

        static void SetHorizontalSpacing(HierarchyViewItem item, float spacing)
        {
            var leftContainer = item.LeftCustomContainer.parent;
            var leftTranslate = leftContainer.style.translate.value;
            if (leftTranslate.x.value > 15f)
            {
                leftTranslate.x = (int)(leftTranslate.x.value / 14f * spacing);
                leftContainer.style.translate = leftTranslate;
            }
        }

        static void AddSettingsButton(Toolbar toolbar)
        {
            var settingsButton = new ToolbarButton { iconImage = (Texture2D)EditorGUIUtility.IconContent("d_EditorSettings Icon").image };
            settingsButton.clicked += () => SettingsService.OpenUserPreferences("Preferences/Smart Tools");
            
            toolbar.Add(settingsButton);

            settingsButton.style.maxWidth = 28;
            var settingsButtonIcon = settingsButton.Q<Image>();
            settingsButtonIcon.style.maxHeight = 16;
            settingsButtonIcon.style.maxWidth = 16;
            settingsButtonIcon.style.marginBottom = 1;
        }
    }

    [NoAutoStaticsCleanup]
    internal static partial class R
    {
        static readonly object[] Args1 = new object[1];

        [NoAutoStaticsCleanup]
        public static class CollectionView
        {
            public static readonly Type Type = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.HierarchyV2.CollectionView");
            public static readonly PropertyInfo fixedItemHeight = Type.GetProperty("fixedItemHeight", BindingFlags.Public | BindingFlags.Instance);
            public static readonly MethodInfo RefreshItems = Type.GetMethod("RefreshItems", BindingFlags.Public | BindingFlags.Instance);

            public static Wrapper Wrap(VisualElement obj) => new() { obj = obj };
            public struct Wrapper
            {
                public VisualElement obj;
                public float fixedItemHeight 
                { 
                    get => (float)CollectionView.fixedItemHeight.GetValue(obj); 
                    set => CollectionView.fixedItemHeight.SetValue(obj, value); 
                }
                public void RefreshItems(bool forceBindItem = true) { Args1[0] = forceBindItem; CollectionView.RefreshItems.Invoke(obj, Args1); }
            }
        }
        [NoAutoStaticsCleanup]
        public static class HierarchyView
        {
            public static readonly Type Type = typeof(Unity.Hierarchy.HierarchyView);
            public static readonly PropertyInfo ListViewLayoutConfiguration = Type.GetProperty("ListViewLayoutConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);

            public static Wrapper Wrap(Unity.Hierarchy.HierarchyView obj) => new() { obj = obj };
            public struct Wrapper
            {
                public Unity.Hierarchy.HierarchyView obj;
                public MultiColumnLayoutConfiguration.Wrapper ListViewLayoutConfiguration 
                { 
                    get => MultiColumnLayoutConfiguration.Wrap(HierarchyView.ListViewLayoutConfiguration.GetValue(obj)); 
                }
            }
        }   
        [NoAutoStaticsCleanup]
        public static class MultiColumnLayoutConfiguration
        {
            public static readonly Type Type = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.HierarchyV2.MultiColumnLayoutConfiguration");
            public static readonly PropertyInfo columns = Type.GetProperty("columns", BindingFlags.Public | BindingFlags.Instance);

            public static Wrapper Wrap(object obj) => new() { obj = obj };
            public struct Wrapper
            {
                public object obj;
                public Columns columns 
                { 
                    get => MultiColumnLayoutConfiguration.columns.GetValue(obj) as Columns; 
                    set => MultiColumnLayoutConfiguration.columns.SetValue(obj, value); 
                }
            }
        }  
    }
}
#endif