using System;
using System.Collections.Generic;
using System.Reflection;

namespace PolyBridge.Sandbox
{
    internal class SandboxMethodInfo
    {
        public string Label { get; }
        public MethodInfo Method { get; }
        public List<SandboxParamInfo> Params { get; }
        public bool IsAsync { get; }

        public SandboxMethodInfo(string label, MethodInfo method, List<SandboxParamInfo> parameters, bool isAsync)
        {
            Label = label;
            Method = method;
            Params = parameters;
            IsAsync = isAsync;
        }
    }

    internal class SandboxParamInfo
    {
        public string Name { get; }
        public string DefaultValue { get; }
        public Type Type { get; }

        public SandboxParamInfo(string name, string defaultValue, Type type)
        {
            Name = name;
            DefaultValue = defaultValue;
            Type = type;
        }
    }

    internal class SandboxServiceInfo
    {
        public string DisplayName { get; }
        public Type ServiceType { get; }
        public List<SandboxMethodInfo> Methods { get; }

        public SandboxServiceInfo(string displayName, Type serviceType, List<SandboxMethodInfo> methods)
        {
            DisplayName = displayName;
            ServiceType = serviceType;
            Methods = methods;
        }
    }
}
