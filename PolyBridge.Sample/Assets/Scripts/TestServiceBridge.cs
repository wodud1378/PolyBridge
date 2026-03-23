using PolyBridge.Core.Attributes;

// 통합 브릿지 — 콜백(BridgeResult/BridgeError) + 이벤트를 하나의 클래스에서 처리
[NativeBridge("com.test.service.IServiceBridge")]
internal partial class TestServiceBridge
{
    // === 콜백 — BridgeResult/BridgeError로 비동기 메서드 매핑 ===

    // 공유 결과 핸들러 — 여러 비동기 메서드에서 사용 (AllowMultiple)
    [NativeBridgeResult(nameof(TestService.RequestLoginAsync))]
    [NativeBridgeResult(nameof(TestService.FetchDataAsync))]
    [NativeBridgeResult(nameof(TestService.LoadProfileAsync))]
    public partial void onSuccess(string result);

    // 전용 결과 핸들러 — 타입 일치로 변환 없이 직접 전달
    [NativeBridgeResult(nameof(TestService.GetCountAsync))]
    public partial void onCountResult(int count);

    // 전용 에러 핸들러 — 파라미터 없음
    [NativeBridgeError(nameof(TestService.GetCountAsync))]
    public partial void onCountError();

    // 공유 에러 핸들러
    [NativeBridgeError(nameof(TestService.RequestLoginAsync))]
    [NativeBridgeError(nameof(TestService.FetchDataAsync))]
    [NativeBridgeError(nameof(TestService.LoadProfileAsync))]
    public partial void onError(string error);

    // === 이벤트 — 어트리뷰트 없음, partial 메서드 자체가 이벤트 ===

    // 단일 파라미터
    public partial void onStateChanged(string state);

    // 복수 파라미터
    public partial void onProgress(int current, int total);

    // 파라미터 없음
    public partial void onCompleted();
}
