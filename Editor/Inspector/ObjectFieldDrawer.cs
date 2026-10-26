
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
    [CustomPropertyDrawer(typeof(Object), useForChildren: false)]
    class ObjectFieldDrawer : PropertyDrawer
    {
        [NoAutoStaticsCleanup]
        static List<Component> ComponentsTempList = new();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var propertyField = new PropertyField(property) { label = GetDisplayName(property) };
       
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                var supportedTypes = GetSupportedTypesAttribute(property);

                propertyField.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
                {
                    var objectField = propertyField.Q<ObjectField>();
                    var objectFieldDisplay = objectField.Q(className:"unity-object-field-display");
                    var objectFieldLabel = objectFieldDisplay.Q<Label>();

                    // DON'T put into userData - that will break right-clicking on ObjectField and Array Foldout (yeah, BRUH...)
                    //objectField.userData = supportedTypes; 
                
                    objectField.RegisterCallback<DragUpdatedEvent>(evt => OnDragUpdated(evt, supportedTypes), CallbackOptions.TrickleDown);
                    objectField.RegisterCallback<DragPerformEvent>(evt => OnDragPerform(evt, property, supportedTypes), CallbackOptions.TrickleDown);

                    if (supportedTypes != null)
                    {
                        objectFieldLabel.RegisterValueChangedCallback(evt => UpdateLabel());
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

                            if (objectFieldLabel.text.StartsWith("None ("))
                            {
                                objectFieldLabel.text = $"None ({str})";
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

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
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
                var supportedTypesData = GetSupportedTypesData(dragObj, GetSupportedTypesAttribute(property));

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

        SupportedReferenceTypesAttribute GetSupportedTypesAttribute(SerializedProperty property)
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

        string GetDisplayName(SerializedProperty property)
        {
            var displayName = property.displayName;
            if (property.depth > 1 && displayName == "Value") // fix for Dictionary
                displayName = "";
            return displayName;
        }

        struct SupportedTypesData
        {
            public Type[] types;
            public int typesCount;
            public bool alwaysReferenceRootObject;

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

        SupportedTypesData GetSupportedTypesData(Object dragObj, SupportedReferenceTypesAttribute attribute)
        {
            var attributeData = new SupportedTypesData() { typesCount = 1 };
            if (attribute != null)
            {
                attributeData.types = attribute.types;
                attributeData.typesCount = attributeData.GetSupportedTypesCount(dragObj);
                attributeData.alwaysReferenceRootObject = attribute.alwaysReferenceRootObject;
            }
            return attributeData;
        }

        void OnDragUpdated(DragUpdatedEvent evt, SupportedReferenceTypesAttribute supportedReferenceTypesAttribute)
        {
            var dragObj = DragAndDrop.objectReferences[0];
        
            evt.StopImmediatePropagation();
            DragUpdated(dragObj, GetSupportedTypesData(dragObj, supportedReferenceTypesAttribute));
        }
        void OnDragPerform(DragPerformEvent evt, SerializedProperty property, SupportedReferenceTypesAttribute supportedReferenceTypesAttribute)
        {
            var dragObj = DragAndDrop.objectReferences[0];

            DragAndDrop.AcceptDrag();
            evt.StopImmediatePropagation();

            var supportedTypes = GetSupportedTypesData(dragObj, supportedReferenceTypesAttribute);

            if (supportedTypes.types == null || supportedTypes.typesCount > 0)
                DragPerform(dragObj, property, supportedTypes);
        }

        void DragUpdated(Object obj, SupportedTypesData supportedTypes)
        {
            DragAndDrop.visualMode = supportedTypes.typesCount > 0 ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
        }
        void DragPerform(Object obj, SerializedProperty property, SupportedTypesData supportedTypes)
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
