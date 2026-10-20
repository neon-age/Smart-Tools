
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Neonage.SmartTools
{
    [NoAutoStaticsCleanup]
    static class SelectSamePrefabSiblings
    {
        static MethodInfo R_GetOriginalSourceOrVariantRoot = typeof(PrefabUtility).GetMethod("GetOriginalSourceOrVariantRoot", BindingFlags.Static | BindingFlags.NonPublic);
        static object[] R_Args1 = new object[1];

        class ParentData
        {
            public Transform parent;
            public List<GameObject> children = new();
        }
        
        [Shortcut("Hierarchy/Select Same Prefab Siblings")]
        // TODO: for some reason it triggers rename of first object???
        [MenuItem("GameObject/Selection/Select Same Prefab Siblings", priority = -1000)]
        static void OnMenu()
        {
            List<GameObject> newSelection = new();
            Dictionary<EntityId, ParentData> parents = new();

            foreach (var go in Selection.gameObjects)
            {
                var parent = go.transform.parent;
                var key = parent ? parent.GetEntityId() : default;
                if (!parents.TryGetValue(key, out var parentData))
                {
                    parents.Add(key, parentData = new());
                }
                parentData.parent = parent;
                parentData.children.Add(go);
            }
            foreach (var parentData in parents.Values)
            {
                FindInParent(parentData.parent, parentData.children);
            }

            void FindInParent(Transform matchParent, List<GameObject> childrenList)
            {
                HashSet<GameObject> prefabsToMatch = new();
                List<GameObject> allSiblings = new();

                foreach (var child in childrenList)
                {
                    R_Args1[0] = child;
                    var prefab = R_GetOriginalSourceOrVariantRoot.Invoke(null, R_Args1) as GameObject;
                    //var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    if (prefab)
                        prefabsToMatch.Add(prefab);
                }

                if (matchParent)
                {
                    var childCount = matchParent.childCount;
                    for (int i = 0; i < childCount; i++)
                    {
                        allSiblings.Add(matchParent.GetChild(i).gameObject);
                    }
                }
                else
                {
                    allSiblings.AddRange(Selection.activeGameObject.scene.GetRootGameObjects());
                }
                
                foreach (var sibling in allSiblings)
                {
                    R_Args1[0] = sibling;
                    var siblingPrefab = R_GetOriginalSourceOrVariantRoot.Invoke(null, R_Args1) as GameObject;
                    if (siblingPrefab && prefabsToMatch.Contains(siblingPrefab))
                    {
                        newSelection.Add(sibling);
                    }
                }
            }
            if (newSelection.Count > 0)
                Selection.objects = newSelection.ToArray();
        }
    }
}