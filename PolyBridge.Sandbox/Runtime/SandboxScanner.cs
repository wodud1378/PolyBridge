using System;
using System.Collections.Generic;
using System.Linq;
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
                var buttonAttr = method.GetCustomAttribute<SandboxButtonAttribute>();
                if (buttonAttr == null) continue;

                var label = buttonAttr.Label ?? method.Name;
                var isAsync = typeof(Task).IsAssignableFrom(method.ReturnType);

                var paramAttrs = method.GetCustomAttributes<SandboxParamAttribute>().ToList();
                var methodParams = method.GetParameters();
                var parameters = new List<SandboxParamInfo>();

                foreach (var p in methodParams)
                {
                    // Skip CancellationToken
                    if (p.ParameterType == typeof(System.Threading.CancellationToken))
                        continue;

                    var paramAttr = paramAttrs.FirstOrDefault(a => a.Name == p.Name);
                    parameters.Add(new SandboxParamInfo(
                        p.Name,
                        paramAttr?.DefaultValue ?? "",
                        p.ParameterType));
                }

                methods.Add(new SandboxMethodInfo(label, method, parameters, isAsync));
            }

            return methods;
        }
    }
}
