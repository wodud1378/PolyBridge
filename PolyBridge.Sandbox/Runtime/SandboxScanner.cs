using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace PolyBridge.Sandbox
{
    internal static class SandboxScanner
    {
        internal static List<SandboxServiceInfo> ScanAll()
        {
            var result = new List<SandboxServiceInfo>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    var sandboxAttr = type.GetCustomAttribute<SandboxAttribute>();
                    if (sandboxAttr == null) continue;

                    var methods = ScanMethods(type);
                    if (methods.Count > 0)
                        result.Add(new SandboxServiceInfo(sandboxAttr.DisplayName, type, methods));
                }
            }

            return result;
        }

        private static List<SandboxMethodInfo> ScanMethods(Type serviceType)
        {
            var methods = new List<SandboxMethodInfo>();

            foreach (var method in serviceType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attr = method.GetCustomAttribute<SandboxMethodAttribute>();
                if (attr == null) continue;

                var label = attr.Label ?? method.Name;
                var isAsync = typeof(Task).IsAssignableFrom(method.ReturnType);
                var parameters = new List<SandboxParamInfo>();

                foreach (var p in method.GetParameters())
                {
                    if (p.ParameterType == typeof(System.Threading.CancellationToken))
                        continue;
                    parameters.Add(new SandboxParamInfo(p.Name, p.ParameterType));
                }

                methods.Add(new SandboxMethodInfo(label, method, parameters, isAsync));
            }

            return methods;
        }
    }
}
