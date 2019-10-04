namespace Microsoft.Quantum.QsProjectSystem
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.ProjectSystem;
    using Microsoft.VisualStudio.ProjectSystem.VS;
    using Microsoft.VisualStudio.Shell.Interop;


    [Export]
    [AppliesTo(UniqueCapability)]
    [ProjectTypeRegistration(
        VsPackage.ProjectTypeGuid, "Q# Project", "Q# Project Files (*.qsproj);*.qsproj",
        VsPackage.ProjectExtension, Language, resourcePackageGuid: VsPackage.PackageGuid, PossibleProjectExtensions = VsPackage.ProjectExtension)]
    internal class QsUnconfiguredProject
    {
        internal const string Language = "Q♯";

        /// <summary>
        /// Constant that is only present for Q# projects. 
        /// May be used by extensions to do something specifically for this project type only.
        /// </summary>
        /// <remarks>
        /// This value needs to be kept in sync with the capability defined in .targets.
        /// </remarks>
        internal const string UniqueCapability = "QSharpProject";

        [ImportingConstructor]
        public QsUnconfiguredProject(UnconfiguredProject unconfiguredProject) =>
            this.ProjectHierarchies = new OrderPrecedenceImportCollection<IVsHierarchy>(projectCapabilityCheckProvider: unconfiguredProject);

        [Import]
        internal UnconfiguredProject UnconfiguredProject { get; private set; }

        [Import]
        internal IActiveConfiguredProjectSubscriptionService SubscriptionService { get; private set; }

        [Import]
        internal IProjectThreadingService ProjectThreadingService { get; private set; }

        [Import]
        internal ActiveConfiguredProject<ConfiguredProject> ActiveConfiguredProject { get; private set; }

        [Import]
        internal ActiveConfiguredProject<QsConfiguredProject> MyActiveConfiguredProject { get; private set; }

        [ImportMany(ExportContractNames.VsTypes.IVsProject, typeof(IVsProject))]
        internal OrderPrecedenceImportCollection<IVsHierarchy> ProjectHierarchies { get; private set; }
    }


    [Export]
    [AppliesTo(QsUnconfiguredProject.UniqueCapability)]
    internal class QsConfiguredProject
    {
        [Import]
        internal ConfiguredProject ConfiguredProject { get; private set; }

        [Import]
        internal ProjectProperties Properties { get; private set; }
    }
}
