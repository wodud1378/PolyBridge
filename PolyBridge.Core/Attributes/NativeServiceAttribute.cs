using System;

namespace PolyBridge.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NativeServiceAttribute : Attribute
    {
        public string AndroidClassPath { get; }
        
        public NativeServiceAttribute(string androidClassPath)
        {
            AndroidClassPath = androidClassPath;
        }
    }
}