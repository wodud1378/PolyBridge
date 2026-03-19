using PolyBridge.Core.Attributes;

// 콜백 브릿지 — 비동기 메서드의 성공/실패 처리 (internal: SDK 내부 구현)
[NativeBridge("com.test.service.IServiceCallback")]
internal partial class TestServiceCallback
{
    // 기본 결과 핸들러 — 여러 비동기 메서드에서 공유 (AllowMultiple)
    [BridgeResult(nameof(TestService.RequestLoginAsync))]
    [BridgeResult(nameof(TestService.FetchDataAsync))]
    [BridgeResult(nameof(TestService.LoadProfileAsync))]
    public partial void onSuccess(string result);

    // 전용 결과 핸들러 — 타입 일치로 변환 없이 직접 전달
    [BridgeResult(nameof(TestService.GetCountAsync))]
    public partial void onCountResult(int count);
    [BridgeError(nameof(TestService.GetCountAsync))]
    public partial void onCountError();

    // 에러 핸들러 — 모든 비동기 메서드에서 공유
    [BridgeError(nameof(TestService.RequestLoginAsync))]
    [BridgeError(nameof(TestService.FetchDataAsync))]
    [BridgeError(nameof(TestService.LoadProfileAsync))]
    public partial void onError(string error);
}

// 이벤트 브릿지 — 네이티브→C# 이벤트 수신 (internal: SDK 내부 구현)
[NativeBridge("com.test.service.IServiceEventListener")]
internal partial class TestServiceEventBridge
{
    // 단일 파라미터
    public partial void onStateChanged(string state);

    // 복수 파라미터
    public partial void onProgress(int current, int total);

    // 파라미터 없음
    public partial void onCompleted();
}
