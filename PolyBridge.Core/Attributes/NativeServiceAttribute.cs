using System;

namespace PolyBridge.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NativeServiceAttribute : Attribute
    {
        public string AndroidClassPath { get; }
        public Type CallbackBridgeType { get; set; }
        public Type EventBridgeType { get; set; }

        public NativeServiceAttribute(string androidClassPath)
        {
            AndroidClassPath = androidClassPath;
        }
    }
}
