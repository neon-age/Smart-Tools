
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neonage.SmartTools
{
    internal class SetActiveToggle : Toggle
    {
        public void Bind(GameObject go)
        {
            var activeSelf = go.activeSelf;
            this.userData = go;
            this.UnregisterValueChangedCallback(OnSetActiveToggle);
            this.RegisterValueChangedCallback(OnSetActiveToggle);
            this.SetValueWithoutNotify(activeSelf);
            this.EnableInClassList("toggle-object-disabled", !activeSelf);
        }

        static void OnSetActiveToggle(ChangeEvent<bool> evt)
        {
            if (evt.target is Toggle toggle && toggle.userData is GameObject go)
            {
                var undoName = (evt.newValue ? "Activate" : "Deactivate") + " GameObjects";

                if (Selection.Contains(go))
                {
                    Undo.RecordObjects(Selection.gameObjects, undoName);

                    foreach (var selection in Selection.gameObjects)
                    {
                        if (selection.activeSelf == evt.previousValue)
                            selection.SetActive(evt.newValue);
                    }
                }
                else
                {
                    Undo.RecordObject(go, undoName);
                    go.SetActive(evt.newValue);
                }
            }
        }
    }
}
#endif