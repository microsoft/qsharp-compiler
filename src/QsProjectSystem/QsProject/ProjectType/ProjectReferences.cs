using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.References;


namespace Microsoft.Quantum.QsProjectSystem
{
    [Export(typeof(IValidProjectReferenceChecker))]
    [AppliesTo(QsUnconfiguredProject.UniqueCapability)]
    class QsProjectReferenceChecker : IValidProjectReferenceChecker
    {
        public Task<SupportedCheckResult> CanAddProjectReferenceAsync(object referencedProject)
        {
            throw new NotImplementedException();
        }

        public Task<CanAddProjectReferencesResult> CanAddProjectReferencesAsync(IImmutableSet<object> referencedProjects)
        {
            throw new NotImplementedException();
        }

        public Task<SupportedCheckResult> CanBeReferencedAsync(object referencingProject)
        {
            throw new NotImplementedException();
        }
    }
}
