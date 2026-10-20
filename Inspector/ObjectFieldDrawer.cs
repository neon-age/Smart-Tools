
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Neonage.SmartTools
{
    // TODO: SupportedTypes attribute
    [CustomPropertyDrawer(typeof(UnityEngine.Object), useForChildren: false)]
    class ObjectFieldDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new PropertyField(property) { label = GetDisplayName(property) };
            field.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
            field.RegisterCallback<DragPerformEvent>(evt => OnDragPerform(evt, property), TrickleDown.TrickleDown);
            return field;
        }

        string GetDisplayName(SerializedProperty property)
        {
            var displayName = property.displayName;
            if (property.depth > 1 && displayName == "Value") // fix for Dictionary
                displayName = "";
            return displayName;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var currentEvent = Event.current;
            var isMouseOver = position.Contains(currentEvent.mousePosition);

            if (isMouseOver && DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is GameObject draggedGo)
            {
                if (currentEvent.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    currentEvent.Use();
                    CreateTypeSelectContextMenu(draggedGo, property).ShowAsContext();
                    return;
                }
            }
            EditorGUI.PropertyField(position, property, label);
        }

        void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is GameObject)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                evt.StopPropagation();
            }
        }
        void OnDragPerform(DragPerformEvent evt, SerializedProperty property)
        {
            if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is GameObject draggedGo)
            {
                DragAndDrop.AcceptDrag();
                evt.StopPropagation();
                CreateTypeSelectContextMenu(draggedGo, property).ShowAsContext();
            }
        }

        GenericMenu CreateTypeSelectContextMenu(GameObject go, SerializedProperty property)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent($"GameObject ({go.name})"), false, () => ApplyValue(property, go));

            foreach (Component component in go.GetComponents<Component>())
            {
                if (component == null) 
                    continue;
                menu.AddItem(new GUIContent($"{component.GetType().Name}"), false, () => ApplyValue(property, component));
            }
            return menu;
        }

        void ApplyValue(SerializedProperty property, Object value)
        {
            property.serializedObject.Update();
            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif