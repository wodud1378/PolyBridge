using PolyBridge.Core.Attributes;

[NativeBridge("com.polybridge.sample.timer.ITimerBridge",
    EventListenerAdd = "setEventListener",
    EventListenerRemove = "removeEventListener")]
internal partial class TimerServiceBridge
{
    // Callback
    [NativeBridgeResult(nameof(TimerService.StartAsync))]
    [NativeBridgeResult(nameof(TimerService.StopAsync))]
    [NativeBridgeResult(nameof(TimerService.ResetAsync))]
    public partial void onSuccess(string result);

    [NativeBridgeError(nameof(TimerService.StartAsync))]
    [NativeBridgeError(nameof(TimerService.StopAsync))]
    [NativeBridgeError(nameof(TimerService.ResetAsync))]
    public partial void onError(string error);

    // Event
    public partial void onTick(string count);
    public partial void onTimerStateChanged(string state);
}
