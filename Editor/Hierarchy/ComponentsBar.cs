
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neonage.SmartTools
{
    internal class ComponentsBar : VisualElement
    {
        List<Button> elements = new();

        public ComponentsBar()
        {
            style.flexDirection = FlexDirection.Row;
        }

        static void OnClick(ClickEvent evt)
        {
            // TODO: custom popup window
            EditorUtility.OpenPropertyEditor((evt.target as VisualElement).userData as Component);
        }

        public void Bind(List<Component> components)
        {
            this.Clear();
            var elementsCount = elements.Count;
            var componentsCount = components.Count;

            if (elementsCount < componentsCount)
            {
                for (int i = elementsCount; i < componentsCount; i++)
                {
                    var element = new Button();
                    element.Q<TextElement>().RemoveFromHierarchy();
                    element.ClearClassList();
                    element.RegisterCallback<ClickEvent>(OnClick);
                    elements.Add(element);
                }
            }
            for (int i = componentsCount - 1; i >= 0; i--)
            {
                var component = components[i];
                var element = elements[i];
                if (!component)
                    continue;
                if (component is ParticleSystemRenderer)
                    continue;
                if (component.hideFlags == HideFlags.HideInInspector)
                    continue;
                element.style.width = 16;
                element.style.height = 16;
                element.userData = component;
                var componentContent = EditorGUIUtility.ObjectContent(component, component.GetType());
                element.iconImage = (Texture2D)componentContent.image;
                element.tooltip = componentContent.text;
                this.Add(element);
            }
        }
    }
}