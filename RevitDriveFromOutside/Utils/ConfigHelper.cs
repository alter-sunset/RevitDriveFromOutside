using RevitDriveFromOutside.Events;

namespace RevitDriveFromOutside.Utils
{
    public static class ConfigHelper
    {
        public static T GetEventConfig<T>(this TaskConfig taskConfig) => taskConfig.EventConfig.ToObject<T>();
    }
}