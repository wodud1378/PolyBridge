# PolyBridge

Unity에서 Android/iOS 네이티브 코드를 호출하기 위한 Roslyn 소스 제너레이터 기반 브릿지 라이브러리.

## 구조

| 프로젝트 | 설명 |
|---|---|
| **PolyBridge.Core** | 런타임 라이브러리 (어트리뷰트, 콜백, 직렬화) |
| **PolyBridge.Generator** | Roslyn 소스 제너레이터 (플랫폼별 코드 자동 생성) |
| **PolyBridge.Test** | 단위 테스트 |

## 샘플 코드

```csharp
[NativeService("com.example.MyPlugin")]
public partial class MyPlugin
{
    [NativeMethod]
    public partial void DoSomething();

    [NativeMethod]
    public partial int GetValue();

    [NativeMethod]
    public partial Task<string> GetUserAsync(string userId);
}
```

빌드 시 `MyPluginAndroid`, `MyPluginIOS` 구현이 자동 생성.

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

### 네이티브 메서드 지정

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

// 사용
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var data = await plugin.LoadDataAsync("key", cts.Token);
```

### 커스텀 직렬화

Non-Blittable 타입은 기본적으로 직렬화/역직렬화. 
기본 Serializer는 `JsonUtility`이며 교체 가능.

```csharp
// 앱 시작 시 한 줄로 교체
PolyBridgeSerializerRegistry.Serializer = new NewtonsoftSerializer();

// IPolyBridgeSerializer 구현
public class NewtonsoftSerializer : IPolyBridgeSerializer
{
    public T Deserialize<T>(string data) => JsonConvert.DeserializeObject<T>(data);
    public string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj);
}
```

## 진단 경고

| ID | 설명 |
|---|---|
| PB0001 | `[NativeService]` 클래스에 `[NativeMethod]`가 없음 |
| PB0002 | AndroidClassPath가 비어 있음 |
| PB0003 | `[NativeMethod]`가 partial이 아님 |
| PB0004 | 비동기가 아닌 메서드에 CancellationToken이 있음 |

## 테스트

```bash
dotnet test PolyBridge.Test/PolyBridge.Test.csproj
```
