using System.Threading.Tasks;
using PolyBridge.Core.Attributes;
using PolyBridge.Sandbox;

[NativeService("com.polybridge.sample.timer.TimerService",
    BridgeType = typeof(TimerServiceBridge))]
[Sandbox("Timer Service")]
public partial class TimerService
{
    // 동기
    [NativeMethod("isRunning")]
    [SandboxMethod("Is Running")]
    public partial bool IsRunning();

    [NativeMethod("getTickCount")]
    [SandboxMethod("Get Tick Count")]
    public partial int GetTickCount();

    [NativeMethod("getInterval")]
    [SandboxMethod("Get Interval")]
    public partial long GetInterval();

    // 비동기
    [NativeMethod("start")]
    [SandboxMethod("Start Timer")]
    public partial Task<string> StartAsync(long intervalMs);

    [NativeMethod("stop")]
    [SandboxMethod("Stop Timer")]
    public partial Task<string> StopAsync();

    [NativeMethod("reset")]
    [SandboxMethod("Reset Timer")]
    public partial Task<string> ResetAsync();

    // Editor Mock
    [MockReturn(nameof(IsRunning))]
    internal bool MockReturnIsRunning() => false;

    [MockReturn(nameof(GetTickCount))]
    internal int MockReturnGetTickCount() => 0;

    [MockReturn(nameof(GetInterval))]
    internal long MockReturnGetInterval() => 1000L;

    [MockReturn(nameof(StartAsync))]
    internal Task<string> MockReturnStartAsync() => Task.FromResult("started");

    [MockReturn(nameof(StopAsync))]
    internal Task<string> MockReturnStopAsync() => Task.FromResult("stopped:0");

    [MockReturn(nameof(ResetAsync))]
    internal Task<string> MockReturnResetAsync() => Task.FromResult("reset:0");
}
