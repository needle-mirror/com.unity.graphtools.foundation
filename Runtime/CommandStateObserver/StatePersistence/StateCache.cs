using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    // Inspired by UnityEditor.StateCache<T>, which (1) is internal and (2) is lacking the
    // ability to hold different state types.
    class StateCache
    {
        string m_CacheFolder;
        Dictionary<Hash128, IStateComponent> m_Cache = new Dictionary<Hash128, IStateComponent>();

        public StateCache(string cacheFolder)
        {
            if (string.IsNullOrEmpty(cacheFolder))
                throw new ArgumentException(nameof(cacheFolder) + " cannot be null or empty string", cacheFolder);

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
                if (pair.Value == null)
                    continue;

                var filePath = GetFilePathForKey(pair.Key);
                try
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (directory != null)
                    {
                        Directory.CreateDirectory(directory);
                        var serializedData = StateComponentHelper.Serialize(pair.Value);
                        File.WriteAllText(filePath, serializedData, Encoding.UTF8);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error saving file {filePath}. Error: {e}");
                }
            }

            m_Cache.Clear();
        }

        public TComponent GetState<TComponent>(Hash128 key, Func<TComponent> defaultValueCreator = null) where TComponent : class, IStateComponent
        {
            ThrowIfInvalid(key);

            if (m_Cache.TryGetValue(key, out var vsc))
                return vsc as TComponent;

            TComponent obj = null;
            var filePath = GetFilePathForKey(key);
            if (File.Exists(filePath))
            {
                string serializedData;
                try
                {
                    serializedData = File.ReadAllText(filePath, Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading file {filePath}. Error: {e}");
                    serializedData = null;
                }

                if (serializedData != null)
                {
                    try
                    {
                        obj = StateComponentHelper.Deserialize<TComponent>(serializedData);
                    }
                    catch (ArgumentException exception)
                    {
                        Debug.LogError($"Invalid file content for {filePath}. Removing file. Error: {exception}");

                        // Remove invalid content
                        RemoveState(key);
                        obj = null;
                    }
                }
            }

            if (obj == null)
                obj = defaultValueCreator?.Invoke();

            if (obj != null)
                m_Cache[key] = obj;

            return obj;
        }

        public void SetState(Hash128 key, IStateComponent state)
        {
            ThrowIfInvalid(key);
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            m_Cache[key] = state;
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

        internal string GetFilePathForKey(Hash128 key)
        {
            // Hashed folder structure to ensure we scale with large amounts of state files.
            // See: https://medium.com/eonian-technologies/file-name-hashing-creating-a-hashed-directory-structure-eabb03aa4091
            string hexKey = key.ToString();
            string hexFolder = hexKey.Substring(0, 2) + "/";
            return m_CacheFolder + hexFolder + hexKey + ".json";
        }
    }
}
