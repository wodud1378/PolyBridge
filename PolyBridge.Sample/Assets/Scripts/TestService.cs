using System.Threading.Tasks;
using PolyBridge.Core.Attributes;

[NativeService("com.test.service")]
public partial class TestService
{
    [NativeMethod("doSomething1")]
    public partial void DoSomething1();
    
    [NativeMethod("doSomething2")]
    public partial Task<int> DoSomething2();
}
