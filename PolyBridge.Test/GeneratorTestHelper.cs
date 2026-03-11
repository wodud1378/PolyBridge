using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PolyBridge.Generator;

namespace PolyBridge.Test
{
    internal static class GeneratorTestHelper
    {
        private static readonly Lazy<IReadOnlyList<MetadataReference>> SharedReferences = new(BuildReferences);

        private static IReadOnlyList<MetadataReference> BuildReferences()
        {
            var references = new HashSet<string>();

            void AddAssembly(Assembly asm)
            {
                if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.Location))
                    references.Add(asm.Location);
            }

            // Core runtime assemblies
            AddAssembly(typeof(object).Assembly);
            AddAssembly(typeof(Attribute).Assembly);
            AddAssembly(typeof(System.Threading.Tasks.Task).Assembly);
            AddAssembly(typeof(Core.Attributes.NativeServiceAttribute).Assembly);

            // System.Runtime
            var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            var sysRuntime = Path.Combine(runtimeDir, "System.Runtime.dll");
            if (File.Exists(sysRuntime)) references.Add(sysRuntime);

            // Load netstandard assembly through the runtime (handles version unification)
            try
            {
                var netstdAsm = Assembly.Load("netstandard");
                if (!string.IsNullOrEmpty(netstdAsm.Location))
                    references.Add(netstdAsm.Location);
            }
            catch { }

            return references.Select(r => (MetadataReference)MetadataReference.CreateFromFile(r)).ToList();
        }

        internal static (ImmutableArray<SyntaxTree> GeneratedTrees, ImmutableArray<Diagnostic> Diagnostics)
            RunGenerator(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { syntaxTree },
                references: SharedReferences.Value,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new PolyBridgeGenerator();
            var driver = CSharpGeneratorDriver.Create(generator)
                .RunGenerators(compilation);

            var result = driver.GetRunResult();
            return (result.GeneratedTrees, result.Diagnostics);
        }

        internal static string? FindGeneratedSource(ImmutableArray<SyntaxTree> trees, string fileNameContains)
        {
            return trees
                .FirstOrDefault(t => t.FilePath.Contains(fileNameContains))?
                .GetText()
                .ToString();
        }
    }
}
