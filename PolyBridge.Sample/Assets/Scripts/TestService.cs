using System.Threading;
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

// 모든 기능 총 집합 — 브릿지 1개로 콜백 + 이벤트 통합
[NativeService("com.test.service.TestPlugin",
    BridgeType = typeof(TestServiceBridge))]
public partial class TestService
{
    // === 동기 메서드 ===

    [NativeMethod("initialize")]
    public partial void Initialize();

    [NativeMethod("isAvailable")]
    public partial bool IsAvailable();

    [NativeMethod("android_getDeviceId", "ios_getDeviceId")]
    public partial string GetDeviceId();

    // === 비동기 메서드 ===

    [NativeMethod("requestLogin")]
    public partial Task RequestLoginAsync();

    [NativeMethod("fetchData")]
    public partial Task<string> FetchDataAsync(string key);

    [NativeMethod("loadProfile")]
    public partial Task<UserInfo> LoadProfileAsync(string userId, CancellationToken ct);

    [NativeMethod("getCount")]
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
