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
    [NativeService(""com.test.MyPlugin"")]
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
    [NativeService(""com.test.MyPlugin"")]
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
    [NativeService(""com.test.MyPlugin"")]
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
    [NativeService(""com.test.MyPlugin"")]
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

            var interfaceSrc = GeneratorTestHelper.FindGeneratedSource(trees, "IMyPluginBridge");
            Assert.NotNull(interfaceSrc);
            Assert.Contains("interface IMyPluginBridge", interfaceSrc);
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

            var interfaceSrc = GeneratorTestHelper.FindGeneratedSource(trees, "IMyPluginBridge");
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

            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroid");
            Assert.NotNull(androidSrc);
            Assert.Contains("UNITY_ANDROID", androidSrc);
            Assert.Contains("class MyPluginAndroid", androidSrc);
            Assert.Contains("IMyPluginBridge", androidSrc);

            var iosSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOS");
            Assert.NotNull(iosSrc);
            Assert.Contains("UNITY_IOS", iosSrc);
            Assert.Contains("class MyPluginIOS", iosSrc);
            Assert.Contains("IMyPluginBridge", iosSrc);
        }

        [Fact]
        public void Android_SyncVoid_CallsBridge()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroid");
            Assert.NotNull(src);
            Assert.Contains("_bridge.Call(\"DoSomething\")", src);
        }

        [Fact]
        public void Android_SyncReturn_CallsBridgeGeneric()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncReturnSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroid");
            Assert.NotNull(src);
            Assert.Contains("_bridge.Call<int>(\"GetValue\")", src);
        }

        [Fact]
        public void Android_AsyncTaskVoid_UsesTCS()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncTaskVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroid");
            Assert.NotNull(src);
            Assert.Contains("TaskCompletionSource<bool>", src);
            Assert.Contains("AndroidBridgeCallback", src);
            Assert.Contains("_bridge.Call(\"LoginAsync\"", src);
        }

        [Fact]
        public void Android_AsyncTaskReturn_UsesTCSWithConversion()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncTaskReturnSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroid");
            Assert.NotNull(src);
            Assert.Contains("TaskCompletionSource<string>", src);
            Assert.Contains("TrySetResult(result)", src);
        }

        [Fact]
        public void IOS_SyncVoid_UsesExtern()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOS");
            Assert.NotNull(src);
            Assert.Contains("DllImport(\"__Internal\")", src);
            Assert.Contains("static extern void DoSomething()", src);
        }

        [Fact]
        public void IOS_SyncReturn_UsesExternWithReturnType()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncReturnSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOS");
            Assert.NotNull(src);
            Assert.Contains("static extern int GetValue()", src);
        }

        [Fact]
        public void IOS_AsyncTaskVoid_UsesCallbackPattern()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncTaskVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOS");
            Assert.NotNull(src);
            Assert.Contains("IOSBridgeCallback.Register", src);
            Assert.Contains("IOSBridgeCallback.OnResult", src);
            Assert.Contains("requestId", src);
        }

        [Fact]
        public void IOS_AsyncExtern_HasCallbackParams()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(AsyncTaskVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOS");
            Assert.NotNull(src);
            Assert.Contains("int requestId", src);
            Assert.Contains("CallbackDelegate callback", src);
        }

        [Fact]
        public void CustomNativeNames_UsedInGeneration()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(CustomNativeNamesSource);

            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroid");
            Assert.NotNull(androidSrc);
            Assert.Contains("android_fire", androidSrc);

            var iosSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOS");
            Assert.NotNull(iosSrc);
            Assert.Contains("ios_fire", iosSrc);
        }

        [Fact]
        public void Parameters_PassedCorrectly()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(WithParametersSource);

            var interfaceSrc = GeneratorTestHelper.FindGeneratedSource(trees, "IMyPluginBridge");
            Assert.NotNull(interfaceSrc);
            Assert.Contains("string message", interfaceSrc);
            Assert.Contains("int count", interfaceSrc);

            var androidSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroid");
            Assert.NotNull(androidSrc);
            Assert.Contains("message, count", androidSrc);
        }

        [Fact]
        public void Constructor_HasPlatformIfElif()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var partialSrc = GeneratorTestHelper.FindGeneratedSource(trees, "MyPlugin.g.cs");
            Assert.NotNull(partialSrc);
            Assert.Contains("#if UNITY_ANDROID", partialSrc);
            Assert.Contains("#elif UNITY_IOS", partialSrc);
            Assert.Contains("#endif", partialSrc);
            Assert.Contains("new MyPluginAndroid()", partialSrc);
            Assert.Contains("new MyPluginIOS()", partialSrc);
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
            Assert.Contains("private readonly IMyPluginBridge _impl", src);
        }

        [Fact]
        public void Android_Constructor_CreatesAndroidBridge()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroid");
            Assert.NotNull(src);
            Assert.Contains("new PolyBridge.Core.Runtime.AndroidBridge(\"com.test.MyPlugin\")", src);
        }

        [Fact]
        public void IOS_Constructor_HasComment()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(SyncVoidSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOS");
            Assert.NotNull(src);
            Assert.Contains("iOS does not require explicit object instantiation", src);
        }

        [Fact]
        public void AsyncWithParams_Android_PassesParamsBeforeCallback()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(WithParametersSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginAndroid");
            Assert.NotNull(src);
            Assert.Contains("id, force, callback", src);
        }

        [Fact]
        public void AsyncWithParams_IOS_PassesParamsBeforeRequestId()
        {
            var (trees, _) = GeneratorTestHelper.RunGenerator(WithParametersSource);
            var src = GeneratorTestHelper.FindGeneratedSource(trees, "MyPluginIOS");
            Assert.NotNull(src);
            Assert.Contains("id, force, requestId", src);
        }
    }
}
