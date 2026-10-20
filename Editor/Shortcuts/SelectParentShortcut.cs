
using System.Collections.Generic;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Neonage.SmartTools
{
    static class SelectParentShortcut
    {
        [Shortcut("Hierarchy/Select Parent", KeyCode.F1)]
        static void OnShortcut()
        {
            if (EditorWindow.focusedWindow is not HierarchyWindow)
                return;

            List<GameObject> newSelection = new();
            HashSet<Transform> parents = new();

            foreach (var go in Selection.transforms)
            {
                var parent = go.transform.parent;
                if (parent)
                    parents.Add(parent);
            }
            foreach (var p in parents)
                newSelection.Add(p.gameObject);

            if (newSelection.Count > 0)
                Selection.objects = newSelection.ToArray();
        }
    }
}