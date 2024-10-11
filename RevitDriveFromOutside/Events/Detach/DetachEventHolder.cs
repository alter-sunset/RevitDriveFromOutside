using Autodesk.Revit.UI;

namespace RevitDriveFromOutside.Events.Detach
{
    public class DetachEventHolder : IEventHolder
    {
        private readonly EventHandlerDetach _eventHandlerDetach = new();
        private readonly ExternalEvents _externalEventDetach = ExternalEvents.Detach;
        public ExternalEvents ExternalEvent => _externalEventDetach;
        public IExternalEventHandler ExternalEventHandler => _eventHandlerDetach;
    }
}