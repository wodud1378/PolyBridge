# PolyBridge

Unity에서 Android/iOS 네이티브 코드를 호출하기 위한 Roslyn 소스 제너레이터 기반 브릿지 라이브러리.

## 구조

| 프로젝트 | 설명 |
|---|---|
| **PolyBridge.Core** | 런타임 라이브러리 (어트리뷰트, 브릿지, 직렬화) |
| **PolyBridge.Generator** | Roslyn 소스 제너레이터 (플랫폼별 코드 자동 생성) |
| **PolyBridge.Test** | 단위 테스트 |

## 샘플 코드

```csharp
// 콜백 브릿지 — 서비스 메서드별 성공/실패 콜백 정의
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

// 이벤트 브릿지 — 메서드명이 Java 인터페이스와 1:1 매칭
[NativeBridge("com.example.IPluginEventListener")]
public partial class MyPluginEventBridge
{
    public partial void onStateChanged(string state);
    public partial void onPaymentCompleted(string receipt, int amount);
}

// 서비스 — 요청→응답 + 콜백/이벤트 브릿지 연결
[NativeService("com.example.MyPlugin",
    CallbackBridgeType = typeof(MyPluginCallback),
    EventBridgeType = typeof(MyPluginEventBridge))]
public partial class MyPlugin
{
    [NativeMethod]
    public partial void DoSomething();

    [NativeMethod]
    public partial Task<string> GetUserAsync(string userId);
}
```

빌드 시 플랫폼별 구현 코드와 Android 프록시 클래스가 자동 생성.

## 주요 기능

### 동기/비동기

`void`, 반환 타입, `Task`, `Task<T>`, `UniTask`, `UniTask<T>` 모두 지원.

```csharp
[NativeMethod]
public partial void Fire();

[NativeMethod]
public partial Task SendAsync();

[NativeMethod]
public partial Task<string> FetchAsync();
```

### NativeBridge

네이티브→C# 통신을 위한 통합 브릿지 클래스. `[NativeBridge]`를 붙이면 클래스 자체가 `AndroidJavaProxy`가 되어 각 partial 메서드가 Java 인터페이스 메서드와 직접 매핑.

콜백과 이벤트 두 가지 용도로 사용:

**콜백 브릿지** — 서비스 메서드별 결과/에러 수신 (`nameof`로 대상 메서드 지정 필수, `AllowMultiple = true`로 하나의 브릿지 메서드가 여러 서비스 메서드를 처리 가능):
```csharp
[NativeBridge("com.example.IPluginCallback")]
public partial class MyPluginCallback
{
    [BridgeResult(nameof(MyPlugin.GetUserAsync))]
    public partial void onSuccess(string result);

    // 반환 타입이 동일하면 직접 전달 (string→string, int→int 등)
    [BridgeResult(nameof(MyPlugin.GetCountAsync))]
    public partial void onCountResult(int count);

    [BridgeError(nameof(MyPlugin.GetUserAsync))]
    public partial void onError(string error);

    // 0-파라미터 에러 핸들러 → () => 람다로 생성
    [BridgeError(nameof(MyPlugin.GetCountAsync))]
    public partial void onCountError();
}
```

**이벤트 브릿지** — 네이티브→C# 이벤트 수신:
```csharp
[NativeBridge("com.example.IPluginEventListener")]
public partial class MyPluginEventBridge
{
    public partial void onStateChanged(string state);
    public partial void onPaymentCompleted(string receipt, int amount);
}

// 서비스에서 typeof로 연결
[NativeService("com.example.MyPlugin",
    CallbackBridgeType = typeof(MyPluginCallback),
    EventBridgeType = typeof(MyPluginEventBridge))]
public partial class MyPlugin { ... }

// 사용
var plugin = new MyPlugin();
plugin.EventBridge.OnStateChanged += state => Debug.Log(state);
plugin.EventBridge.OnPaymentCompleted += (receipt, amount) => Debug.Log($"{receipt}: {amount}");

// 정리
plugin.Dispose();
```

- 메서드명, 파라미터 수/타입을 자유롭게 정의 — Java 인터페이스와 정확히 일치시키면 됨
- 콜백 브릿지는 `[BridgeResult(nameof(Method))]`/`[BridgeError(nameof(Method))]`로 대상 서비스 메서드를 지정 (필수)
- `AllowMultiple = true` — 하나의 브릿지 메서드가 여러 서비스 메서드를 처리 가능
- 타입 변환: 동일 타입은 직접 전달, `string`→다른 타입은 `Parse`/`Deserialize` 자동 적용
- 이벤트 브릿지는 partial 메서드 자체가 이벤트
- 서비스와 분리되어 독립적으로 재사용 가능

### 네이티브 메서드명 지정

```csharp
[NativeMethod("android_getName", "ios_getName")]
public partial string GetName();
```

### CancellationToken

async 메서드에 `CancellationToken` 파라미터 추가 시 취소/타임아웃을 지원.
CT는 네이티브 호출에서 자동 제외, 취소 시 `TaskCanceledException`이 발생.

```csharp
[NativeMethod]
public partial Task<string> LoadDataAsync(string key, CancellationToken ct);
```

### Editor Mock

에디터 환경에서 네이티브 플러그인 대신 Mock 구현을 사용.
Mock 어트리뷰트가 없는 메서드는 `default` 값으로 폴백.

```csharp
[MockImpl(nameof(DoSomething))]
internal void MockImplDoSomething() { }

[MockReturn(nameof(GetValueAsync))]
internal Task<int> MockReturnGetValueAsync() => Task.FromResult(42);
```

### 커스텀 직렬화

Non-Blittable 타입은 자동으로 직렬화/역직렬화.
기본 Serializer는 `JsonUtility`이며 교체 가능.

```csharp
PolyBridgeSerializerRegistry.Serializer = new NewtonsoftSerializer();
```

## 네이티브 개발 가이드

### Android

PolyBridge는 C# 쪽 보일러플레이트를 생성하며, Java/Kotlin 쪽은 네이티브 개발자가 구현.

**콜백 인터페이스** (비동기 메서드용 — 메서드명은 사용자 정의, `[BridgeResult(nameof(Method))]`/`[BridgeError(nameof(Method))]`와 매칭):
```java
package com.example;

public interface IPluginCallback {
    void onSuccess(String result);
    void onError(String error);
}
```

**이벤트 리스너 인터페이스** (자유롭게 정의 — C# `NativeBridge` partial 메서드와 1:1 매칭):
```java
package com.example;

public interface IPluginEventListener {
    void onStateChanged(String state);
    void onPaymentCompleted(String receipt, int amount);
}
```

**브릿지 클래스 예시:**
```java
package com.example;

public class MyPlugin {
    private IPluginEventListener listener;

    // 동기
    public String getName() { return "hello"; }

    // 비동기
    public void getUserAsync(String userId, IPluginCallback callback) {
        try {
            String result = api.getUser(userId);
            callback.onSuccess(result);
        } catch (Exception e) {
            callback.onError(e.getMessage());
        }
    }

    // 이벤트
    public void addListener(IPluginEventListener listener) {
        this.listener = listener;
    }

    public void removeListener(IPluginEventListener listener) {
        this.listener = null;
    }
}
```

### iOS

iOS는 C 함수 규약 기반. P/Invoke extern으로 자동 생성됨.

```objc
// 동기
const char* MyPlugin_getName() {
    return strdup("hello");
}

// 비동기
typedef void (*BridgeCallback)(int requestId, const char* result, const char* error);

void MyPlugin_getUserAsync(const char* userId, int requestId, BridgeCallback callback) {
    callback(requestId, result, NULL);
}
```

## 진단 경고

| ID | 설명 |
|---|---|
| PB0001 | `[NativeService]` 클래스에 `[NativeMethod]`가 없음 |
| PB0002 | AndroidClassPath가 비어 있음 |
| PB0003 | `[NativeMethod]`가 partial이 아님 |
| PB0004 | 비동기가 아닌 메서드에 CancellationToken이 있음 |
| PB0005 | `[NativeBridge]` 클래스에 이미 base class가 있어 AndroidJavaProxy와 충돌 |

## 테스트

```bash
dotnet test PolyBridge.Test/PolyBridge.Test.csproj
```
