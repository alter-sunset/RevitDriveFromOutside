using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitDriveFromOutside.Utils;

namespace RevitDriveFromOutside.Events.Detach
{
    public class EventHandlerDetach : RevitEventWrapper<DetachConfig>
    {
        public override void Execute(UIApplication uiApp, DetachConfig detachConfig)
        {
            using Application application = uiApp.Application;

            foreach (string filePath in detachConfig.Files)
            {
                using ErrorSwallower errorSwallower = new(uiApp, application);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                Document document;
                BasicFileInfo fileInfo;
                bool isWorkshared;

                try
                {
                    fileInfo = BasicFileInfo.Extract(filePath);
                    if (!fileInfo.IsWorkshared)
                    {
                        document = application.OpenDocumentFile(filePath);
                        isWorkshared = false;
                    }
                    else
                    {
                        ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(filePath);
                        WorksetConfiguration worksetConfiguration = new(WorksetConfigurationOption.CloseAllWorksets);
                        document = modelPath.OpenDetached(application, worksetConfiguration);
                        isWorkshared = true;
                    }
                }
                catch
                {
                    continue;
                }
                document.DeleteAllLinks();

                if (detachConfig.RemoveEmptyWorksets && isWorkshared)
                    document.RemoveEmptyWorksets();

                if (detachConfig.Purge)
                    document.PurgeAll();

                string documentTitle = document.Title.Replace("_detached", "").Replace("_отсоединено", "");
                if (detachConfig.IsToRename)
                    documentTitle = documentTitle.Replace(detachConfig.MaskInName, detachConfig.MaskOutName);

                string fileDetachedPath = "";
                string folder = detachConfig.FolderPath;
                string titleWithExtension = documentTitle + ".rvt";
                fileDetachedPath = Path.Combine(folder, titleWithExtension);

                if (detachConfig.CheckForEmptyView)
                {
                    document.OpenAllWorksets();
                    using FilteredElementCollector stuff = new(document);
                    try
                    {
                        string titleEmpty = Path.GetFileNameWithoutExtension(fileDetachedPath);

                        Element view = stuff.OfClass(typeof(View3D))
                            .FirstOrDefault(e => e.Name == detachConfig.ViewName && !((View3D)e).IsTemplate);

                        if (view is not null
                            && document.IsViewEmpty(view))
                            fileDetachedPath = fileDetachedPath.Replace(titleEmpty, $"EMPTY_{titleEmpty}");
                    }
                    catch { }
                }

                SaveAsOptions saveAsOptions = new()
                {
                    OverwriteExistingFile = true,
                    MaximumBackups = 1
                };
                WorksharingSaveAsOptions worksharingSaveAsOptions = new()
                {
                    SaveAsCentral = true
                };
                if (isWorkshared)
                    saveAsOptions.SetWorksharingOptions(worksharingSaveAsOptions);

                ModelPath modelDetachedPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(fileDetachedPath);
                document?.SaveAs(modelDetachedPath, saveAsOptions);

                if (isWorkshared)
                {
                    try
                    {
                        document.FreeTheModel();
                    }
                    catch { }
                }

                document?.Close();

                if (isWorkshared)
                {
                    TransmissionData transmissionData = TransmissionData.ReadTransmissionData(modelDetachedPath);
                    if (transmissionData is not null)
                    {
                        transmissionData.IsTransmitted = true;
                        TransmissionData.WriteTransmissionData(modelDetachedPath, transmissionData);
                    }
                    try
                    {
                        Directory.Delete(fileDetachedPath.Replace(".rvt", "_backup"), true);
                    }
                    catch { }
                }
            }
        }
    }
}