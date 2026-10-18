#if UNITY_EDITOR
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

        public void OnCreateUI(ScriptableObject prefs, string searchContext, VisualElement root)
        {
            try
            {
                var serializedObject = new SerializedObject(prefs);
                serializedObject.Update();

                var container = new VisualElement();
                root.Add(container);
                root.AddToClassList("unity-inspector-main-container"); // makes PropertyField respect the fixed labelWidth
                root.AddToClassList("unity-inspector-element");

                container.style.paddingLeft = 7;
                container.style.paddingTop = 2;

                var titleLabel = new Label(label);
                titleLabel.style.fontSize = 18;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.marginBottom = 10;
                container.Add(titleLabel);

                var iterator = serializedObject.GetIterator();

                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.name == "m_Script")
                        continue;
                    container.Add(new PropertyField(iterator));
                }
                root.Bind(serializedObject);

                root.TrackSerializedObjectValue(serializedObject, OnSerializedObjectChanged);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
#endif