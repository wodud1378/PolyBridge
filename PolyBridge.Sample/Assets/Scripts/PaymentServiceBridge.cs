using PolyBridge.Core.Attributes;

[NativeBridge("com.polybridge.sample.payment.IPaymentBridge",
    EventListenerAdd = "setEventListener",
    EventListenerRemove = "removeEventListener")]
internal partial class PaymentServiceBridge
{
    // Callback — 모든 비동기 메서드 공유
    [NativeBridgeResult(nameof(PaymentService.InitializeAsync))]
    [NativeBridgeResult(nameof(PaymentService.PurchaseAsync))]
    [NativeBridgeResult(nameof(PaymentService.GetReceiptAsync))]
    public partial void onSuccess(string result);

    [NativeBridgeError(nameof(PaymentService.InitializeAsync))]
    [NativeBridgeError(nameof(PaymentService.PurchaseAsync))]
    [NativeBridgeError(nameof(PaymentService.GetReceiptAsync))]
    public partial void onError(string error);

    // Event
    public partial void onPaymentStateChanged(string state);
    public partial void onReceiptReady(string transactionId, string receipt);
}
