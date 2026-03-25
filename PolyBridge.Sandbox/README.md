# PolyBridge.Sandbox

네이티브 브릿지 메서드를 런타임에서 테스트할 수 있는 디버그 UI 도구. UIToolkit 기반으로 에디터 Play Mode와 빌드에서 동일하게 동작.

## 구조

```
PolyBridge.Sandbox/
├── Runtime/
│   ├── Attributes/
│   │   ├── SandboxAttribute.cs              [Sandbox("이름")] -- 클래스 대상
│   │   └── SandboxMethodAttribute.cs        [SandboxMethod("라벨")] -- 메서드 대상
│   ├── Config/
│   │   ├── SandboxConfig.cs                 ScriptableObject (OnEnable + Preloaded Assets로 로드)
│   │   └── SandboxGesture.cs               ISandboxGesture 인터페이스 + KeyboardShortcutGesture, MultiTouchGesture
│   ├── Gesture/
│   │   ├── ISandboxGestureDetector.cs      제스처 감지 인터페이스
│   │   ├── KeyboardGestureDetector.cs      키보드 단축키 감지
│   │   ├── MultiTouchGestureDetector.cs    멀티 터치 감지
│   │   └── SandboxInput.cs                 ISandboxInput + InputSystemInput (ENABLE_INPUT_SYSTEM), LegacyInput
│   ├── Console/
│   │   ├── SandboxLogEntry.cs              로그 엔트리 모델 (SandboxLogLevel: Info, Warning, Error, Native)
│   │   ├── SandboxConsole.cs               콘솔 UI (I/W/E/N 필터, 검색, 3상태: Minimized/Medium/Maximized)
│   │   └── Native/
│   │       ├── INativeLogReader.cs         네이티브 로그 리더 인터페이스
│   │       ├── AndroidLogReader.cs         Android logcat 리더
│   │       └── NativeLogReaderFactory.cs   플랫폼별 로그 리더 팩토리
│   ├── SandboxInitializer.cs               [RuntimeInitializeOnLoadMethod] 자동 부트
│   ├── SandboxRunner.cs                    제스처 감지 → UI 토글 + 정적 API (ShowSandbox/HideSandbox/ToggleSandbox)
│   ├── PolyBridgeSandbox.cs               UIToolkit UI 빌드 (서비스 탭 → Methods|Events 서브 탭 → 버튼/결과)
│   ├── SandboxScanner.cs                   리플렉션으로 [Sandbox] 서비스 스캔
│   ├── SandboxMethodInvoker.cs             메서드 호출 + 결과 반환
│   └── SandboxMethodInfo.cs                내부 모델
├── Editor/
│   ├── SandboxEditorMenu.cs                Window > PolyBridge > Sandbox 메뉴
│   ├── SandboxEditorWindow.cs              UIToolkit 에디터 윈도우 (Config/PanelSettings ObjectField + Create, 제스처 목록 관리)
│   └── SandboxConfigInspector.cs           커스텀 인스펙터
└── Resources/
    ├── PolyBridgeSandbox.uxml              런타임 UI 템플릿
    └── PolyBridgeSandbox.uss               런타임 UI 스타일
```

## 어트리뷰트

서비스 클래스에 다음 어트리뷰트를 추가하면 Sandbox UI가 자동 구성됨:

| 어트리뷰트 | 대상 | 설명 |
|---|---|---|
| `[Sandbox("이름")]` | 클래스 | Sandbox UI에 서비스 탭으로 표시 |
| `[SandboxMethod("라벨")]` | 메서드 | 호출 버튼 생성 |

```csharp
using PolyBridge.Sandbox;

[Sandbox("Payment Service")]
public partial class PaymentService
{
    [SandboxMethod("Initialize")]
    public partial Task InitializeAsync();

    [SandboxMethod("Purchase")]
    public partial Task<PaymentResult> PurchaseAsync(string productId, int amount);
}
```

## 설정

### 에디터 윈도우

`Window > PolyBridge > Sandbox` 메뉴로 UIToolkit 기반 에디터 윈도우를 열고:

- **Configuration** -- Config/PanelSettings 에셋을 ObjectField로 할당하거나 Create 버튼으로 생성
- **Settings** -- 자동 초기화 여부, 제스처 목록 관리 (추가/삭제)

Config 생성 시 에셋 저장 위치를 자유롭게 선택 가능. `Resources` 폴더에 둘 필요 없음 -- Preloaded Assets에 자동 등록되어 런타임에서 로드됨.

### SandboxConfig

`ScriptableObject` 에셋. `OnEnable`에서 정적 참조를 설정하고, Preloaded Assets를 통해 런타임에서 자동 로드.

| 필드 | 설명 |
|---|---|
| `autoInitialize` | Play Mode / 앱 시작 시 자동 초기화 |
| `panelSettings` | UIToolkit 런타임 렌더링에 필요한 PanelSettings |
| `gestures` | 등록된 제스처 목록 (복수 등록 가능) |

Config 생성 시 PanelSettings도 같은 위치에 자동 생성. PanelSettings 기본값: ScaleWithScreenSize, MatchWidthOrHeight, 기준 해상도 1920x1080, match=1.

Config를 Inspector에서 선택하면 "Open Editor Window" 버튼으로 에디터 윈도우에 접근 가능.

### 제스처

여러 제스처를 동시에 등록 가능. `ISandboxGesture` 인터페이스를 구현하며, 어떤 환경에서든 동작하도록 키보드 + 터치를 함께 등록하는 것을 권장.

| 제스처 | 클래스 | 설명 | 설정 |
|---|---|---|---|
| **Keyboard Shortcut** | `KeyboardShortcutGesture` | 키보드 단축키 | Key, Require Shift |
| **Multi Touch** | `MultiTouchGesture` | N손가락 탭 | Touches (2~5) |

에디터 윈도우에서 `+` 버튼으로 추가, `-` 버튼으로 삭제.

### 입력 시스템 호환성

입력 추상화: `ISandboxInput` 인터페이스를 통해 Legacy Input Manager와 새로운 Input System 패키지 모두 지원.

| 입력 시스템 | 구현 클래스 | 활성 조건 | 키보드 지원 | 터치 지원 |
|---|---|---|---|---|
| **Legacy Input Manager** | `LegacyInput` | 기본 | 모든 KeyCode 지원 | 지원 |
| **Input System** | `InputSystemInput` | `ENABLE_INPUT_SYSTEM` 정의 시 | 주요 키만 지원 (알파벳, 숫자, F키, 특수키 일부) | 지원 |

Input System 사용 시 일부 특수 키가 매핑되지 않을 수 있음. 기본 단축키(BackQuote + Shift)는 양쪽 모두 지원.

**Input System에서 지원되는 키:**
- `A`~`Z` (전체)
- `Alpha0`~`Alpha9` (전체)
- `F1`~`F12` (전체)
- `BackQuote`, `Escape`, `Space`, `Tab`, `Return`, `Backspace`, `Delete`
- `LeftShift`, `RightShift`, `LeftControl`, `RightControl`, `LeftAlt`, `RightAlt`

## 동작 흐름

```
앱 시작 / Play Mode 진입
    ↓
SandboxConfig가 Preloaded Assets에 등록되어 있으면 OnEnable → 정적 참조 설정
    ↓
SandboxConfig.autoInitialize == true?
    ↓ Yes
[RuntimeInitializeOnLoadMethod]
→ SandboxRunner 생성 (DontDestroyOnLoad)
→ 제스처 디텍터 등록
    ↓
사용자가 제스처 입력 (Shift+` 또는 3손가락 탭)
    ↓
Sandbox UI 토글 (표시/숨김)
→ [Sandbox] 서비스 스캔
→ 서비스별 탭 → Methods|Events 서브 탭
→ Methods: 메서드별 버튼 + 파라미터 입력 + 결과 표시
→ Events: 브릿지 이벤트 구독 → 이벤트명별 실시간 로그 표시
```

## 정적 API

`autoInitialize = false`일 때 또는 코드에서 직접 제어할 때:

```csharp
SandboxRunner.ShowSandbox();
SandboxRunner.HideSandbox();
SandboxRunner.ToggleSandbox();
```

## 런타임 UI

- **서브 탭** -- 각 서비스 내 `Methods` | `Events` 탭
- **Methods 탭** -- `[SandboxMethod]`이 붙은 메서드마다 버튼 생성. 비동기 메서드는 `(async)` 표시. 파라미터 입력 필드와 호출 결과/에러 메시지 표시
- **Events 탭** -- 브릿지 이벤트를 구독하여 이벤트명별 실시간 로그를 표시
- **콘솔** -- I(Info)/W(Warning)/E(Error)/N(Native) 필터 버튼, 검색 기능, 3단계 상태 전환 (Minimized/Medium/Maximized). 로그 항목 탭 시 클립보드에 복사
- **네이티브 로그** -- Android에서 logcat을 읽어 콘솔에 표시. 2초 간격으로 현재 프로세스 로그를 자동 수집
- **디버그 로깅** -- 초기화, 서비스 스캔, 인스턴스 생성, 메서드 호출, 이벤트 구독 등 주요 동작마다 `[PolyBridge Sandbox]` 프리픽스로 Unity 콘솔에 로그 출력. 콘솔 Medium/Maximized 상태에서 Sandbox UI 안에서도 확인 가능

에디터에서는 Mock 결과, Android에서는 실제 네이티브 결과가 표시됨.
