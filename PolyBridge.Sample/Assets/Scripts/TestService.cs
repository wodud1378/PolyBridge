using System.Threading;
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;
using PolyBridge.Sandbox;

[NativeService("com.test.service.TestPlugin",
    BridgeType = typeof(TestServiceBridge))]
[Sandbox("Test Service")]
public partial class TestService
{
    // === 동기 메서드 ===

    [NativeMethod("initialize")]
    [SandboxMethod("Initialize")]
    public partial void Initialize();

    [NativeMethod("isAvailable")]
    [SandboxMethod("Is Available")]
    public partial bool IsAvailable();

    [NativeMethod("android_getDeviceId", "ios_getDeviceId")]
    [SandboxMethod("Get Device ID")]
    public partial string GetDeviceId();

    // === 비동기 메서드 ===

    [NativeMethod("requestLogin")]
    [SandboxMethod("Request Login")]
    public partial Task RequestLoginAsync();

    [NativeMethod("fetchData")]
    [SandboxMethod("Fetch Data")]
    public partial Task<string> FetchDataAsync(string key);

    [NativeMethod("loadProfile")]
    [SandboxMethod("Load Profile")]
    public partial Task<UserInfo> LoadProfileAsync(string userId, CancellationToken ct);

    [NativeMethod("getCount")]
    [SandboxMethod("Get Count")]
    public partial Task<int> GetCountAsync();

    // === Editor Mock ===

    [MockImpl(nameof(Initialize))]
    internal void MockImplInitialize() { }

    [MockReturn(nameof(IsAvailable))]
    internal bool MockReturnIsAvailable() => true;

    [MockReturn(nameof(FetchDataAsync))]
    internal Task<string> MockReturnFetchDataAsync() => Task.FromResult("{\"mock\":true}");

    [MockReturn(nameof(GetCountAsync))]
    internal Task<int> MockReturnGetCountAsync() => Task.FromResult(42);
}
