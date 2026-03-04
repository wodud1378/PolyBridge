using System;

namespace PolyBridge.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class NativeMethodAttribute : Attribute
    {
        public string AndroidName { get; }
        public string IOSName { get; }
        
        public NativeMethodAttribute(string androidName = null, string iosName = null)
        {
            AndroidName = androidName;
            IOSName = iosName;
        }
    }
}