using System.Threading.Tasks;
using PolyBridge.Core.Attributes;
using PolyBridge.Sandbox;

[NativeService("com.polybridge.sample.login.LoginService",
    BridgeType = typeof(LoginServiceBridge))]
[Sandbox("Login Service")]
public partial class LoginService
{
    [NativeMethod("initialize")]
    [SandboxMethod("Initialize")]
    public partial Task InitializeAsync();

    [NativeMethod("login")]
    [SandboxMethod("Login")]
    public partial Task<LoginResult> LoginAsync(string username, string password);

    // Editor Mock
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
}
