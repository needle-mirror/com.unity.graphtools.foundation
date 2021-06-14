using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    public struct SearcherSize
    {
        public static readonly SearcherSize defaultSearcherSize = new SearcherSize { Size = new Vector2(500, 400), RightLeftRatio = 1.0f };

        public Vector2 Size;
        public float RightLeftRatio;
    }

    public static class GraphToolStateExtensionsForSearcherSize
    {
        static SerializedValueDictionary<string, SearcherSize> GetSizes(this GraphToolState state)
        {
            SerializedValueDictionary<string, SearcherSize> sizes = null;
            var valueString = state.Preferences.GetString(StringPref.SearcherSize);
            if (valueString != null)
            {
                sizes = JsonUtility.FromJson<SerializedValueDictionary<string, SearcherSize>>(valueString);
            }

            sizes ??= new SerializedValueDictionary<string, SearcherSize>();
            return sizes;
        }

        static void SaveSizes(this GraphToolState state, SerializedValueDictionary<string, SearcherSize> sizes)
        {
            if (sizes != null)
            {
                var valueString = JsonUtility.ToJson(sizes);
                state.Preferences.SetString(StringPref.SearcherSize, valueString);
            }
        }

        /// <summary>
        /// Gets the searcher window rect and left right ratio for <see cref="sizeName"/>.
        /// </summary>
        /// <param name="state">The state that contains size information.</param>
        /// <param name="sizeName">A string for the usage of the searcher.</param>
        public static SearcherSize GetSearcherSize(this GraphToolState state, string sizeName)
        {
            var sizes = state.GetSizes();
            if (string.IsNullOrEmpty(sizeName) || !sizes.TryGetValue(sizeName, out var size))
            {
                if (!sizes.TryGetValue("", out size))
                {
                    size = SearcherSize.defaultSearcherSize;
                }
            }
            return size;
        }

        /// <summary>
        /// Sets default searcher window size and left-right ratio for <see cref="sizeName"/>.
        /// </summary>
        /// <param name="state">The state that contains size information.</param>
        /// <param name="sizeName">A string for the usage of the searcher. Passing null for the usage will define the default for any searcher window.</param>
        /// <param name="size">The size of the window.</param>
        /// <param name="rightLeftRatio">The ratio between the left size and the right size (details) of the searcher.</param>
        public static void SetSearcherSize(this GraphToolState state, string sizeName, Vector2 size, float rightLeftRatio = 1.0f)
        {
            sizeName ??= "";

            var sizes = state.GetSizes();
            if (sizes.TryGetValue(sizeName, out var currentSize))
            {
                if (currentSize.Size == size && currentSize.RightLeftRatio == rightLeftRatio)
                {
                    return;
                }
            }

            sizes[sizeName] = new SearcherSize { Size = size, RightLeftRatio = rightLeftRatio }; ;
            state.SaveSizes(sizes);
        }

        /// <summary>
        /// Sets searcher window size and left-right ratio for <see cref="sizeName"/>, if it is not already set.
        /// </summary>
        /// <param name="state">The state that contains size information.</param>
        /// <param name="sizeName">A string for the usage of the searcher. Passing null for the usage will define the default for any searcher window.</param>
        /// <param name="size">The size of the window.</param>
        /// <param name="rightLeftRatio">The ratio between the left size and the right size (details) of the searcher.</param>
        public static void SetInitialSearcherSize(this GraphToolState state, string sizeName, Vector2 size, float rightLeftRatio = 1.0f)
        {
            sizeName ??= "";

            var sizes = state.GetSizes();
            if (!sizes.TryGetValue(sizeName, out _))
            {
                sizes[sizeName] = new SearcherSize { Size = size, RightLeftRatio = rightLeftRatio };
                state.SaveSizes(sizes);
            }
        }

        internal static void ResetSearcherSizes(this GraphToolState state)
        {
            var sizes = state.GetSizes();
            sizes.Clear();
            state.SaveSizes(sizes);
        }
    }
}
