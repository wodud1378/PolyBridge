# PolyBridge.Generator

Roslyn `IIncrementalGenerator` 기반 소스 제너레이터. `[NativeService]`와 `[NativeBridge]` 클래스를 분석하여 플랫폼별 구현 코드를 자동 생성.

## 디렉토리

```
Models/       데이터 모델 (ServiceModel, MethodModel, BridgeModel, BridgeMethodModel, ParameterModel)
Generators/   코드 생성기 (AndroidGenerator, IOSGenerator, EditorImplGenerator, NativeBridgeGenerator)
Builders/     코드 빌더 유틸리티 (CodeBuilder, SourceEmitter)
```

## 생성 파이프라인

### Pipeline 1: NativeService

1. `[NativeService]` + `partial class` 구문 감지
2. `ServiceModel` / `MethodModel` 추출
3. 서비스당 생성:
   - `I{Name}Impl` -- 인터페이스 (BridgeType이 있으면 `IDisposable` 상속)
   - `{Name}.g.cs` -- partial 클래스 (플랫폼 분기, `_nativeBridge` 필드, `Bridge` 프로퍼티, `RegisterBridge` 메서드, Dispose)
   - `{Name}EditorImpl` -- 에디터 구현 (`#if UNITY_EDITOR`)
   - `{Name}AndroidImpl` -- Android 구현 (비동기 메서드는 BridgeType 인스턴스 사용) (`#if UNITY_ANDROID`)
   - `{Name}IOSImpl` -- iOS 구현 (`#if UNITY_IOS`)

### Pipeline 2: NativeBridge

1. `[NativeBridge]` + `partial class` 구문 감지
2. `BridgeModel` / `BridgeMethodModel` 추출
3. 브릿지당 생성:
   - `{Name}.g.cs` -- event 선언, partial 메서드 구현 (NativeDispatcher.Post), IDisposable
   - `{Name}.Android.g.cs` -- `AndroidJavaProxy` 상속 + 생성자 (`#if UNITY_ANDROID`)

## Android 프록시 생성

### NativeBridge (콜백/이벤트 통합)

`NativeBridge` 클래스 자체가 `AndroidJavaProxy`로 생성. 하나의 브릿지에서 콜백과 이벤트를 모두 처리.

- **콜백 메서드** -- `[BridgeResult]`/`[BridgeError]` 어트리뷰트가 붙은 메서드
- **이벤트 메서드** -- 어트리뷰트가 없는 메서드

```csharp
// 생성: Android partial
#if UNITY_ANDROID
internal partial class TestServiceBridge : UnityEngine.AndroidJavaProxy
{
    public TestServiceBridge() : base("com.test.service.IServiceBridge") { }
}
#endif

// 생성: event + partial 메서드 구현 (이벤트 메서드만)
internal partial class TestServiceBridge : System.IDisposable
{
    public event System.Action<string> OnStateChanged;
    public event System.Action<int, int> OnProgress;
    public event System.Action OnCompleted;

    public partial void onStateChanged(string state)
    {
        NativeDispatcher.Post(() => OnStateChanged?.Invoke(state));
    }

    public partial void onProgress(int current, int total)
    {
        NativeDispatcher.Post(() => OnProgress?.Invoke(current, total));
    }

    public partial void onCompleted()
    {
        NativeDispatcher.Post(() => OnCompleted?.Invoke());
    }
}
```

서비스 partial 클래스에서 생성되는 코드:
```csharp
// TestService.g.cs
public partial class TestService : System.IDisposable
{
    private readonly TestServiceBridge _nativeBridge;
    internal TestServiceBridge Bridge => _nativeBridge;
    // ...
}

// TestServiceAndroidImpl.g.cs
internal void RegisterBridge(TestServiceBridge bridge)
{
    _nativeBridge = bridge;
    _bridge.Call("addListener", _nativeBridge);
}
```

## 핵심 모델

### MethodModel

| 속성 | 설명 |
|---|---|
| `AccessModifier` | 사용자 선언의 접근 제한자 |
| `PartialModifier` | 생성 시 사용할 한정자 (예: `"public partial"`) |
| `AllParameters` | CT 포함 전체 파라미터 |
| `NativeParameters` | CT 제외 파라미터 |
| `MockMethodName` | Mock 어트리뷰트로 지정된 메서드명 |

### BridgeMethodModel

| 속성 | 설명 |
|---|---|
| `Name` | 메서드명 (Java 인터페이스 메서드명과 동일) |
| `EventName` | C# event명 (첫 글자 대문자) |
| `Parameters` | 파라미터 목록 (복수 파라미터 지원) |
| `EventDelegateType` | 이벤트 델리게이트 타입 (예: `System.Action<string, int>`) |
| `TargetMethodName` | `[BridgeResult]`/`[BridgeError]`로 지정된 대상 서비스 메서드명 |

### 콜백 코드 생성 규칙

- `[BridgeResult(nameof(Method))]` / `[BridgeError(nameof(Method))]` -- 대상 메서드 지정 필수 (파라미터 없는 생성자 없음)
- `AllowMultiple = true` -- 하나의 브릿지 메서드가 여러 서비스 메서드를 처리 가능
- 0-파라미터 브릿지 메서드 -> `() =>` 람다로 생성
- 타입 변환: 동일 타입(예: `int`->`int`) -> 직접 전달, `string`->다른 타입 -> `Parse`/`Deserialize` 자동 적용
- 어트리뷰트가 없는 메서드는 이벤트로 취급

## 플랫폼 생성기 확장

`IPlatformGenerator` 구현체를 `PolyBridgeGenerator.Generators` 배열에 추가 시 새 플랫폼 지원 가능. 현재는 Android, iOS만 지원.
