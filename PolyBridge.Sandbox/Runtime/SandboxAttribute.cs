using System;

namespace PolyBridge.Sandbox
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SandboxAttribute : Attribute
    {
        public string DisplayName { get; }

        public SandboxAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
