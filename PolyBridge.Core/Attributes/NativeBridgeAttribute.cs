using System;

namespace PolyBridge.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NativeBridgeAttribute : Attribute
    {
        public string AndroidInterfacePath { get; }

        public NativeBridgeAttribute(string androidInterfacePath)
        {
            AndroidInterfacePath = androidInterfacePath;
        }
    }
}
