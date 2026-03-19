using System;

namespace PolyBridge.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class BridgeErrorAttribute : Attribute
    {
        public string MethodName { get; }

        public BridgeErrorAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
