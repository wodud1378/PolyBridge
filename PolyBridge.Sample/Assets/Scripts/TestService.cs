using System.Threading;
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

// 모든 기능 총 집합
[NativeService("com.test.service.TestPlugin",
    CallbackBridgeType = typeof(TestServiceCallback),
    EventBridgeType = typeof(TestServiceEventBridge))]
public partial class TestService
{
    // === 동기 메서드 ===

    // void
    [NativeMethod("initialize")]
    public partial void Initialize();

    // 반환값
    [NativeMethod("isAvailable")]
    public partial bool IsAvailable();

    // 플랫폼별 네이티브 이름 지정
    [NativeMethod("android_getDeviceId", "ios_getDeviceId")]
    public partial string GetDeviceId();

    // === 비동기 메서드 ===

    // Task (void 비동기) — 기본 BridgeResult(onSuccess) 사용
    [NativeMethod("requestLogin")]
    public partial Task RequestLoginAsync();

    // Task<string> — 기본 BridgeResult(onSuccess) + string→string 변환 없음
    [NativeMethod("fetchData")]
    public partial Task<string> FetchDataAsync(string key);

    // Task<string> + CancellationToken
    [NativeMethod("loadProfile")]
    public partial Task<UserInfo> LoadProfileAsync(string userId, CancellationToken ct);

    // Task<int> — 전용 BridgeResult(nameof(GetCountAsync)) → onCountResult(int) 직접 전달
    [NativeMethod("getCount")]
    public partial Task<int> GetCountAsync();

    // === Editor Mock ===

    // MockImpl — void 메서드용
    [MockImpl(nameof(Initialize))]
    internal void MockImplInitialize() { }

    // MockReturn — 반환값 메서드용
    [MockReturn(nameof(IsAvailable))]
    internal bool MockReturnIsAvailable() => true;

    [MockReturn(nameof(FetchDataAsync))]
    internal Task<string> MockReturnFetchDataAsync() => Task.FromResult("{\"mock\":true}");

    [MockReturn(nameof(GetCountAsync))]
    internal Task<int> MockReturnGetCountAsync() => Task.FromResult(42);
}
