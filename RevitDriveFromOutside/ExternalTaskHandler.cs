using System.Text.Json;
using RevitDriveFromOutside.Events;
using RevitDriveFromOutside.Events.Detach;
using RevitDriveFromOutside.Events.Transmit;
using RevitDriveFromOutside.Utils;

namespace RevitDriveFromOutside
{
    public class ExternalTaskHandler(IEventHolder[] eventHolders)
    {
        private readonly IEventHolder[] _eventHolders = eventHolders;

        private static readonly string FOLDER_CONFIGS = InitializeFolderConfigs();
        private static string InitializeFolderConfigs() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
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
                if (taskConfig is not null)
                    RaiseEvent(taskConfig);
            }
        }
        /// <summary>
        /// This will get the oldest file from folder
        /// </summary>
        /// <returns></returns>
        public static TaskConfig GetOldestMessage()
        {
            string[] files = [.. Directory.GetFiles(FOLDER_CONFIGS).OrderBy(File.GetLastWriteTime)];
            if (files.Length == 0) return null;

            using FileStream fileStream = File.OpenRead(files[0]);
            using JsonDocument document = JsonDocument.Parse(fileStream);
            JsonElement root = document.RootElement;

            return new TaskConfig
            {
                ExternalEvent = root.GetProperty("ExternalEvent").Deserialize<ExternalEvents>(),
                EventConfig = root.GetProperty("EventConfig"),
                FilePath = files[0]
            };
        }
        private void RaiseEvent(TaskConfig taskConfig)
        {
            IEventHolder eventHolder = _eventHolders
                .FirstOrDefault(e => e.ExternalEvent == taskConfig.ExternalEvent);
            if (eventHolder is null) return;

            // Create a dictionary mapping external events to their handlers
            Dictionary<ExternalEvents, Action> eventHandlers = new()
            {
                { ExternalEvents.Transmit, () =>
                    {
                        TransmitConfig transmitConfig = taskConfig.GetEventConfig<TransmitConfig>();
                        EventHandlerTransmit? handler = eventHolder.ExternalEventHandler as EventHandlerTransmit;
                        handler?.Raise(transmitConfig);
                    }
                },
                { ExternalEvents.Detach, () =>
                    {
                        DetachConfig detachConfig = taskConfig.GetEventConfig<DetachConfig>();
                        EventHandlerDetach? handler = eventHolder.ExternalEventHandler as EventHandlerDetach;
                        handler?.Raise(detachConfig);
                    }
                },
            };

            // Invoke the appropriate event handler if it exists
            if (eventHandlers.TryGetValue(taskConfig.ExternalEvent, out var raiseEvent))
                raiseEvent();

            // Delete the file after raising the event
            File.Delete(taskConfig.FilePath);
        }
    }
}