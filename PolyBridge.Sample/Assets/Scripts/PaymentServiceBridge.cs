using PolyBridge.Core.Attributes;

[NativeBridge("com.polybridge.sample.IBridgeCallback")]
internal partial class PaymentServiceBridge
{
    [NativeBridgeResult(nameof(PaymentService.InitializeAsync))]
    [NativeBridgeResult(nameof(PaymentService.PurchaseAsync))]
    public partial void onSuccess(string result);

    [NativeBridgeError(nameof(PaymentService.InitializeAsync))]
    [NativeBridgeError(nameof(PaymentService.PurchaseAsync))]
    public partial void onError(string error);
}
