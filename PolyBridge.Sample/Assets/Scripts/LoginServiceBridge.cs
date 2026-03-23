using PolyBridge.Core.Attributes;

[NativeBridge("com.polybridge.sample.IBridgeCallback")]
internal partial class LoginServiceBridge
{
    [NativeBridgeResult(nameof(LoginService.InitializeAsync))]
    [NativeBridgeResult(nameof(LoginService.LoginAsync))]
    public partial void onSuccess(string result);

    [NativeBridgeError(nameof(LoginService.InitializeAsync))]
    [NativeBridgeError(nameof(LoginService.LoginAsync))]
    public partial void onError(string error);
}
