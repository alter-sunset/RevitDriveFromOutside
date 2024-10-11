# RevitDriveFromOutside

  A lifelong dream has finally come true!
  
  Using this template, you will be able to send commands to the Revit application from the outside!

  Yeah, that would be awesome, though that's not exectly the case.
  Nevertheless, with this template, you can achieve some degree of external control over Revit.

  1. Register all your external events, in some valid Revit context (ApplicationInitialized event in this case).
  ```c#
List<IEventHolder> events =
[
    new TransmitEventHolder(),
    new DetachEventHolder(),
];
```
```c#
public interface IEventHolder
{
    public ExternalEvents ExternalEvent { get; }
    public IExternalEventHandler ExternalEventHandler { get; }
}
```
```c#
public enum ExternalEvents
{
    Transmit,
    Detach
}
```

  2. Deploy a listener that will keep track of incoming messages.
  (Here we have the simplest solution I could think of: put the configuration files (JSON) in some folder and tell your listener to check them periodically).
```c#
ExternalTaskHandler externalTaskHandler = new(events);
await externalTaskHandler.LookForSingleTask(TimeSpan.FromMinutes(1));
```
  3. Combine message reading and external event invocation using configuration from the message.
```c#
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
```
  *Fun fact, if you want to send default BuiltInCommands to Revit without using Transactions, you can do it without external events (more or less so).
