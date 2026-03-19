using System;
using System.Diagnostics;

namespace PolyBridge.Core.Attributes
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method)]
    public class MockReturnAttribute : Attribute
    {
        public string MethodName { get; }

        public MockReturnAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
