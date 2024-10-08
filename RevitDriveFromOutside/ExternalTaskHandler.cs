using System.Text.Json;
using RevitDriveFromOutside.Events;
using RevitDriveFromOutside.Events.Detach;
using RevitDriveFromOutside.Events.Transmit;
using RevitDriveFromOutside.Utils;

namespace RevitDriveFromOutside
{
    public class ExternalTaskHandler(List<IEventHolder> eventHolders)
    {
        private readonly List<IEventHolder> _eventHolders = eventHolders;

        private static readonly string FOLDER_CONFIGS = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"RevitListener\Tasks");
        /// <summary>
        /// Method that will listen for new Tasks and run them accordingly
        /// </summary>
        /// <param name="period">Frequency of method execution</param>
        /// <returns></returns>
        public async Task LookForSingleTask(TimeSpan period)
        {
            using PeriodicTimer timer = new(period);
            while (await timer.WaitForNextTickAsync())
            {
                TaskConfig taskConfig = GetOldestMessage();
                if (taskConfig != null)
                    RaiseEvent(taskConfig);
            }
        }
        /// <summary>
        /// This will get the oldest file from folder
        /// </summary>
        /// <returns></returns>
        public static TaskConfig GetOldestMessage()
        {
            string? file = Directory.GetFiles(FOLDER_CONFIGS)
                .OrderBy(File.GetLastWriteTime)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(file))
                return null;
            using FileStream fileStream = File.OpenRead(file);
            JsonDocument document = JsonDocument.Parse(fileStream);
            fileStream.Close();
            fileStream.Dispose();
            JsonElement root = document.RootElement;

            TaskConfig taskConfig = new()
            {
                ExternalEvent = root
                    .GetProperty("ExternalEvent")
                    .Deserialize<ExternalEvents>(),

                EventConfig = root.GetProperty("EventConfig"),

                FilePath = file
            };
            return taskConfig;
        }
        private void RaiseEvent(TaskConfig taskConfig)
        {
            IEventHolder? eventHolder = _eventHolders.FirstOrDefault(e => e.ExternalEvent == taskConfig.ExternalEvent);
            switch (taskConfig.ExternalEvent)
            {
                case ExternalEvents.Transmit:
                    TransmitConfig transmitConfig = taskConfig.GetEventConfig<TransmitConfig>();
                    EventHandlerTransmit eventHandlerTransmit = eventHolder.ExternalEventHandler as EventHandlerTransmit;
                    eventHandlerTransmit.Raise(transmitConfig);
                    break;

                case ExternalEvents.Detach:
                    DetachConfig detachConfig = taskConfig.GetEventConfig<DetachConfig>();
                    EventHandlerDetach eventHandlerDetach = eventHolder.ExternalEventHandler as EventHandlerDetach;
                    eventHandlerDetach.Raise(detachConfig);
                    break;

                default:
                    return;
            }
            File.Delete(taskConfig.FilePath);
        }
    }
}