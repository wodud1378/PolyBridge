using System;

namespace PolyBridge.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NativeBridgeAttribute : Attribute
    {
        public string AndroidInterfacePath { get; }
        public string EventListenerAdd { get; set; }
        public string EventListenerRemove { get; set; }

        public NativeBridgeAttribute(string androidInterfacePath)
        {
            AndroidInterfacePath = androidInterfacePath;
        }
    }
}
