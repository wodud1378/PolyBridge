using System;

namespace PolyBridge.Sandbox
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SandboxParamAttribute : Attribute
    {
        public string Name { get; }
        public string DefaultValue { get; }

        public SandboxParamAttribute(string name, string defaultValue = "")
        {
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}
