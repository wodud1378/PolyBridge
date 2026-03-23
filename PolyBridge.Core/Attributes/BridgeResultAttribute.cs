using System;

namespace PolyBridge.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class NativeBridgeResultAttribute : Attribute
    {
        public string MethodName { get; }

        public NativeBridgeResultAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
