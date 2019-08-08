using System;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.Quantum.QsProjectSystem
{
    public static class QsIconsMonikers
    {
        private const int IconId = 0;

        /// <summary>
        /// Needs to match the QsIconsGuid defined in the imagemanifest. 
        /// </summary>
        private static readonly Guid ManifestGuid = 
            new Guid("99495504-4c00-4fa2-8132-dbdef447a705");

        public static ImageMoniker ProjectIconImageMoniker =>
            new ImageMoniker { Guid = ManifestGuid, Id = IconId };
    }
}
