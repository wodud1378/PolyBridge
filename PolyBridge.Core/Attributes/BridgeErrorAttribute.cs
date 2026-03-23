using System;

namespace PolyBridge.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class NativeBridgeErrorAttribute : Attribute
    {
        public string MethodName { get; }

        public NativeBridgeErrorAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
