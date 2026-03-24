using PolyBridge.Core.Attributes;

[NativeBridge("com.polybridge.sample.payment.IPaymentBridge",
    EventListenerAdd = "setEventListener",
    EventListenerRemove = "removeEventListener")]
internal partial class PaymentServiceBridge
{
    // Callback
    [NativeBridgeResult(nameof(PaymentService.InitializeAsync))]
    [NativeBridgeResult(nameof(PaymentService.PurchaseAsync))]
    public partial void onSuccess(string result);

    [NativeBridgeError(nameof(PaymentService.InitializeAsync))]
    [NativeBridgeError(nameof(PaymentService.PurchaseAsync))]
    public partial void onError(string error);

    // Event
    public partial void onPaymentStateChanged(string state);
    public partial void onReceiptReady(string transactionId, string receipt);
}
