using System;

namespace PolyBridge.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class BridgeResultAttribute : Attribute
    {
        public string MethodName { get; }

        public BridgeResultAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
