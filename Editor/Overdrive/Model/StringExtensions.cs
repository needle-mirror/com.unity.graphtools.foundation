namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// String extension methods.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Makes a displayable name for a variable.
        /// </summary>
        /// <remarks>This is merely a wrapper for <see cref="ObjectNames.NicifyVariableName"/>.</remarks>
        /// <param name="value">The variable name to nicify.</param>
        /// <returns>The nicified variable name.</returns>
        public static string Nicify(this string value)
        {
            return ObjectNames.NicifyVariableName(value);
        }
    }
}
