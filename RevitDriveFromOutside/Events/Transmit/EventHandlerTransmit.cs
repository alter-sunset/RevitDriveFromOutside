using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitDriveFromOutside.Events.Transmit
{
    public class EventHandlerTransmit : RevitEventWrapper<TransmitConfig>
    {
        public override void Execute(UIApplication uiApp, TransmitConfig transmitConfig)
        {
            foreach (string file in transmitConfig.Files)
            {
                if (!File.Exists(file)) continue;

                string transmittedFilePath = Path.Combine(transmitConfig.FolderPath, Path.GetFileName(file));
                File.Copy(file, transmittedFilePath, true);

                ModelPath transmittedModelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(transmittedFilePath);
                TransmissionData transData = TransmissionData.ReadTransmissionData(transmittedModelPath);
                if (transData is null) continue;

                ICollection<ElementId> externalReferences = transData.GetAllExternalFileReferenceIds();
                foreach (ElementId refId in externalReferences)
                {
                    ExternalFileReference extRef = transData.GetLastSavedReferenceData(refId);
                    if (extRef.ExternalFileReferenceType is not ExternalFileReferenceType.RevitLink) continue;

                    transData.SetDesiredReferenceData(refId, extRef.GetPath(), extRef.PathType, false);
                }
                transData.IsTransmitted = true;
                TransmissionData.WriteTransmissionData(transmittedModelPath, transData);
            }
        }
    }
}