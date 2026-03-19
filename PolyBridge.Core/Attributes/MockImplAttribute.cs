using System;
using System.Diagnostics;

namespace PolyBridge.Core.Attributes
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method)]
    public class MockImplAttribute : Attribute
    {
        public string MethodName { get; }

        public MockImplAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
