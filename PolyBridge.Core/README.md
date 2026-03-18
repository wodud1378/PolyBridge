# PolyBridge.Core

Unity 런타임에서 사용되는 핵심 라이브러리. 어트리뷰트, 네이티브 콜백, 직렬화를 포함.

## 디렉토리

```
Attributes/       어트리뷰트 ([NativeService], [NativeMethod], [MockImpl], [MockReturn], [PolyBridgeConfiguration])
Runtime/          네이티브 콜백 처리 (AndroidBridge, AndroidBridgeCallback, IOSBridgeCallback, NativeDispatcher)
Serialization/    직렬화 인터페이스 및 레지스트리
```

## 어트리뷰트

### NativeService

네이티브 브릿지 클래스를 선언. `AndroidClassPath`는 Java 클래스 경로.

```csharp
[NativeService("com.example.MyPlugin")]
public partial class MyPlugin { }
```

### NativeMethod

네이티브 메서드 선언. 플랫폼 별 이름을 지정 가능.

```csharp
[NativeMethod]
public partial void Ping();

[NativeMethod("android_fetch", "ios_fetch")]
public partial Task<string> FetchAsync();
```

### MockImpl / MockReturn

에디터 환경에서 네이티브 호출 대신 사용할 Mock 메서드를 지정.

- `[MockImpl]` — void 메서드용. EditorImpl에서 해당 메서드를 호출.
- `[MockReturn]` — 반환값이 있는 메서드용. EditorImpl에서 반환값을 사용.
- Mock 어트리뷰트가 없는 메서드는 `default` 값으로 폴백.

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

## 직렬화

`IPolyBridgeSerializer` 커스텀 직렬화 지원.

```csharp
// 기본값: JsonUtility (Unity 환경)
// 교체 예시
PolyBridgeSerializerRegistry.Serializer = new MyCustomSerializer();
```

## 런타임

- **NativeDispatcher**: 네이티브 콜백을 메인 스레드로 전달하는 `SynchronizationContext` 관리
- **AndroidBridge**: `AndroidJavaObject` 래퍼. 동기/비동기 Java 메서드 호출
- **AndroidBridgeCallback**: `AndroidJavaProxy` 구현. Java → C# 비동기 콜백
- **IOSBridgeCallback**: iOS P/Invoke 비동기 콜백 관리. `Register`/`Unregister`/`OnResult`
