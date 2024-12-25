using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using RevitDriveFromOutside.Utils;

namespace RevitDriveFromOutside.Events.Detach
{
    public static class DetachHelper
    {
        public static void DetachModel(this DetachConfig detachConfig, Application application, string filePath)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                (Document document, bool isWorkshared) = OpenDocument(application, filePath);
                if (document is null) return;

                ProcessDocument(document, detachConfig);
                string fileDetachedPath = GetDetachedFilePath(detachConfig, document, filePath);
                SaveDocument(document, fileDetachedPath, isWorkshared);
                Cleanup(document, fileDetachedPath, isWorkshared);
            }
            catch { }
        }
        private static (Document, bool) OpenDocument(Application application, string filePath)
        {
            Document document = null;
            bool isWorkshared = false;

            try
            {
                BasicFileInfo fileInfo = BasicFileInfo.Extract(filePath);
                if (!fileInfo.IsWorkshared)
                {
                    document = application.OpenDocumentFile(filePath);
                }
                else
                {
                    ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(filePath);
                    WorksetConfiguration worksetConfig = new(WorksetConfigurationOption.CloseAllWorksets);
                    document = modelPath.OpenDetached(application, worksetConfig);
                    isWorkshared = true;
                }
            }
            catch { }

            return (document, isWorkshared);
        }
        private static void ProcessDocument(Document document, DetachConfig detachConfig)
        {
            document.DeleteAllLinks();
#if R22_OR_GREATER
            if (detachConfig.RemoveEmptyWorksets && document.IsWorkshared)
                document.RemoveEmptyWorksets();
#endif
#if R24_OR_GREATER
            if (detachConfig.Purge)
                document.PurgeAll();
#endif
        }
        private static string GetDetachedFilePath(DetachConfig detachConfig, Document document, string originalFilePath)
        {
            string documentTitle = document.Title
                .Replace("_detached", "")
                .Replace("_отсоединено", "");

            string fileDetachedPath = Path.Combine(detachConfig.FolderPath, $"{documentTitle}.rvt");

            if (detachConfig.IsToRename)
                fileDetachedPath = RenamePath(fileDetachedPath,
                    RenameType.Title,
                    detachConfig.MaskInName, detachConfig.MaskOutName);

            if (detachConfig.CheckForEmptyView)
                CheckAndModifyForEmptyView(document, detachConfig, ref fileDetachedPath);

            return fileDetachedPath;
        }
        private static void CheckAndModifyForEmptyView(Document document, DetachConfig detachConfig, ref string fileDetachedPath)
        {
            document.OpenAllWorksets();
            try
            {
                string onlyTitle = Path.GetFileNameWithoutExtension(fileDetachedPath);
                string folder = Path.GetDirectoryName(fileDetachedPath);
                string extension = Path.GetExtension(fileDetachedPath);
                Element view = new FilteredElementCollector(document)
                    .OfClass(typeof(View3D))
                    .FirstOrDefault(e => e.Name == detachConfig.ViewName && !((View3D)e).IsTemplate);

                if (view is not null && document.IsViewEmpty(view))
                    fileDetachedPath = RenamePath(fileDetachedPath, RenameType.Empty);
            }
            catch { }
        }
        private static string RenamePath(string filePath, RenameType renameType, string maskIn = "", string maskOut = "")
        {
            string title = Path.GetFileNameWithoutExtension(filePath);
            string folder = Path.GetDirectoryName(filePath);
            string extension = Path.GetExtension(filePath);

            switch (renameType)
            {
                case RenameType.Folder:
                    folder = folder.Replace(maskIn, maskOut);
                    break;
                case RenameType.Title:
                    title = title.Replace(maskIn, maskOut);
                    break;
                case RenameType.Empty:
                    title = $"EMPTY_{title}";
                    break;
            }

            return Path.Combine(folder, $"{title}{extension}");
        }
        private enum RenameType
        {
            Folder,
            Title,
            Empty
        }
        private static void SaveDocument(Document document, string fileDetachedPath, bool isWorkshared)
        {
            SaveAsOptions saveOptions = new()
            {
                OverwriteExistingFile = true,
                MaximumBackups = 1
            };

            if (isWorkshared)
            {
                WorksharingSaveAsOptions worksharingOptions = new() { SaveAsCentral = true };
                saveOptions.SetWorksharingOptions(worksharingOptions);
            }

            try
            {
                ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(fileDetachedPath);
                document.SaveAs(modelPath, saveOptions);
            }
            catch { }
        }
        private static void Cleanup(Document document, string fileDetachedPath, bool isWorkshared)
        {
            try
            {
                document.FreeTheModel();
            }
            catch { }
            document?.Close();
            if (isWorkshared)
            {
                ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(fileDetachedPath);
                UpdateTransmissionData(modelPath);
                Directory.Delete(fileDetachedPath.Replace(".rvt", "_backup"), true);
            }
            else
            {
                File.Delete(fileDetachedPath.Replace(".rvt", ".0001.rvt"));
                File.Delete(fileDetachedPath.Replace(".rvt", ".0002.rvt"));
                File.Delete(fileDetachedPath.Replace(".rvt", ".0003.rvt"));
            }
        }
        private static void UpdateTransmissionData(ModelPath modelPath)
        {
            TransmissionData transmissionData = TransmissionData.ReadTransmissionData(modelPath);
            if (transmissionData is not null)
            {
                transmissionData.IsTransmitted = true;
                TransmissionData.WriteTransmissionData(modelPath, transmissionData);
            }
        }
    }
}
