using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace PolyBridge.Sample2
{
    [NativeService("com.sample.payment.PaymentBridge")]
    public partial class SamplePaymentService
    {
        // Sync, void, no params
        [NativeMethod("initialize")]
        public partial void Initialize();

        // Sync, return value
        [NativeMethod("isAvailable")]
        public partial bool IsAvailable();

        // Sync, with params
        [NativeMethod("setUserId")]
        public partial void SetUserId(string userId);

        // Async, void return
        [NativeMethod("requestLogin")]
        public partial Task RequestLogin();

        // Async, with return value
        [NativeMethod("purchase")]
        public partial Task<string> Purchase(string productId, int quantity);

        // Different native names per platform
        [NativeMethod("androidGetReceipt", "iosGetReceipt")]
        public partial Task<string> GetReceipt(string transactionId);
    }
}
