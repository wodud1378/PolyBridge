using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

[NativeService("com.test.service")]
public partial class TestService
{
    [NativeMethod("doSomething1")]
    public partial void DoSomething1();
    
    [NativeMethod("doSomething2")]
    public partial Task<int> DoSomething2();
    
    [NativeMethod("doSomething3")]
    public partial Task DoSomething3();
    
    [MockImpl(nameof(DoSomething1))]
    internal void MockImplDoSomething1() { }

    [MockReturn(nameof(DoSomething2))]
    internal Task<int> MockReturnValueDoSomething2() => Task.FromResult(42);
}
