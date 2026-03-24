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
        public Type Type { get; }

        public SandboxParamInfo(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }

    internal class SandboxEventInfo
    {
        public string Name { get; }
        public EventInfo Event { get; }

        public SandboxEventInfo(string name, EventInfo eventInfo)
        {
            Name = name;
            Event = eventInfo;
        }
    }

    internal class SandboxServiceInfo
    {
        public string DisplayName { get; }
        public Type ServiceType { get; }
        public List<SandboxMethodInfo> Methods { get; }
        public List<SandboxEventInfo> Events { get; }
        public PropertyInfo BridgeProperty { get; }

        public bool HasEvents => Events.Count > 0;

        public SandboxServiceInfo(string displayName, Type serviceType,
            List<SandboxMethodInfo> methods, List<SandboxEventInfo> events, PropertyInfo bridgeProperty)
        {
            DisplayName = displayName;
            ServiceType = serviceType;
            Methods = methods;
            Events = events;
            BridgeProperty = bridgeProperty;
        }
    }
}
