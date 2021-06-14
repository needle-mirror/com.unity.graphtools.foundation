using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for the model used to display the blackboard.
    /// </summary>
    public interface IBlackboardGraphModel : IGraphElementModel
    {
        /// <summary>
        /// Whether the model is valid.
        /// </summary>
        bool Valid { get; }

        /// <summary>
        /// Gets the title of the blackboard.
        /// </summary>
        /// <returns>The title of the blackboard.</returns>
        string GetBlackboardTitle();

        /// <summary>
        /// Gets the sub-title of the blackboard.
        /// </summary>
        /// <returns>The sub-title of the blackboard.</returns>
        string GetBlackboardSubTitle();

        /// <summary>
        /// Gets the section names.
        /// </summary>
        /// <returns>The section names.</returns>
        IEnumerable<string> SectionNames { get; }

        /// <summary>
        /// Gets the <see cref="IVariableDeclarationModel"/> for the section <paramref name="sectionName"/>.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>The <see cref="IVariableDeclarationModel"/> for the section.</returns>
        IEnumerable<IVariableDeclarationModel> GetSectionRows(string sectionName);
    }
}
