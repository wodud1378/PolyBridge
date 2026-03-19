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
// 통합 브릿지 — 콜백(BridgeResult/BridgeError) + 이벤트를 하나의 클래스에서 처리
[NativeBridge("com.test.service.IServiceBridge")]
internal partial class TestServiceBridge
{
    // 콜백 — BridgeResult/BridgeError로 비동기 메서드 매핑
    [BridgeResult(nameof(TestService.RequestLoginAsync))]
    [BridgeResult(nameof(TestService.FetchDataAsync))]
    public partial void onSuccess(string result);

    [BridgeResult(nameof(TestService.GetCountAsync))]
    public partial void onCountResult(int count);

    [BridgeError(nameof(TestService.RequestLoginAsync))]
    [BridgeError(nameof(TestService.FetchDataAsync))]
    public partial void onError(string error);

    [BridgeError(nameof(TestService.GetCountAsync))]
    public partial void onCountError();

    // 이벤트 — 어트리뷰트 없음, partial 메서드 자체가 이벤트
    public partial void onStateChanged(string state);
    public partial void onProgress(int current, int total);
}

// 서비스 — BridgeType 하나로 콜백/이벤트 통합
[NativeService("com.test.service.TestPlugin",
    BridgeType = typeof(TestServiceBridge))]
public partial class TestService
{
    [NativeMethod]
    public partial void DoSomething();

    [NativeMethod]
    public partial Task<string> FetchDataAsync(string key);

    [NativeMethod]
    public partial Task<int> GetCountAsync();
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

네이티브에서 C#으로의 통신을 위한 통합 브릿지 클래스. `[NativeBridge]`를 붙이면 클래스 자체가 `AndroidJavaProxy`가 되어 각 partial 메서드가 Java 인터페이스 메서드와 직접 매핑.

하나의 NativeBridge 클래스에서 콜백과 이벤트를 함께 처리:

- **콜백 메서드** — `[BridgeResult(nameof(Method))]` / `[BridgeError(nameof(Method))]` 어트리뷰트가 붙은 메서드. 비동기 서비스 메서드의 성공/실패 결과를 수신
- **이벤트 메서드** — 어트리뷰트가 없는 메서드. partial 메서드 자체가 이벤트로 생성

```csharp
[NativeBridge("com.test.service.IServiceBridge")]
internal partial class TestServiceBridge
{
    // 콜백 — 비동기 메서드별 결과/에러 수신 (AllowMultiple = true)
    [BridgeResult(nameof(TestService.RequestLoginAsync))]
    [BridgeResult(nameof(TestService.FetchDataAsync))]
    [BridgeResult(nameof(TestService.LoadProfileAsync))]
    public partial void onSuccess(string result);

    [BridgeResult(nameof(TestService.GetCountAsync))]
    public partial void onCountResult(int count);

    [BridgeError(nameof(TestService.GetCountAsync))]
    public partial void onCountError();

    [BridgeError(nameof(TestService.RequestLoginAsync))]
    [BridgeError(nameof(TestService.FetchDataAsync))]
    [BridgeError(nameof(TestService.LoadProfileAsync))]
    public partial void onError(string error);

    // 이벤트 — 어트리뷰트 없음, 메서드명이 Java 인터페이스와 1:1 매칭
    public partial void onStateChanged(string state);
    public partial void onProgress(int current, int total);
    public partial void onCompleted();
}

// 서비스에서 BridgeType으로 연결
[NativeService("com.test.service.TestPlugin",
    BridgeType = typeof(TestServiceBridge))]
public partial class TestService { ... }

// 사용 — Bridge 프로퍼티로 이벤트 구독
var service = new TestService();
service.Bridge.OnStateChanged += state => Debug.Log(state);
service.Bridge.OnProgress += (current, total) => Debug.Log($"{current}/{total}");

// 정리
service.Dispose();
```

- 메서드명, 파라미터 수/타입을 자유롭게 정의 — Java 인터페이스와 정확히 일치시키면 됨
- 콜백 메서드는 `[BridgeResult(nameof(Method))]`/`[BridgeError(nameof(Method))]`로 대상 서비스 메서드를 지정 (필수)
- 어트리뷰트가 없는 메서드는 이벤트로 생성
- `AllowMultiple = true` — 하나의 브릿지 메서드가 여러 서비스 메서드를 처리 가능
- 타입 변환: 동일 타입은 직접 전달, `string`에서 다른 타입은 `Parse`/`Deserialize` 자동 적용
- 생성 코드에서 `_nativeBridge` 필드, `Bridge` 프로퍼티, `RegisterBridge` 메서드가 자동 생성

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

**브릿지 인터페이스** (콜백 + 이벤트 통합 -- 메서드명은 사용자 정의, C# NativeBridge partial 메서드와 1:1 매칭):
```java
package com.test.service;

public interface IServiceBridge {
    // 콜백
    void onSuccess(String result);
    void onCountResult(int count);
    void onError(String error);
    void onCountError();

    // 이벤트
    void onStateChanged(String state);
    void onProgress(int current, int total);
    void onCompleted();
}
```

**브릿지 클래스 예시:**
```java
package com.test.service;

public class TestPlugin {
    private IServiceBridge bridge;

    // 동기
    public String getName() { return "hello"; }

    // 비동기
    public void fetchDataAsync(String key, IServiceBridge callback) {
        try {
            String result = api.fetch(key);
            callback.onSuccess(result);
        } catch (Exception e) {
            callback.onError(e.getMessage());
        }
    }

    // 이벤트
    public void addListener(IServiceBridge listener) {
        this.bridge = listener;
    }

    public void removeListener(IServiceBridge listener) {
        this.bridge = null;
    }
}
```

### iOS (실험적)

> iOS는 P/Invoke 기반 코드 생성을 지원하지만, 테스트 기기 부재로 실기기 검증이 완료되지 않은 상태입니다. 동기/비동기 메서드의 기본 코드 생성은 동작하나, `NativeBridge` 이벤트 연동 등 일부 기능은 Android와 동일한 수준의 안정성을 보장하지 않습니다. 추후 안정화 예정.

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

## 빌드

### 제너레이터 빌드

```bash
# 테스트 실행
dotnet test PolyBridge.Test/PolyBridge.Test.csproj

# 제너레이터 DLL 빌드 + Unity 플러그인 폴더에 복사
bash Scripts/build-generator.sh
```

빌드 결과물은 `PolyBridge.Core/Plugins/PolyBridge.Generator.dll`에 출력됨.
Unity Inspector에서 해당 DLL을 선택하고:
1. `RoslynAnalyzer` 라벨 추가
2. 모든 플랫폼 체크 해제

### Unity 프로젝트에 적용

1. `PolyBridge.Core/` 패키지 import
2. `PolyBridge.Core/Plugins/PolyBridge.Generator.dll`에 `RoslynAnalyzer` 라벨 설정
3. `[NativeService]`, `[NativeBridge]` 어트리뷰트로 서비스 선언
4. Unity가 자동으로 소스 제너레이터를 실행하여 코드 생성

## 진단 경고

| ID | 설명 |
|---|---|
| PB0001 | `[NativeService]` 클래스에 `[NativeMethod]`가 없음 |
| PB0002 | AndroidClassPath가 비어 있음 |
| PB0003 | `[NativeMethod]`가 partial이 아님 |
| PB0004 | 비동기가 아닌 메서드에 CancellationToken이 있음 |
| PB0005 | `[NativeBridge]` 클래스에 이미 base class가 있어 AndroidJavaProxy와 충돌 |
| PB0006 | 비동기 메서드가 있지만 BridgeType이 지정되지 않음 |

## 테스트

```bash
dotnet test PolyBridge.Test/PolyBridge.Test.csproj
```
