using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;


namespace Microsoft.Quantum.QsProjectSystem
{
    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(QsUnconfiguredProject.UniqueCapability)]
    internal class ProjectTreePropertiesProvider : IProjectTreePropertiesProvider
    {
        /// <summary>
        /// Updates the property values for each node in the project tree, 
        /// given the context and values calculated by lower priority tree properties providers (if any exist).
        /// </summary>
        public void CalculatePropertyValues(
            IProjectTreeCustomizablePropertyContext propertyContext,
            IProjectTreeCustomizablePropertyValues propertyValues)
        {
            // Only set the icon for the root project node. 
            if (propertyValues.Flags.Contains(ProjectTreeFlags.Common.ProjectRoot))
            { propertyValues.Icon = QsIconsMonikers.ProjectIconImageMoniker.ToProjectSystemType(); }
        }
    }
}