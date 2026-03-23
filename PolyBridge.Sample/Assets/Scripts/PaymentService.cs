using System.Threading.Tasks;
using PolyBridge.Core.Attributes;
using PolyBridge.Sandbox;

[NativeService("com.polybridge.sample.payment.PaymentService",
    BridgeType = typeof(PaymentServiceBridge))]
[Sandbox("Payment Service")]
public partial class PaymentService
{
    [NativeMethod("initialize")]
    [SandboxButton("Initialize")]
    public partial Task InitializeAsync();

    [NativeMethod("purchase")]
    [SandboxButton("Purchase")]
    [SandboxParam("productId", "item_001")]
    [SandboxParam("amount", "1000")]
    public partial Task<PaymentResult> PurchaseAsync(string productId, int amount);

    // Editor Mock
    [MockReturn(nameof(InitializeAsync))]
    internal Task MockReturnInitializeAsync() => Task.CompletedTask;

    [MockReturn(nameof(PurchaseAsync))]
    internal Task<PaymentResult> MockReturnPurchaseAsync() => Task.FromResult(new PaymentResult
    {
        transactionId = "mock_txn_001",
        productId = "item_001",
        amount = 1000,
        currency = "KRW",
        status = "mock_completed"
    });
}
