using System;

namespace PolyBridge.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class PolyBridgeConfigurationAttribute : Attribute
    {
        public bool EmitPhysicalFiles { get; set; } = false;
    }
}