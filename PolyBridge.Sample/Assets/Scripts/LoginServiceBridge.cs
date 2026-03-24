using PolyBridge.Core.Attributes;

[NativeBridge("com.polybridge.sample.login.ILoginBridge",
    EventListenerAdd = "setSessionListener",
    EventListenerRemove = "removeSessionListener")]
internal partial class LoginServiceBridge
{
    // Callback
    [NativeBridgeResult(nameof(LoginService.InitializeAsync))]
    [NativeBridgeResult(nameof(LoginService.LoginAsync))]
    [NativeBridgeResult(nameof(LoginService.LogoutAsync))]
    public partial void onSuccess(string result);

    [NativeBridgeError(nameof(LoginService.InitializeAsync))]
    [NativeBridgeError(nameof(LoginService.LoginAsync))]
    [NativeBridgeError(nameof(LoginService.LogoutAsync))]
    public partial void onError(string error);

    // Event
    public partial void onSessionStateChanged(string state);
}
