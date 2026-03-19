using System;

namespace PolyBridge.Sandbox
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SandboxButtonAttribute : Attribute
    {
        public string Label { get; }

        public SandboxButtonAttribute(string label = null)
        {
            Label = label;
        }
    }
}
