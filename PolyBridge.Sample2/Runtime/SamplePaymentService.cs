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
    }
}
