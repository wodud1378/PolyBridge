using System;

namespace PolyBridge.Sandbox
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SandboxMethodAttribute : Attribute
    {
        public string Label { get; }

        public SandboxMethodAttribute(string label = null)
        {
            Label = label;
        }
    }
}
