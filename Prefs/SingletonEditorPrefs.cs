#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Scripting.LifecycleManagement;
using System.IO;

namespace Neonage.SmartTools
{
    internal abstract class SingletonEditorPrefs : ScriptableObject
    {
        public abstract string PrefsKey { get; }
        public abstract string AssetName { get; }
        public virtual string AssetExtension => "asset";

        public string LibraryPath => Path.Combine(Application.dataPath.Remove(Application.dataPath.Length - 7, 7), "Library", $"{AssetName}.{AssetExtension}");
    }
    [NoAutoStaticsCleanup]
    internal abstract class SingletonPrefs<T> : SingletonEditorPrefs where T : SingletonEditorPrefs
    {
        public static T Instance => Load();
        static T _Instance;
    
        public static T Load()
        {
            if (_Instance)
                return _Instance;
            _Instance = ScriptableObject.CreateInstance<T>();

            string json;
            if (File.Exists(_Instance.LibraryPath))
                json = File.ReadAllText(_Instance.LibraryPath);
            else
                json = EditorPrefs.GetString(_Instance.PrefsKey);
            
            EditorJsonUtility.FromJsonOverwrite(json, Instance);
            return _Instance;
        }

        public static string Save()
        {
            if (!_Instance)
                return default;
            var json = EditorJsonUtility.ToJson(_Instance, true);
            File.WriteAllText(_Instance.LibraryPath, json);
            EditorPrefs.SetString(_Instance.PrefsKey, json);
            return json;
        }
    }
}
#endif