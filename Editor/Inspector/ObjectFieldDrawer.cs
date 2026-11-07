
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using Object = UnityEngine.Object;

namespace Neonage.SmartTools
{
    // TODO: fix valid drag highlight color (it should appear blue)
    [CustomPropertyDrawer(typeof(Object), useForChildren: false)]
    public class ObjectFieldDrawer : PropertyDrawer
    {
        [NoAutoStaticsCleanup]
        static List<Component> ComponentsTempList = new();

        public override VisualElement CreatePropertyGUI(SerializedProperty property) => CreateField(property, GetLabel(property), fieldInfo);
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) => OnIMGUI(position, property, label, fieldInfo);

        public static PropertyField CreateField(SerializedProperty property, string label, FieldInfo fieldInfo)
        {   
            if (property.propertyPath.Contains("m_PersistentCalls.m_Calls.Array.")) // skip UnityEvents
            {
                return new PropertyField(property) { label = "" };
            }
            var propertyField = new PropertyField(property);

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                var supportedTypes = GetSupportedTypesAttribute(property, fieldInfo);

                propertyField.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
                {
                    var objectField = propertyField.Q<ObjectField>();
                    var objectFieldDisplay = objectField.Q(className: "unity-object-field-display");
                    var objectFieldDisplayLabel = objectFieldDisplay.Q<Label>();

                    // DON'T put into userData - that will break right-clicking on ObjectField and Array Foldout (yeah, BRUH...)
                    //objectField.userData = supportedTypes;

                    objectField.label = label;
                    objectField.RegisterCallback<DragUpdatedEvent>(evt => OnDragUpdated(evt, supportedTypes), CallbackOptions.TrickleDown);
                    objectField.RegisterCallback<DragPerformEvent>(evt => OnDragPerform(evt, property, supportedTypes), CallbackOptions.TrickleDown);

                    if (supportedTypes != null)
                    {
                        objectField.allowSceneObjects = supportedTypes.allowSceneObjects;

                        objectFieldDisplayLabel.RegisterValueChangedCallback(evt => UpdateLabel());
                        UpdateLabel();

                        void UpdateLabel()
                        {
                            var str = "";
                            var types = supportedTypes.types;
                            for (int i = 0; i < types.Length; i++)
                            {
                                var t = types[i];
                                str += i < types.Length - 1 ? $"{t.Name} or " : $"{t.Name}";
                            }

                            if (objectFieldDisplayLabel.text.StartsWith("None ("))
                            {
                                objectFieldDisplayLabel.text = $"None ({str})";
                            }
                            objectFieldDisplay.tooltip = str;
                        }
                    }

                    var parentProp = property.Copy();
                    parentProp.Parent();

                    if (parentProp.isArray) // override dragging into array header
                    {
                        var itemContainer = propertyField.parent.parent;
                        if (itemContainer.ClassListContains("unity-list-view__reorderable-item__container"))
                        {
                            if (itemContainer.parent.parent.parent is Foldout foldout && !foldout.ClassListContains("custom-object-field-registered"))
                            {
                                foldout.AddToClassList("custom-object-field-registered");
                                var foldoutToggle = foldout.Q<Toggle>(className: "unity-foldout__toggle--inspector");
                                foldoutToggle.userData = objectField.userData;
                                foldoutToggle.RegisterCallback<DragUpdatedEvent>(evt => OnDragUpdated(evt, supportedTypes), CallbackOptions.TrickleDown);
                                foldoutToggle.RegisterCallback<DragPerformEvent>(evt => OnDragPerform(evt, parentProp, supportedTypes), CallbackOptions.TrickleDown);
                            }
                        }
                    }
                });
            }
            return propertyField;
        }

        public static void OnIMGUI(Rect position, SerializedProperty property, GUIContent label, FieldInfo fieldInfo)
        {
            EditorGUI.BeginProperty(position, label, property);

            var currentEvent = Event.current;
            var isMouseOver = position.Contains(currentEvent.mousePosition);

            if (property.propertyPath.Contains("m_PersistentCalls.m_Calls.Array.")) // skip UnityEvents
            {
                EditorGUI.PropertyField(position, property, label);
                EditorGUI.EndProperty();
                return;
            }

            if (isMouseOver && DragAndDrop.objectReferences.Length > 0)
            {
                var dragObj = DragAndDrop.objectReferences[0];
                var supportedTypesData = new SupportedTypesData(dragObj, GetSupportedTypesAttribute(property, fieldInfo));

                if (currentEvent.type == EventType.DragUpdated)
                {
                    currentEvent.Use();
                    DragUpdated(dragObj, supportedTypesData);
                }
                else if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    currentEvent.Use();
                    DragPerform(dragObj, property, supportedTypesData);
                    return;
                }
            }
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndProperty();
        }

        static SupportedReferenceTypesAttribute GetSupportedTypesAttribute(SerializedProperty property, FieldInfo fieldInfo)
        {
            var attribute = fieldInfo.GetCustomAttribute<SupportedReferenceTypesAttribute>();
            if (attribute == null)
            {
                var parentProp = property.Copy();
                if (parentProp.Parent())
                {
                    attribute = SerializedPropertyUtil.GetAttribute<SupportedReferenceTypesAttribute>(parentProp);
                    //if (supportedTypes == null && parentProp.Parent())
                    //{
                    //    supportedTypes = SerializedPropertyUtil.GetAttribute<SupportedReferenceTypesAttribute>(parentProp);
                    //}
                }
            }
            return attribute;
        }

        public static string GetLabel(SerializedProperty property)
        {
            var displayName = property.displayName;
            if (property.depth > 1 && displayName == "Value") // fix for Dictionary
                displayName = "";
            return displayName;
        }

        struct SupportedTypesData
        {
            public int typesCount;
            public Type[] types;
            public bool allowSceneObjects;
            public bool alwaysReferenceRootObject;

            public SupportedTypesData(Object dragObj, SupportedReferenceTypesAttribute attribute)
            {
                this = default;
                typesCount = 1;
                if (attribute == null)
                    return;
                types = attribute.types;
                allowSceneObjects = attribute.allowSceneObjects;
                alwaysReferenceRootObject = attribute.alwaysReferenceRootObject;
                typesCount = GetSupportedTypesCount(dragObj);
            }

            public int GetSupportedTypesCount(Object obj)
            {
                var typesTotalCount = 0;
                if (obj is GameObject go)
                {
                    typesTotalCount += GetSupportedTypesCount(typeof(GameObject));

                    ComponentsTempList.Clear();
                    go.GetComponents(ComponentsTempList);
                    foreach (Component component in ComponentsTempList)
                        if (component)
                            typesTotalCount += GetSupportedTypesCount(component.GetType());
                }
                else
                    typesTotalCount += GetSupportedTypesCount(obj.GetType());
                return typesTotalCount;
            }

            public bool IsSupportedType(Type type)
            {
                if (types == null)
                    return true;
                return GetSupportedTypesCount(type) > 0;
            }
            public int GetSupportedTypesCount(Type type)
            {
                var supportedTypesCount = 0;
                foreach (var t in types)
                {
                    if (t.IsAssignableFrom(type))
                        supportedTypesCount++;
                }
                return supportedTypesCount;
            }
        }

        static void OnDragUpdated(DragUpdatedEvent evt, SupportedReferenceTypesAttribute supportedReferenceTypesAttribute)
        {
            var dragObj = DragAndDrop.objectReferences[0];

            evt.StopImmediatePropagation();
            DragUpdated(dragObj, new SupportedTypesData(dragObj, supportedReferenceTypesAttribute));
        }
        static void OnDragPerform(DragPerformEvent evt, SerializedProperty property, SupportedReferenceTypesAttribute supportedReferenceTypesAttribute)
        {
            var dragObj = DragAndDrop.objectReferences[0];

            DragAndDrop.AcceptDrag();
            evt.StopImmediatePropagation();

            var supportedTypes = new SupportedTypesData(dragObj, supportedReferenceTypesAttribute);

            if (supportedTypes.types == null || supportedTypes.typesCount > 0)
                DragPerform(dragObj, property, supportedTypes);
        }

        static void DragUpdated(Object obj, SupportedTypesData supportedTypes)
        {
            var allowDrag = supportedTypes.typesCount > 0;
            if (obj is GameObject go)
            {
                if (!supportedTypes.allowSceneObjects && go.scene != default)
                    allowDrag = false;
            }
            DragAndDrop.visualMode = allowDrag ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
        }
        static void DragPerform(Object obj, SerializedProperty property, SupportedTypesData supportedTypes)
        {
            List<(string path, Object obj)> items = new();
            Dictionary<string, int> duplicatePathsCounter = new();

            if (obj is GameObject go)
            {
                if (supportedTypes.IsSupportedType(typeof(GameObject)))
                    items.Add(new($"GameObject ({go.name})", go));

                ComponentsTempList.Clear();
                go.GetComponents(ComponentsTempList);
                for (int i = 0; i < ComponentsTempList.Count; i++)
                {
                    var component = ComponentsTempList[i];
                    if (component && supportedTypes.IsSupportedType(component.GetType()))
                    {
                        var path = $"{component.GetType().Name}";
                        if (!duplicatePathsCounter.ContainsKey(path))
                            duplicatePathsCounter.Add(path, 0);
                        var duplicateIdx = duplicatePathsCounter[path]++;
                        items.Add((duplicateIdx == 0 ? path : $"{path} ({duplicateIdx + 1})", component));
                    }
                }
            }
            else
            {
                if (supportedTypes.IsSupportedType(obj.GetType()))
                    items.Add(($"{obj.GetType().Name}", obj));
            }

            if (items.Count > 1)
            {
                var menu = new GenericMenu();
                foreach (var item in items)
                {
                    var itemObject = item.obj;
                    if (supportedTypes.alwaysReferenceRootObject && itemObject is Component component)
                        itemObject = component.gameObject;
                    menu.AddItem(new(item.path), false, () => ApplyValue(property, itemObject));
                }
                menu.ShowAsContext();
            }
            else
            {
                var itemObject = items[0].obj;
                if (supportedTypes.alwaysReferenceRootObject && itemObject is Component component)
                    itemObject = component.gameObject;
                ApplyValue(property, itemObject);
            }
        }

        static void ApplyValue(SerializedProperty property, Object value)
        {
            if (property.isArray)
            {
                var idx = property.arraySize;
                property.InsertArrayElementAtIndex(idx);
                property.GetArrayElementAtIndex(idx).objectReferenceValue = value;
            }
            else
            {
                property.objectReferenceValue = value;
            }
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
