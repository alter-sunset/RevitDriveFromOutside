using RevitDriveFromOutside.Events;
using System.Text.Json;

namespace RevitDriveFromOutside.Utils
{
    public static class ConfigHelper
    {
        public static T GetEventConfig<T>(this TaskConfig taskConfig)
        {
            return ((JsonElement)taskConfig.EventConfig).Deserialize<T>();
        }
    }
}