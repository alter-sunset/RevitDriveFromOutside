# RevitDriveFromOutside

  A lifelong dream finally comes true!
  
  With this template you can send commands to your Revit app from the outside!

  Yeah, that would be awesome, though that's not exectly the case.
  Yet, with this template you can achieve external control over your Revit to a certain extent.

  Firstly you have to register all your external events, in some valid Revit context (ApplicationInitialized event in this case).
  ```c#
List<IEventHolder> events = [];
events.Add(new TransmitEventHolder());
events.Add(new DetachEventHolder());
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

  Then you have to deploy a listener that will monitor incoming messages.
  (Here you have a simlpest solution I could think of: put config files (JSON) inside some folder, and tell your listener to periodically check for them).
```c#
ExternalTaskHandler externalTaskHandler = new(application, events);
await externalTaskHandler.ListenForNewTasks(TimeSpan.FromMinutes(1));
```
```c#
public interface IExternalTaskHandler
{
    Task ListenForNewTasks(TimeSpan period);
    public List<TaskConfig> ReadMessages();
}
```
  Then chain together message reader and raising of your external event using config from a message.
```c#
public async Task ListenForNewTasks(TimeSpan period)
{
    using PeriodicTimer timer = new(period);
    while (await timer.WaitForNextTickAsync())
    {
        List<TaskConfig> configs = ReadMessages();

        foreach (TaskConfig config in configs)
        {
            RaiseEvent(config);
        }
    }
}
```
  *Fun fact, if you want to send default BuiltInCommands to Revit without using Transactions, you can do it without external events (more or less so).
