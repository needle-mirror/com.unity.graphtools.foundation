using System;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// <see cref="IVariableDeclarationModel"/> extension methods.
    /// </summary>
    public static class VariableDeclarationModelExtensions
    {
        /// <summary>
        /// Indicates whether a <see cref="IVariableDeclarationModel"/> requires initialization.
        /// </summary>
        /// <param name="self">The variable declaration model to query.</param>
        /// <returns>True if the variable declaration model requires initialization, false otherwise.</returns>
        public static bool RequiresInitialization(this IVariableDeclarationModel self)
        {
            if (self == null)
                return false;

            Type dataType = TypeHandleHelpers.ResolveType(self.DataType);

            return dataType.IsValueType || dataType == typeof(string);
        }

        /// <summary>
        /// Indicates whether a <see cref="IVariableDeclarationModel"/> requires special inspector initialization.
        /// </summary>
        /// <param name="self">The variable declaration model to query.</param>
        /// <returns>True if the variable declaration model requires initialization, false otherwise.</returns>
        public static bool RequiresInspectorInitialization(this IVariableDeclarationModel self)
        {
            return self.RequiresInitialization();
        }
    }
}
