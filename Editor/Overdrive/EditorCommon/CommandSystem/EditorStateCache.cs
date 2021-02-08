using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    // Inspired by UnityEditor.StateCache<T>, but this internal class was lacking the
    // ability to hold different state types.
    class EditorStateCache
    {
        string m_CacheFolder;
        Dictionary<Hash128, EditorStateComponent> m_Cache = new Dictionary<Hash128, EditorStateComponent>();

        public EditorStateCache(string cacheFolder)
        {
            if (string.IsNullOrEmpty(cacheFolder))
                throw new ArgumentException("cacheFolder cannot be null or empty string", cacheFolder);

            if (cacheFolder.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                throw new ArgumentException("Cache folder path has invalid path characters: '" + cacheFolder + "'");
            }

            cacheFolder = ConvertSeparatorsToUnity(cacheFolder);
            if (!cacheFolder.EndsWith("/"))
            {
                Debug.LogError("The cache folder path should end with a forward slash: '/'. Path: " + cacheFolder + ". Fixed up.");
                cacheFolder += "/";
            }
            if (cacheFolder.StartsWith("/"))
            {
                Debug.LogError("The cache folder path should not start with a forward slash: '/'. Path: " + cacheFolder + ". Fixed up."); // since on OSX a leading '/' means the root directory
                cacheFolder = cacheFolder.TrimStart('/');
            }

            m_CacheFolder = cacheFolder;
        }

        static string ConvertSeparatorsToUnity(string path)
        {
            return path.Replace('\\', '/');
        }

        public void Flush()
        {
            foreach (var pair in m_Cache)
            {
                var json = JsonUtility.ToJson(pair.Value);
                var filePath = GetFilePathForKey(pair.Key);
                try
                {
                    var directory = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(filePath, json, Encoding.UTF8); // Persist state
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error saving file {filePath}. Error: {e}");
                }
            }

            m_Cache.Clear();
        }

        public T GetState<T>(Hash128 key, T defaultValue = null) where T : EditorStateComponent
        {
            ThrowIfInvalid(key);

            if (m_Cache.TryGetValue(key, out var vsc))
                return vsc as T ?? defaultValue;

            var filePath = GetFilePathForKey(key);
            if (File.Exists(filePath))
            {
                string jsonString;
                try
                {
                    jsonString = File.ReadAllText(filePath, Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading file {filePath}. Error: {e}");
                    return defaultValue;
                }

                T obj;
                try
                {
                    obj = JsonUtility.FromJson<T>(jsonString);
                }
                catch (ArgumentException exception)
                {
                    Debug.LogError($"Invalid file content for {filePath}. Removing file. Error: {exception}");
                    RemoveState(key);
                    return defaultValue;
                }

                m_Cache[key] = obj;
                return obj;
            }

            m_Cache[key] = defaultValue;
            return defaultValue;
        }

        public void RemoveState(Hash128 key)
        {
            ThrowIfInvalid(key);

            m_Cache.Remove(key);

            string filePath = GetFilePathForKey(key);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        static void ThrowIfInvalid(Hash128 key)
        {
            if (!key.isValid)
                throw new ArgumentException("Hash128 key is invalid: " + key);
        }

        string GetFilePathForKey(Hash128 key)
        {
            // Hashed folder structure to ensure we scale with large amounts of state files.
            // See: https://medium.com/eonian-technologies/file-name-hashing-creating-a-hashed-directory-structure-eabb03aa4091
            string hexKey = key.ToString();
            string hexFolder = hexKey.Substring(0, 2) + "/";
            return m_CacheFolder + hexFolder + hexKey + ".json";
        }
    }
}
