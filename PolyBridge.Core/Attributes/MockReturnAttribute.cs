using System;

namespace PolyBridge.Core.Attributes
{
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
