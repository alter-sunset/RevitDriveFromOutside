using Autodesk.Revit.ApplicationServices;
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
                detachConfig.DetachModel(application, filePath);
            }
        }
    }
}