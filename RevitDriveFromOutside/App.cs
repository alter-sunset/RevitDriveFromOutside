using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using RevitDriveFromOutside.Events;
using RevitDriveFromOutside.Events.Detach;
using RevitDriveFromOutside.Events.Transmit;

namespace RevitDriveFromOutside
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            app.ControlledApplication.ApplicationInitialized += OnInitialized;
            return Result.Succeeded;
        }

        private async void OnInitialized(object? sender, ApplicationInitializedEventArgs e)
        {
            //Initialize all External Events
            IEventHolder[] events =
            [
                new TransmitEventHolder(),
                new DetachEventHolder(),
            ];

            //Initialize Task Handler and pass Event instances to it
            ExternalTaskHandler externalTaskHandler = new(events);

            //Start listener, duh
#if R25_OR_GREATER
            await externalTaskHandler.LookForSingleTask(TimeSpan.FromMinutes(1));
#else
            externalTaskHandler.LookForSingleTask(TimeSpan.FromMinutes(1));
#endif
        }

        public Result OnShutdown(UIControlledApplication app)
        {
            app.ControlledApplication.ApplicationInitialized -= OnInitialized;
            return Result.Succeeded;
        }
    }
}