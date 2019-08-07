using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;


namespace QsProjectSystem
{

    [ProvideProjectFactory(typeof(QsProjectSystemPackage), "Q# Project",
        "Q# Project Files (*.qsproj);*.qsproj", "qsproj", "qsproj",
        @"Templates\Projects\QSharpProject", LanguageVsTemplate = "QSharpProject")]

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(QsProjectSystemPackage.PackageGuidString)]
    public sealed class QsProjectSystemPackage : AsyncPackage
    {
        /// <summary>
        /// QsProjectSystemPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "3d28495d-e5df-4f6d-b974-0f4349f34a39";
        public const string QsProjectFactoryGuidString = "AE300E5E-0476-4A1C-89BC-2721B8251387";

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            this.RegisterProjectFactory(new QsProjectFactory(this));
        }
    }

    [Guid(QsProjectSystemPackage.QsProjectFactoryGuidString)]
    public sealed class QsProjectFactory : IVsProjectFactory
    {
        private QsProjectSystemPackage Package;
        public QsProjectFactory(QsProjectSystemPackage package) =>
            this.Package = package;


        public int CanCreateProject(string pszFilename, uint grfCreateFlags, out int pfCanCreate) =>
            throw new NotImplementedException();

        public int CreateProject(string pszFilename, string pszLocation, string pszName, uint grfCreateFlags, ref Guid iidProject, out IntPtr ppvProject, out int pfCanceled) =>
            throw new NotImplementedException();

        public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp) =>
            throw new NotImplementedException();

        public int Close() =>
            throw new NotImplementedException();
    }
}
