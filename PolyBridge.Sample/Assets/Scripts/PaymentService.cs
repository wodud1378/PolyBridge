using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PolyBridge.Core.Attributes;
using PolyBridge.Sandbox;

[NativeService("com.polybridge.sample.payment.PaymentService",
    BridgeType = typeof(PaymentServiceBridge))]
[Sandbox("Payment Service")]
public partial class PaymentService
{
    // 동기 — 반환값
    [NativeMethod("isInitialized")]
    [SandboxMethod("Is Initialized")]
    public partial bool IsInitialized();

    [NativeMethod("getServiceName")]
    [SandboxMethod("Get Service Name")]
    public partial string GetServiceName();

    // 비동기 — UniTask void
    [NativeMethod("initialize")]
    [SandboxMethod("Initialize")]
    public partial UniTask InitializeAsync();

    // 비동기 — UniTask 복합 타입 반환
    [NativeMethod("purchase")]
    [SandboxMethod("Purchase")]
    public partial UniTask<PaymentResult> PurchaseAsync(string productId, int amount);

    // 비동기 — string 반환
    [NativeMethod("getReceipt")]
    [SandboxMethod("Get Receipt")]
    public partial Task<string> GetReceiptAsync(string transactionId);

    // Editor Mock
    [MockReturn(nameof(IsInitialized))]
    internal bool MockReturnIsInitialized() => true;

    [MockReturn(nameof(GetServiceName))]
    internal string MockReturnGetServiceName() => "PaymentService-mock";

    [MockReturn(nameof(InitializeAsync))]
    internal UniTask MockReturnInitializeAsync() => UniTask.CompletedTask;

    [MockReturn(nameof(PurchaseAsync))]
    internal UniTask<PaymentResult> MockReturnPurchaseAsync() => UniTask.FromResult(new PaymentResult
    {
        transactionId = "mock_txn_001",
        productId = "item_001",
        amount = 1000,
        currency = "KRW",
        status = "mock_completed"
    });

    [MockReturn(nameof(GetReceiptAsync))]
    internal Task<string> MockReturnGetReceiptAsync() => Task.FromResult("{\"transactionId\":\"mock_txn\",\"receipt\":\"mock_receipt\"}");
}
