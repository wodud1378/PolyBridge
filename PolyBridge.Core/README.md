# PolyBridge.Core

Unity 런타임에서 사용되는 핵심 라이브러리. 어트리뷰트, 런타임 유틸리티, 직렬화를 포함.

## 디렉토리

```
Attributes/       어트리뷰트 ([NativeService], [NativeMethod], [NativeBridge], [NativeBridgeResult], [NativeBridgeError], [MockImpl], [MockReturn], [PolyBridgeConfiguration])
Runtime/          런타임 유틸리티 (AndroidBridge, IOSBridgeCallback, NativeDispatcher)
Serialization/    직렬화 인터페이스 및 레지스트리
```

## 어트리뷰트

### NativeService

네이티브 브릿지 클래스를 선언.

```csharp
[NativeService("com.test.service.TestPlugin",
    BridgeType = typeof(TestServiceBridge))]
public partial class TestService { }
```

| 프로퍼티 | 설명 | 기본값 |
|---|---|---|
| `AndroidClassPath` | Java 브릿지 클래스 경로 (필수, 생성자 파라미터) | -- |
| `BridgeType` | NativeBridge 클래스 (typeof) -- 콜백과 이벤트를 통합 처리 | null |

### NativeMethod

네이티브 메서드 선언. 플랫폼별 이름을 지정 가능.

```csharp
[NativeMethod]
public partial void Ping();

[NativeMethod("android_fetch", "ios_fetch")]
public partial Task<string> FetchAsync();
```

| 파라미터 | 설명 | 기본값 |
|---|---|---|
| `androidName` | Android에서의 메서드명 | null (C# 메서드명 사용) |
| `iosName` | iOS에서의 메서드명 | null (C# 메서드명 사용) |

### NativeBridge

네이티브에서 C#으로의 통신을 위한 통합 브릿지 클래스. 하나의 클래스에서 콜백과 이벤트를 함께 처리. Android에서 클래스 자체가 `AndroidJavaProxy`로 생성되어 Java 인터페이스 메서드와 1:1 매칭.

- **콜백 메서드** -- `[NativeBridgeResult]`/`[NativeBridgeError]` 어트리뷰트가 붙은 메서드. 비동기 서비스 메서드의 결과/에러를 수신
- **이벤트 메서드** -- 어트리뷰트가 없는 메서드. partial 메서드 자체가 이벤트로 생성

```csharp
[NativeBridge("com.test.service.IServiceBridge",
    EventListenerAdd = "addListener",
    EventListenerRemove = "removeListener")]
internal partial class TestServiceBridge
{
    // 콜백 -- NativeBridgeResult/NativeBridgeError로 비동기 메서드 매핑
    [NativeBridgeResult(nameof(TestService.RequestLoginAsync))]
    [NativeBridgeResult(nameof(TestService.FetchDataAsync))]
    [NativeBridgeResult(nameof(TestService.LoadProfileAsync))]
    public partial void onSuccess(string result);

    [NativeBridgeResult(nameof(TestService.GetCountAsync))]
    public partial void onCountResult(int count);

    [NativeBridgeError(nameof(TestService.GetCountAsync))]
    public partial void onCountError();

    [NativeBridgeError(nameof(TestService.RequestLoginAsync))]
    [NativeBridgeError(nameof(TestService.FetchDataAsync))]
    [NativeBridgeError(nameof(TestService.LoadProfileAsync))]
    public partial void onError(string error);

    // 이벤트 -- 어트리뷰트 없음, partial 메서드 자체가 이벤트
    public partial void onStateChanged(string state);
    public partial void onProgress(int current, int total);
    public partial void onCompleted();
}
```

| 프로퍼티 | 설명 | 기본값 |
|---|---|---|
| `AndroidInterfacePath` | Java 인터페이스 경로 (필수, 생성자 파라미터) | -- |
| `EventListenerAdd` | 네이티브 측 리스너 등록 메서드명 | null |
| `EventListenerRemove` | 네이티브 측 리스너 해제 메서드명 | null |

- 메서드명/파라미터를 자유롭게 정의 -- Java 인터페이스와 정확히 일치시키면 됨
- `HasEventListener` 판정은 `EventListenerAdd` 존재 여부로 결정
- 제너레이터가 `event Action<T>` 선언, partial 메서드 구현, `IDisposable` 자동 생성
- `NativeService`에서 `BridgeType = typeof(...)` 로 연결
- 생성 코드에서 `_nativeBridge` 필드, `Bridge` 프로퍼티, `RegisterBridge` 메서드가 자동 생성

### NativeBridgeResult / NativeBridgeError

`NativeBridge`에서 콜백 용도로 사용 시, 대상 서비스 메서드를 지정하는 어트리뷰트. `methodName` 필수, `AllowMultiple = true`.

```csharp
// 대상 서비스 메서드명 지정 필수 (파라미터 없는 생성자 없음)
[NativeBridgeResult(nameof(TestService.FetchDataAsync))]
public partial void onSuccess(string result);

// 동일 타입(int->int)이면 직접 전달, string->다른 타입은 Parse/Deserialize 자동 적용
[NativeBridgeResult(nameof(TestService.GetCountAsync))]
public partial void onCountResult(int count);

// 에러 핸들러도 대상 메서드 지정 필수
[NativeBridgeError(nameof(TestService.FetchDataAsync))]
public partial void onError(string error);

// 0-파라미터 에러 핸들러 -> () => 람다로 생성
[NativeBridgeError(nameof(TestService.GetCountAsync))]
public partial void onCountError();

// AllowMultiple = true -- 하나의 브릿지 메서드가 여러 서비스 메서드를 처리 가능
[NativeBridgeResult(nameof(TestService.RequestLoginAsync))]
[NativeBridgeResult(nameof(TestService.FetchDataAsync))]
public partial void onSharedResult(string result);
```

어트리뷰트가 없는 메서드는 이벤트로 취급되어 `event Action<T>` 형태로 생성.

### MockImpl / MockReturn

에디터 환경에서 네이티브 호출 대신 사용할 Mock 메서드를 지정. `[Conditional("UNITY_EDITOR")]` 적용으로 프로덕션 빌드에서 제거.

```csharp
[MockImpl(nameof(DoSomething))]
internal void MockImplDoSomething() { }

[MockReturn(nameof(GetValueAsync))]
internal Task<int> MockReturnGetValueAsync() => Task.FromResult(42);
```

| 어트리뷰트 | 파라미터 | 설명 |
|---|---|---|
| `MockImpl` | `methodName` | void 메서드의 Mock 구현 |
| `MockReturn` | `methodName` | 반환값이 있는 메서드의 Mock 구현 |

### PolyBridgeConfiguration

어셈블리 레벨 설정. 생성 코드를 물리 파일로 출력할지 제어.

```csharp
[assembly: PolyBridgeConfiguration(EmitPhysicalFiles = true)]
```

## 런타임

- **NativeDispatcher**: 네이티브 콜백을 메인 스레드로 전달하는 `SynchronizationContext` 관리
- **AndroidBridge**: `AndroidJavaObject` 래퍼. 동기/비동기 Java 메서드 호출
- **IOSBridgeCallback**: iOS P/Invoke 비동기 콜백 관리. `Register`/`Unregister`/`OnResult`

> Android 프록시는 `NativeBridge` 클래스 자체가 `AndroidJavaProxy`로 생성.
> 비동기 메서드는 `BridgeType`으로 지정된 NativeBridge 인스턴스를 사용.

## 직렬화

`IPolyBridgeSerializer` 커스텀 직렬화 지원.

```csharp
PolyBridgeSerializerRegistry.Serializer = new MyCustomSerializer();
```
