
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Neonage.SmartTools
{
    internal abstract class CustomSettingsProvider : SettingsProvider
    {
        internal CustomSettingsProvider(string path, SettingsScope scope) : base(path, scope) {}

        public abstract void OnSerializedObjectChanged(SerializedObject so);

        public VisualElement CreateDefaultRootWithTitle(SerializedObject serializedObject)
        {
            var root = new VisualElement();
            root.AddToClassList("unity-inspector-main-container"); // makes PropertyField respect the fixed labelWidth
            root.AddToClassList("unity-inspector-element");

            root.style.paddingLeft = 7;
            root.style.paddingTop = 2;

            var title = new Label(label);
            title.style.fontSize = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 10;
            root.Add(title);

            root.Bind(serializedObject);
            root.TrackSerializedObjectValue(serializedObject, OnSerializedObjectChanged);
            return root;
        }

        public VisualElement OnCreateUI(ScriptableObject prefs, string searchContext, VisualElement windowRoot)
        {
            try
            {
                var serializedObject = new SerializedObject(prefs);
                serializedObject.Update();

                var root = CreateDefaultRootWithTitle(serializedObject);
                windowRoot.Add(root);

                var iterator = serializedObject.GetIterator();

                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.name == "m_Script")
                        continue;
                    iterator.isExpanded = true;
                    root.Add(new PropertyField(iterator));
                }
                return root;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            return null;
        }
    }
}