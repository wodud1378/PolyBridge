using System.Threading.Tasks;
using PolyBridge.Core.Attributes;
using PolyBridge.Sandbox;

[NativeService("com.polybridge.sample.login.LoginService",
    BridgeType = typeof(LoginServiceBridge))]
[Sandbox("Login Service")]
public partial class LoginService
{
    // 동기
    [NativeMethod("isLoggedIn")]
    [SandboxMethod("Is Logged In")]
    public partial bool IsLoggedIn();

    [NativeMethod("getCurrentUser")]
    [SandboxMethod("Get Current User")]
    public partial string GetCurrentUser();

    // 비동기 — void
    [NativeMethod("initialize")]
    [SandboxMethod("Initialize")]
    public partial Task InitializeAsync();

    // 비동기 — 복합 타입
    [NativeMethod("login")]
    [SandboxMethod("Login")]
    public partial Task<LoginResult> LoginAsync(string username, string password);

    // 비동기 — void
    [NativeMethod("logout")]
    [SandboxMethod("Logout")]
    public partial Task LogoutAsync();

    // Editor Mock
    [MockReturn(nameof(IsLoggedIn))]
    internal bool MockReturnIsLoggedIn() => false;

    [MockReturn(nameof(GetCurrentUser))]
    internal string MockReturnGetCurrentUser() => "";

    [MockReturn(nameof(InitializeAsync))]
    internal Task MockReturnInitializeAsync() => Task.CompletedTask;

    [MockReturn(nameof(LoginAsync))]
    internal Task<LoginResult> MockReturnLoginAsync() => Task.FromResult(new LoginResult
    {
        userId = "mock_user_001",
        displayName = "testuser",
        token = "mock_token_abc",
        expiresIn = 3600
    });

    [MockReturn(nameof(LogoutAsync))]
    internal Task MockReturnLogoutAsync() => Task.CompletedTask;
}
