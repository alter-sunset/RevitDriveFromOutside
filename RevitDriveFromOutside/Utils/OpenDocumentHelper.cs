using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;

namespace RevitDriveFromOutside.Utils
{
    public static class OpenDocumentHelper
    {
        public static Document OpenDetached(this ModelPath modelPath,
            Application application,
            WorksetConfiguration worksetConfiguration)
        {
            OpenOptions openOptions = new()
            {
                DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets
            };
            return modelPath.OpenDocument(openOptions, worksetConfiguration, application);
        }
        private static Document OpenDocument(this ModelPath modelPath,
            OpenOptions openOptions,
            WorksetConfiguration worksetConfiguration,
            Application application)
        {
            openOptions.SetOpenWorksetsConfiguration(worksetConfiguration);
            Document openedDoc = application.OpenDocumentFile(modelPath, openOptions);
            return openedDoc;
        }
    }
}