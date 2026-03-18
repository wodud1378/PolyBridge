using System;

namespace PolyBridge.Core.Attributes
{
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
