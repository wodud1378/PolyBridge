# PolyBridge.Generator

Roslyn `IIncrementalGenerator` 기반 소스 제너레이터. `[NativeService]` 클래스를 분석하여 Android/iOS 플랫폼별 구현 코드를 자동 생성.

## 디렉토리

```
Models/       데이터 모델 (ServiceModel, MethodModel, ParameterModel, IAsyncType)
Generators/   플랫폼별 코드 생성기 (AndroidGenerator, IOSGenerator)
Builders/     코드 빌더 유틸리티 (CodeBuilder, SourceEmitter)
```

## 생성 흐름

1. `[NativeService]` + `partial class` 구문 감지
2. `ServiceModel` / `MethodModel` 추출
3. 서비스당 4개 파일 생성:
   - `I{Name}Impl` — 인터페이스
   - `{Name}.g.cs` — partial 클래스 (플랫폼 분기 + 위임)
   - `{Name}Android` — Android 구현 (`#if UNITY_ANDROID`)
   - `{Name}IOS` — iOS 구현 (`#if UNITY_IOS`)

## MethodModel 핵심 속성

| 속성 | 설명 |
|---|---|
| `AllParameters` | CT 포함 전체 파라미터 (메서드 시그니처용) |
| `NativeParameters` | CT 제외 파라미터 (네이티브 호출용) |
| `NativeParameterExpressions` | 복합 타입에 `Serialize()` 적용된 인자 목록 |
| `HasCancellationToken` | CT 파라미터 존재 여부 |
| `ResultConversion()` | 반환 타입 변환 (기본 타입: Parse, 복합 타입: Deserialize) |
| `ParameterConversion()` | 파라미터 변환 (기본 타입: 직접 전달, 복합 타입: Serialize) |

## 생성 코드 예시

### Android 비동기 + CancellationToken

```csharp
var tcs = new TaskCompletionSource<string>();
var callback = new AndroidBridgeCallback(
    result => { try { tcs.TrySetResult(result); } catch (Exception ex) { tcs.TrySetException(ex); } },
    error => tcs.TrySetException(new Exception(error)));
var ctr = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
_bridge.Call("getUser", userId, callback);
try { return await tcs.Task; }
finally { ctr.Dispose(); }
```

### iOS 비동기 + 복합 타입 파라미터

```csharp
// extern 선언에서 복합 타입은 string으로 변환
[DllImport("__Internal")]
private static extern void SaveUser_Extern(string data, int requestId, CallbackDelegate callback);

// 호출 시 Serialize 적용
SaveUser_Extern(PolyBridgeSerializerRegistry.Serializer.Serialize(data), requestId, IOSBridgeCallback.OnResult);
```

## 플랫폼 생성기 확장

`IPlatformGenerator`구현체를 `PolyBridgeGenerator.Generators` 배열에 추가 시 새 플랫폼 지원 가능하며, 현재는 Android, iOS만 지원.
