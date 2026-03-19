# PolyBridge.Core

Unity 런타임에서 사용되는 핵심 라이브러리. 어트리뷰트, 런타임 유틸리티, 직렬화를 포함.

## 디렉토리

```
Attributes/       어트리뷰트 ([NativeService], [NativeMethod], [NativeBridge], [BridgeResult], [BridgeError], [MockImpl], [MockReturn], [PolyBridgeConfiguration])
Runtime/          런타임 유틸리티 (AndroidBridge, IOSBridgeCallback, NativeDispatcher)
Serialization/    직렬화 인터페이스 및 레지스트리
```

## 어트리뷰트

### NativeService

네이티브 브릿지 클래스를 선언.

```csharp
[NativeService("com.example.MyPlugin",
    CallbackBridgeType = typeof(MyPluginCallback),
    EventBridgeType = typeof(MyPluginEventBridge))]
public partial class MyPlugin { }
```

| 프로퍼티 | 설명 | 기본값 |
|---|---|---|
| `AndroidClassPath` | Java 브릿지 클래스 경로 (필수) | — |
| `CallbackBridgeType` | 콜백 브릿지 클래스 (typeof) | null |
| `EventBridgeType` | 이벤트 브릿지 클래스 (typeof) | null |

### NativeMethod

네이티브 메서드 선언. 플랫폼별 이름을 지정 가능.

```csharp
[NativeMethod]
public partial void Ping();

[NativeMethod("android_fetch", "ios_fetch")]
public partial Task<string> FetchAsync();
```

### NativeBridge

네이티브→C# 통신을 위한 통합 브릿지 클래스. 콜백과 이벤트 두 가지 용도로 사용. Android에서 클래스 자체가 `AndroidJavaProxy`로 생성되어 Java 인터페이스 메서드와 1:1 매칭.

**콜백 브릿지** — `[BridgeResult(nameof(Method))]`/`[BridgeError(nameof(Method))]`로 대상 서비스 메서드를 지정 (필수):
```csharp
[NativeBridge("com.example.IPluginCallback")]
public partial class MyPluginCallback
{
    [BridgeResult(nameof(MyPlugin.GetUserAsync))]
    public partial void onSuccess(string result);

    [BridgeResult(nameof(MyPlugin.GetCountAsync))]
    public partial void onCountResult(int count);

    [BridgeError(nameof(MyPlugin.GetUserAsync))]
    public partial void onError(string error);

    [BridgeError(nameof(MyPlugin.GetCountAsync))]
    public partial void onCountError();
}
```

**이벤트 브릿지** — partial 메서드 자체가 이벤트:
```csharp
[NativeBridge("com.example.IPluginEventListener")]
public partial class MyPluginEventBridge
{
    public partial void onStateChanged(string state);
    public partial void onPaymentCompleted(string receipt, int amount);
}
```

- 메서드명/파라미터를 자유롭게 정의 — Java 인터페이스와 정확히 일치시키면 됨
- 제너레이터가 `event Action<T>` 선언, partial 메서드 구현, `IDisposable` 자동 생성
- `NativeService`에서 `CallbackBridgeType = typeof(...)` / `EventBridgeType = typeof(...)` 로 연결

### BridgeResult / BridgeError

`NativeBridge`에서 콜백 용도로 사용 시, 대상 서비스 메서드를 지정하는 어트리뷰트. `nameof(Method)` 필수, `AllowMultiple = true`.

```csharp
// 대상 서비스 메서드명 지정 필수 (파라미터 없는 생성자 없음)
[BridgeResult(nameof(MyPlugin.GetUserAsync))]
public partial void onSuccess(string result);

// 동일 타입(int→int)이면 직접 전달, string→다른 타입은 Parse/Deserialize 자동 적용
[BridgeResult(nameof(MyPlugin.GetCountAsync))]
public partial void onCountResult(int count);

// 에러 핸들러도 대상 메서드 지정 필수
[BridgeError(nameof(MyPlugin.GetUserAsync))]
public partial void onError(string error);

// 0-파라미터 에러 핸들러 → () => 람다로 생성
[BridgeError(nameof(MyPlugin.GetCountAsync))]
public partial void onCountError();

// AllowMultiple = true — 하나의 브릿지 메서드가 여러 서비스 메서드를 처리 가능
[BridgeResult(nameof(MyPlugin.MethodA))]
[BridgeResult(nameof(MyPlugin.MethodB))]
public partial void onSharedResult(string result);
```

### MockImpl / MockReturn

에디터 환경에서 네이티브 호출 대신 사용할 Mock 메서드를 지정. `[Conditional("UNITY_EDITOR")]` 적용으로 프로덕션 빌드에서 제거.

```csharp
[MockImpl(nameof(DoSomething))]
internal void MockImplDoSomething() { }

[MockReturn(nameof(GetValueAsync))]
internal Task<int> MockReturnGetValueAsync() => Task.FromResult(42);
```

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
> 비동기 메서드는 `CallbackBridgeType`으로 지정된 NativeBridge 인스턴스를 사용.

## 직렬화

`IPolyBridgeSerializer` 커스텀 직렬화 지원.

```csharp
PolyBridgeSerializerRegistry.Serializer = new MyCustomSerializer();
```
