#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Neonage.SmartTools
{
    static class SelectAllSiblingsShortcut
    {
        [Shortcut("Hierarchy/Select All Siblings", KeyCode.A)]
        static void OnShortcut()
        {
            if (EditorWindow.focusedWindow is not HierarchyWindow)
                return;

            List<GameObject> newSelection = new();
            HashSet<Transform> parents = new();

            foreach (var go in Selection.gameObjects)
            {
                var parent = go.transform.parent;
                //if (parent)
                parents.Add(parent);
            }
            foreach (var parent in parents)
            {
                if (parent)
                {
                    var childCount = parent.childCount;
                    for (int i = 0; i < childCount; i++)
                    {
                        newSelection.Add(parent.GetChild(i).gameObject);
                    }
                }
                // we actually don't want to select all root objects accidentally... 
                //else 
                //{
                //    newSelection.AddRange(Selection.activeGameObject.scene.GetRootGameObjects());
                //}
            }
            if (newSelection.Count > 0)
                Selection.objects = newSelection.ToArray();
        }
    }
}
#endif