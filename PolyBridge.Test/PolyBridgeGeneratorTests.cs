using System.Linq;
using Xunit;

namespace PolyBridge.Test
{
    public class PolyBridgeGeneratorTests
    {
        private const string SyncVoidSource = @"
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeService(""com.test.MyPlugin"")]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial void DoSomething();
    }
}";

        private const string SyncReturnSource = @"
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeService(""com.test.MyPlugin"")]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial int GetValue();
    }
}";

        private const string AsyncTaskVoidSource = @"
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.ICallback"")]
    public partial class MyPluginCallback
    {
        [BridgeResult(nameof(MyPlugin.LoginAsync))]
        public partial void onSuccess(string result);

        [BridgeError(nameof(MyPlugin.LoginAsync))]
        public partial void onError(string error);
    }

    [NativeService(""com.test.MyPlugin"", BridgeType = typeof(MyPluginCallback))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial Task LoginAsync();
    }
}";

        private const string AsyncTaskReturnSource = @"
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.ICallback"")]
    public partial class MyPluginCallback
    {
        [BridgeResult(nameof(MyPlugin.GetUserNameAsync))]
        public partial void onSuccess(string result);

        [BridgeError(nameof(MyPlugin.GetUserNameAsync))]
        public partial void onError(string error);
    }

    [NativeService(""com.test.MyPlugin"", BridgeType = typeof(MyPluginCallback))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial Task<string> GetUserNameAsync();
    }
}";

        private const string MultipleMethodsSource = @"
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.ICallback"")]
    public partial class MyPluginCallback
    {
        [BridgeResult(nameof(MyPlugin.SendAsync))]
        [BridgeResult(nameof(MyPlugin.FetchAsync))]
        public partial void onSuccess(string result);

        [BridgeError(nameof(MyPlugin.SendAsync))]
        [BridgeError(nameof(MyPlugin.FetchAsync))]
        public partial void onError(string error);
    }

    [NativeService(""com.test.MyPlugin"", BridgeType = typeof(MyPluginCallback))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial void Fire();

        [NativeMethod]
        public partial int GetCount();

        [NativeMethod]
        public partial Task SendAsync();

        [NativeMethod]
        public partial Task<string> FetchAsync();
    }
}";

        private const string CustomNativeNamesSource = @"
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeService(""com.test.MyPlugin"")]
    public partial class MyPlugin
    {
        [NativeMethod(""android_fire"", ""ios_fire"")]
        public partial void Fire();
    }
}";

        private const string WithParametersSource = @"
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.ICallback"")]
    public partial class MyPluginCallback
    {
        [BridgeResult(nameof(MyPlugin.FetchAsync))]
        public partial void onSuccess(string result);

        [BridgeError(nameof(MyPlugin.FetchAsync))]
        public partial void onError(string error);
    }

    [NativeService(""com.test.MyPlugin"", BridgeType = typeof(MyPluginCallback))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial void Send(string message, int count);

        [NativeMethod]
        public partial Task<string> FetchAsync(string id, bool force);
    }
}";

        [Fact]
        public void SyncVoid_GeneratesInterfaceAndPartialClass()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(SyncVoidSource);

            Assert.Empty(diagnostics);

            var interfaceSrc = GeneratorTestHelper.FindGeneratedSource(trees, "IMyPluginImpl");
            Assert.NotNull(interfaceSrc);
            Assert.Contains("interface IMyPluginImpl", interfaceSrc);
            Assert.Contains("void DoSomething()", interfaceSrc);

            var partialSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(partialSrc);
            Assert.Contains("partial class MyPlugin", partialSrc);
            Assert.Contains("_impl.DoSomething()", partialSrc);
        }

        [Fact]
        public void SyncReturn_GeneratesCorrectReturnType()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(SyncReturnSource);

            Assert.Empty(diagnostics);

            var interfaceSrc = GeneratorTestHelper.FindGeneratedSource(trees, "IMyPluginImpl");
            Assert.NotNull(interfaceSrc);
            Assert.Contains("int GetValue()", interfaceSrc);

            var partialSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(partialSrc);
            Assert.Contains("return _impl.GetValue()", partialSrc);
        }

        [Fact]
        public void AsyncTaskVoid_GeneratesAsyncMethod()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(AsyncTaskVoidSource);

            Assert.Empty(diagnostics);

            var partialSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(partialSrc);
            Assert.Contains("async", partialSrc);
            Assert.Contains("await _impl.LoginAsync()", partialSrc);
        }

        [Fact]
        public void AsyncTaskReturn_GeneratesAsyncWithReturn()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(AsyncTaskReturnSource);

            Assert.Empty(diagnostics);

            var partialSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(partialSrc);
            Assert.Contains("return await _impl.GetUserNameAsync()", partialSrc);
        }

        [Fact]
        public void PlatformClasses_GeneratedForAndroidAndIOS()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);

            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(androidSrc);
            Assert.Contains("UNITY_ANDROID", androidSrc);
            Assert.Contains("class MyPluginAndroidImpl", androidSrc);
            Assert.Contains("IMyPluginImpl", androidSrc);

            var iosSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(iosSrc);
            Assert.Contains("UNITY_IOS", iosSrc);
            Assert.Contains("class MyPluginIOSImpl", iosSrc);
            Assert.Contains("IMyPluginImpl", iosSrc);
        }

        [Fact]
        public void Android_SyncVoid_CallsBridge()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(src);
            Assert.Contains("_bridge.Call(\"DoSomething\")", src);
        }

        [Fact]
        public void Android_SyncReturn_CallsBridgeGeneric()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncReturnSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(src);
            Assert.Contains("_bridge.Call<int>(\"GetValue\")", src);
        }

        [Fact]
        public void Android_AsyncTaskVoid_UsesTCS()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncTaskVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(src);
            Assert.Contains("TaskCompletionSource<bool>", src);
            Assert.Contains("MyPluginCallback", src);
            Assert.Contains("_bridge.Call(\"LoginAsync\"", src);
        }

        [Fact]
        public void Android_AsyncTaskReturn_UsesTCSWithConversion()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncTaskReturnSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(src);
            Assert.Contains("TaskCompletionSource<string>", src);
            Assert.Contains("TrySetResult(result)", src);
        }

        [Fact]
        public void IOS_SyncVoid_UsesExtern()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(src);
            Assert.Contains("DllImport(\"__Internal\"", src);
            Assert.Contains("static extern void DoSomething_Extern()", src);
        }

        [Fact]
        public void IOS_SyncReturn_UsesExternWithReturnType()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncReturnSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(src);
            Assert.Contains("static extern int GetValue_Extern()", src);
        }

        [Fact]
        public void IOS_AsyncTaskVoid_UsesCallbackPattern()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncTaskVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(src);
            Assert.Contains("IOSBridgeCallback.Register", src);
            Assert.Contains("IOSBridgeCallback.OnResult", src);
            Assert.Contains("requestId", src);
        }

        [Fact]
        public void IOS_AsyncExtern_HasCallbackParams()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncTaskVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(src);
            Assert.Contains("int requestId", src);
            Assert.Contains("CallbackDelegate callback", src);
        }

        [Fact]
        public void CustomNativeNames_UsedInGeneration()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(CustomNativeNamesSource);

            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(androidSrc);
            Assert.Contains("android_fire", androidSrc);

            var iosSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(iosSrc);
            Assert.Contains("ios_fire", iosSrc);
        }

        [Fact]
        public void Parameters_PassedCorrectly()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(WithParametersSource);

            var interfaceSrc = GeneratorTestHelper.FindGeneratedSource(trees, "IMyPluginImpl");
            Assert.NotNull(interfaceSrc);
            Assert.Contains("string message", interfaceSrc);
            Assert.Contains("int count", interfaceSrc);

            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(androidSrc);
            Assert.Contains("message, count", androidSrc);
        }

        [Fact]
        public void Constructor_HasPlatformIfElif()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var partialSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(partialSrc);
            Assert.Contains("#if UNITY_EDITOR", partialSrc);
            Assert.Contains("new MyPluginEditorImpl(this)", partialSrc);
            Assert.Contains("#elif UNITY_ANDROID", partialSrc);
            Assert.Contains("#elif UNITY_IOS", partialSrc);
            Assert.Contains("#endif", partialSrc);
            Assert.Contains("new MyPluginAndroidImpl()", partialSrc);
            Assert.Contains("new MyPluginIOSImpl()", partialSrc);
        }

        [Fact]
        public void MultipleMethodsSource_GeneratesAll()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(MultipleMethodsSource);
            Assert.Empty(diagnostics);

            var partialSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(partialSrc);
            Assert.Contains("Fire()", partialSrc);
            Assert.Contains("GetCount()", partialSrc);
            Assert.Contains("SendAsync()", partialSrc);
            Assert.Contains("FetchAsync()", partialSrc);
        }

        [Fact]
        public void NoMethods_ReportsPB0001Warning()
        {
            var source = @"
using PolyBridge.Core.Attributes;
namespace TestApp
{
    [NativeService(""com.test.Empty"")]
    public partial class EmptyPlugin
    {
    }
}";
            var (_, diagnostics) = GeneratorTestHelper.RunGenerator(source);
            Assert.Contains(diagnostics, d => d.Id == "PB0001");
        }

        [Fact]
        public void EmptyClassPath_ReportsPB0002Warning()
        {
            var source = @"
using PolyBridge.Core.Attributes;
namespace TestApp
{
    [NativeService("""")]
    public partial class NoPathPlugin
    {
        [NativeMethod]
        public partial void Ping();
    }
}";
            var (_, diagnostics) = GeneratorTestHelper.RunGenerator(source);
            Assert.Contains(diagnostics, d => d.Id == "PB0002");
        }

        [Fact]
        public void NonPartialClass_Ignored()
        {
            var source = @"
using PolyBridge.Core.Attributes;
namespace TestApp
{
    [NativeService(""com.test.Plugin"")]
    public class NotPartial
    {
        [NativeMethod]
        public void Ping() { }
    }
}";
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(source);
            // Non-partial class should be ignored entirely - no generated files for it
            var interfaceSrc = GeneratorTestHelper.FindGeneratedSource(trees, "INotPartialBridge");
            Assert.Null(interfaceSrc);
        }

        [Fact]
        public void GeneratedFiles_HaveAutoGeneratedHeader()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(src);
            Assert.Contains("// <auto-generated />", src);
        }

        [Fact]
        public void ImplField_IsDeclaredInPartialClass()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(src);
            Assert.Contains("private readonly IMyPluginImpl _impl", src);
        }

        [Fact]
        public void Android_Constructor_CreatesAndroidBridge()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(src);
            Assert.Contains("new PolyBridge.Core.Runtime.AndroidBridge(\"com.test.MyPlugin\")", src);
        }

        [Fact]
        public void IOS_Constructor_HasComment()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(src);
            Assert.Contains("iOS does not require explicit object instantiation", src);
        }

        [Fact]
        public void AsyncWithParams_Android_PassesParamsBeforeCallback()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(WithParametersSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(src);
            Assert.Contains("id, force, callback", src);
        }

        [Fact]
        public void AsyncWithParams_IOS_PassesParamsBeforeRequestId()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(WithParametersSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(src);
            Assert.Contains("id, force, requestId", src);
        }

        // --- CancellationToken Tests ---

        private const string AsyncWithCTSource = @"
using System.Threading;
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.ICallback"")]
    public partial class MyPluginCallback
    {
        [BridgeResult(nameof(MyPlugin.GetUserAsync))]
        public partial void onSuccess(string result);

        [BridgeError(nameof(MyPlugin.GetUserAsync))]
        public partial void onError(string error);
    }

    [NativeService(""com.test.MyPlugin"", BridgeType = typeof(MyPluginCallback))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial Task<string> GetUserAsync(string userId, CancellationToken cancellationToken);
    }
}";

        private const string AsyncVoidWithCTSource = @"
using System.Threading;
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.ICallback"")]
    public partial class MyPluginCallback
    {
        [BridgeResult(nameof(MyPlugin.SendAsync))]
        public partial void onSuccess(string result);

        [BridgeError(nameof(MyPlugin.SendAsync))]
        public partial void onError(string error);
    }

    [NativeService(""com.test.MyPlugin"", BridgeType = typeof(MyPluginCallback))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial Task SendAsync(CancellationToken ct);
    }
}";

        private const string SyncWithCTSource = @"
using System.Threading;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeService(""com.test.MyPlugin"")]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial void DoSomething(string message, CancellationToken cancellationToken);
    }
}";

        [Fact]
        public void CT_DetectedInMethodSignature()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(AsyncWithCTSource);
            Assert.Empty(diagnostics);

            // CT should be in the method signature (ParameterDeclarations)
            var partialSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(partialSrc);
            Assert.Contains("CancellationToken cancellationToken", partialSrc);
        }

        [Fact]
        public void CT_ExcludedFromNativeCall_Android()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncWithCTSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(src);
            // Native call should have userId but NOT cancellationToken
            Assert.Contains("userId, callback", src);
            Assert.DoesNotContain("cancellationToken, callback", src);
        }

        [Fact]
        public void CT_RegistersTrySetCanceled_Android()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncWithCTSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(src);
            Assert.Contains("cancellationToken.Register", src);
            Assert.Contains("TrySetCanceled", src);
            Assert.Contains("ctr.Dispose()", src);
        }

        [Fact]
        public void CT_TryFinally_Android()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncWithCTSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(src);
            Assert.Contains("try {", src);
            Assert.Contains("finally {", src);
        }

        [Fact]
        public void CT_ExcludedFromExtern_IOS()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncWithCTSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(src);
            // Extern declaration should have userId, requestId, callback but NOT cancellationToken
            Assert.Contains("string userId, int requestId", src);
            // The extern line should not include cancellationToken
            Assert.DoesNotContain("CancellationToken cancellationToken, int requestId", src);
        }

        [Fact]
        public void CT_RegistersUnregisterAndTrySetCanceled_IOS()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncWithCTSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(src);
            Assert.Contains("cancellationToken.Register", src);
            Assert.Contains("IOSBridgeCallback.Unregister(requestId)", src);
            Assert.Contains("TrySetCanceled", src);
            Assert.Contains("ctr.Dispose()", src);
        }

        [Fact]
        public void CT_VoidAsync_Works()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(AsyncVoidWithCTSource);
            Assert.Empty(diagnostics);

            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(androidSrc);
            Assert.Contains("ct.Register", androidSrc);
            // No native params, so callback comes directly
            Assert.Contains("_bridge.Call(\"SendAsync\", callback)", androidSrc);
        }

        [Fact]
        public void CT_OnSyncMethod_ReportsPB0004()
        {
            var (_, diagnostics) = GeneratorTestHelper.RunGenerator(SyncWithCTSource);
            Assert.Contains(diagnostics, d => d.Id == "PB0004");
        }

        [Fact]
        public void CT_OnSyncMethod_CTExcludedFromNativeCall()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncWithCTSource);
            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(androidSrc);
            // Native call should only contain message, not cancellationToken
            Assert.Contains("\"DoSomething\", message", androidSrc);
            // cancellationToken should not appear in the bridge Call (but it will appear in the method signature)
            Assert.DoesNotContain("\"DoSomething\", message, cancellationToken", androidSrc);
        }

        // --- Custom Serialization Tests ---

        private const string ComplexReturnTypeSource = @"
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    public class UserInfo { public string Name; }

    [NativeBridge(""com.test.ICallback"")]
    public partial class MyPluginCallback
    {
        [BridgeResult(nameof(MyPlugin.GetUserAsync))]
        public partial void onSuccess(string result);

        [BridgeError(nameof(MyPlugin.GetUserAsync))]
        public partial void onError(string error);
    }

    [NativeService(""com.test.MyPlugin"", BridgeType = typeof(MyPluginCallback))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial Task<UserInfo> GetUserAsync();
    }
}";

        private const string ComplexParameterSource = @"
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    public class UserData { public string Name; }

    [NativeBridge(""com.test.ICallback"")]
    public partial class MyPluginCallback
    {
        [BridgeResult(nameof(MyPlugin.SendDataAsync))]
        public partial void onSuccess(string result);

        [BridgeError(nameof(MyPlugin.SendDataAsync))]
        public partial void onError(string error);
    }

    [NativeService(""com.test.MyPlugin"", BridgeType = typeof(MyPluginCallback))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial void SaveUser(UserData data);

        [NativeMethod]
        public partial Task SendDataAsync(string id, UserData data);
    }
}";

        [Fact]
        public void Serialization_ComplexReturn_UsesSerializerRegistry()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(ComplexReturnTypeSource);
            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(androidSrc);
            Assert.Contains("PolyBridgeSerializerRegistry.Serializer.Deserialize<", androidSrc);
            Assert.DoesNotContain("JsonUtility", androidSrc);
        }

        [Fact]
        public void Serialization_PrimitiveReturn_NoSerializerCall()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncTaskReturnSource);
            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(androidSrc);
            // string return should just pass through, no serializer call
            Assert.Contains("TrySetResult(result)", androidSrc);
            Assert.DoesNotContain("PolyBridgeSerializerRegistry", androidSrc);
        }

        [Fact]
        public void Serialization_ComplexParam_UsesSerializerSerialize_Android()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(ComplexParameterSource);
            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(androidSrc);
            Assert.Contains("PolyBridgeSerializerRegistry.Serializer.Serialize(data)", androidSrc);
        }

        [Fact]
        public void Serialization_ComplexParam_UsesSerializerSerialize_IOS()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(ComplexParameterSource);
            var iosSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(iosSrc);
            Assert.Contains("PolyBridgeSerializerRegistry.Serializer.Serialize(data)", iosSrc);
        }

        [Fact]
        public void Serialization_ComplexParam_IOSExtern_UsesStringType()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(ComplexParameterSource);
            var iosSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOSImpl");
            Assert.NotNull(iosSrc);
            // The extern for SaveUser should have "string data" instead of "UserData data"
            Assert.Contains("string data", iosSrc);
        }

        [Fact]
        public void Serialization_PrimitiveParam_PassedDirectly()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(WithParametersSource);
            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(androidSrc);
            // Primitive params should be passed directly, not serialized
            Assert.DoesNotContain("Serialize(message)", androidSrc);
            Assert.DoesNotContain("Serialize(count)", androidSrc);
        }

        // --- EditorImpl Tests ---

        private const string MockImplSource = @"
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.ICallback"")]
    public partial class MyPluginCallback
    {
        [BridgeResult(nameof(MyPlugin.GetValueAsync))]
        public partial void onSuccess(string result);

        [BridgeError(nameof(MyPlugin.GetValueAsync))]
        public partial void onError(string error);
    }

    [NativeService(""com.test.MyPlugin"", BridgeType = typeof(MyPluginCallback))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial void DoSomething();

        [NativeMethod]
        public partial Task<int> GetValueAsync();

        [MockImpl(nameof(DoSomething))]
        internal void MockImplDoSomething() { }

        [MockReturn(nameof(GetValueAsync))]
        internal Task<int> MockReturnGetValueAsync() => Task.FromResult(42);
    }
}";

        private const string NoMockSource = @"
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.ICallback"")]
    public partial class MyPluginCallback
    {
        [BridgeResult(nameof(MyPlugin.SendAsync))]
        [BridgeResult(nameof(MyPlugin.GetCountAsync))]
        public partial void onSuccess(string result);

        [BridgeError(nameof(MyPlugin.SendAsync))]
        [BridgeError(nameof(MyPlugin.GetCountAsync))]
        public partial void onError(string error);
    }

    [NativeService(""com.test.MyPlugin"", BridgeType = typeof(MyPluginCallback))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial void Fire();

        [NativeMethod]
        public partial string GetName();

        [NativeMethod]
        public partial Task SendAsync();

        [NativeMethod]
        public partial Task<int> GetCountAsync();
    }
}";

        [Fact]
        public void EditorImpl_ClassGenerated()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginEditorImpl");
            Assert.NotNull(src);
            Assert.Contains("UNITY_EDITOR", src);
            Assert.Contains("class MyPluginEditorImpl", src);
            Assert.Contains("IMyPluginImpl", src);
        }

        [Fact]
        public void EditorImpl_HasOwnerField()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginEditorImpl");
            Assert.NotNull(src);
            Assert.Contains("private readonly MyPlugin _owner", src);
            Assert.Contains("MyPlugin owner", src);
            Assert.Contains("_owner = owner", src);
        }

        [Fact]
        public void EditorImpl_MockImpl_CallsOwnerMethod()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(MockImplSource);
            Assert.Empty(diagnostics);

            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginEditorImpl");
            Assert.NotNull(src);
            Assert.Contains("_owner.MockImplDoSomething()", src);
        }

        [Fact]
        public void EditorImpl_MockReturn_CallsOwnerMethod()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(MockImplSource);
            Assert.Empty(diagnostics);

            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginEditorImpl");
            Assert.NotNull(src);
            Assert.Contains("return _owner.MockReturnGetValueAsync()", src);
        }

        [Fact]
        public void EditorImpl_NoMock_VoidFallback()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(NoMockSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginEditorImpl");
            Assert.NotNull(src);
            // void method with no mock → empty body (no return statement needed)
            Assert.Contains("void Fire()", src);
        }

        [Fact]
        public void EditorImpl_NoMock_SyncReturnFallback()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(NoMockSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginEditorImpl");
            Assert.NotNull(src);
            Assert.Contains("return default", src);
        }

        [Fact]
        public void EditorImpl_NoMock_TaskVoidFallback()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(NoMockSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginEditorImpl");
            Assert.NotNull(src);
            Assert.Contains("Task.CompletedTask", src);
        }

        [Fact]
        public void EditorImpl_NoMock_TaskTFallback()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(NoMockSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginEditorImpl");
            Assert.NotNull(src);
            Assert.Contains("Task.FromResult<", src);
        }

        // --- NativeBridge Tests ---

        private const string EventBridgeSource = @"
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.IMyEventListener"")]
    public partial class MyEventBridge
    {
        public partial void onStateChanged(string state);
        public partial void onCountUpdated(int count);
        public partial void onReady();
    }
}";

        private const string ServiceWithEventBridgeSource = @"
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.IMyEventListener"")]
    public partial class MyPluginEventBridge
    {
        public partial void onReady();
    }

    [NativeService(""com.test.MyPlugin"", BridgeType = typeof(MyPluginEventBridge))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial void DoSomething();
    }
}";

        [Fact]
        public void EventBridge_DeclaresEvents()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(EventBridgeSource);
            Assert.Empty(diagnostics);

            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyEventBridge.g.cs");
            Assert.NotNull(src);
            Assert.Contains("event System.Action<string> OnStateChanged", src);
            Assert.Contains("event System.Action<int> OnCountUpdated", src);
            Assert.Contains("event System.Action OnReady", src);
        }

        [Fact]
        public void EventBridge_ImplementsPartialMethods()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(EventBridgeSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyEventBridge.g.cs");
            Assert.NotNull(src);
            Assert.Contains("OnStateChanged?.Invoke(state)", src);
            Assert.Contains("OnReady?.Invoke()", src);
            Assert.Contains("NativeDispatcher.Post", src);
        }

        [Fact]
        public void EventBridge_ImplementsIDisposable()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(EventBridgeSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyEventBridge.g.cs");
            Assert.NotNull(src);
            Assert.Contains("System.IDisposable", src);
            Assert.Contains("void Dispose()", src);
        }

        [Fact]
        public void EventBridge_Android_ExtendsAndroidJavaProxy()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(EventBridgeSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyEventBridge.Android");
            Assert.NotNull(src);
            Assert.Contains("UNITY_ANDROID", src);
            Assert.Contains("UnityEngine.AndroidJavaProxy", src);
            Assert.Contains("com.test.IMyEventListener", src);
        }

        [Fact]
        public void ServiceWithEventBridge_HasEventBridgeField()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(ServiceWithEventBridgeSource);
            Assert.Empty(diagnostics);

            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(src);
            Assert.Contains("_nativeBridge", src);
            Assert.Contains("Bridge", src);
            Assert.Contains("System.IDisposable", src);
        }

        [Fact]
        public void ServiceWithEventBridge_ConstructorCreatesAndRegisters()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(ServiceWithEventBridgeSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(src);
            Assert.Contains("new global::TestApp.MyPluginEventBridge()", src);
            Assert.Contains("RegisterBridge", src);
        }

        [Fact]
        public void ServiceWithEventBridge_Android_HasRegistration()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(ServiceWithEventBridgeSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(src);
            Assert.Contains("addListener", src);
            Assert.Contains("RegisterBridge", src);
        }

        [Fact]
        public void ServiceWithEventBridge_Dispose()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(ServiceWithEventBridgeSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(src);
            Assert.Contains("_impl?.Dispose()", src);
            Assert.Contains("_nativeBridge?.Dispose()", src);
        }

        [Fact]
        public void NoEventBridge_NoIDisposable()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(src);
            Assert.DoesNotContain("IDisposable", src);
            Assert.DoesNotContain("Dispose", src);
        }

        // --- AccessModifier Mirroring Tests ---

        private const string PublicMethodSource = @"
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeService(""com.test.MyPlugin"")]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial void DoSomething();
    }
}";

        private const string InternalMethodSource = @"
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeService(""com.test.MyPlugin"")]
    public partial class MyPlugin
    {
        [NativeMethod]
        internal partial void DoSomething();
    }
}";

        private const string PrivateEventBridgeSource = @"
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.IListener"")]
    public partial class MyBridge
    {
        private partial void onReady();
    }
}";

        [Fact]
        public void AccessModifier_Public_Mirrored()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(PublicMethodSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(src);
            Assert.Contains("public partial void DoSomething()", src);
        }

        [Fact]
        public void AccessModifier_Internal_Mirrored()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(InternalMethodSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(src);
            Assert.Contains("internal partial void DoSomething()", src);
        }

        [Fact]
        public void AccessModifier_PrivateEventBridge_Mirrored()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(PrivateEventBridgeSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyBridge.g.cs");
            Assert.NotNull(src);
            Assert.Contains("private partial void onReady()", src);
        }

        [Fact]
        public void AccessModifier_NoModifier_OmittedInEventBridge()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(EventBridgeSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyEventBridge.g.cs");
            Assert.NotNull(src);
            Assert.Contains("public partial void onStateChanged(", src);
        }

        // --- Proxy Generation Tests ---

        [Fact]
        public void Proxy_CallbackBridgeType()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncTaskVoidSource);

            // AndroidImpl uses the callback bridge
            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(androidSrc);
            Assert.Contains("new global::TestApp.MyPluginCallback()", androidSrc);

            // NativeBridge generates the proxy class separately
            var bridgeSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginCallback.g.cs");
            Assert.NotNull(bridgeSrc);
            Assert.Contains("event", bridgeSrc);
            Assert.Contains("onSuccess", bridgeSrc);
            Assert.Contains("onError", bridgeSrc);
        }

        private const string CustomCallbackInterfaceSource = @"
using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

namespace TestApp
{
    [NativeBridge(""com.test.IMyCallback"")]
    public partial class MyCustomCallback
    {
        [BridgeResult(nameof(MyPlugin.FetchAsync))]
        public partial void onSuccess(string result);

        [BridgeError(nameof(MyPlugin.FetchAsync))]
        public partial void onError(string error);
    }

    [NativeService(""com.test.MyPlugin"",
        BridgeType = typeof(MyCustomCallback))]
    public partial class MyPlugin
    {
        [NativeMethod]
        public partial Task<string> FetchAsync();
    }
}";

        [Fact]
        public void Proxy_CustomCallbackInterface()
        {
            var (trees, diagnostics) = GeneratorTestHelper.RunGenerator(CustomCallbackInterfaceSource);
            Assert.Empty(diagnostics);

            // AndroidImpl uses the custom callback bridge
            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(androidSrc);
            Assert.Contains("MyCustomCallback()", androidSrc);

            // NativeBridge generates Android partial with custom interface path
            var bridgeAndroid = GeneratorTestHelper.FindGeneratedSource(trees, "MyCustomCallback.Android");
            Assert.NotNull(bridgeAndroid);
            Assert.Contains("com.test.IMyCallback", bridgeAndroid);
        }

        [Fact]
        public void Proxy_NoProxyForSyncOnlyService()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroidImpl");
            Assert.NotNull(src);
            Assert.DoesNotContain("BridgeCallback", src);
            Assert.DoesNotContain("EventListener", src);
        }
    }
}

