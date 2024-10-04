using Autodesk.Revit.UI;

namespace RevitDriveFromOutside.Events.Detach
{
    public class DetachEventHolder : IEventHolder
    {
        private readonly EventHandlerDetach _eventHandlerDetach = new();
        private readonly ExternalEvents _externalEventDetach = ExternalEvents.Detach;
        public ExternalEvents ExternalEvent { get => _externalEventDetach; }
        public IExternalEventHandler ExternalEventHandler { get => _eventHandlerDetach; }
    }
}