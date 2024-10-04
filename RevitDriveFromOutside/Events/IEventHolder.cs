using Autodesk.Revit.UI;

namespace RevitDriveFromOutside.Events
{
    public interface IEventHolder
    {
        public ExternalEvents ExternalEvent { get; }
        public IExternalEventHandler ExternalEventHandler { get; }
    }
}