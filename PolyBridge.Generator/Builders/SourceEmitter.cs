using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace PolyBridge.Generator.Builders
{
    internal class SourceEmitter
    {
        private readonly SourceProductionContext _context;
        private readonly string _namespace;
        private readonly string _outputDir;

        public SourceEmitter(SourceProductionContext context, string ns, string outputDir = null)
        {
            _context = context;
            _namespace = ns;
            _outputDir = outputDir;
        }

        public void Emit(string name, string modifiers,
            bool isInterface = false, string inheritance = null,
            string preprocessorGuard = null, Action<CodeBuilder> body = null)
        {
            var builder = new CodeBuilder();

            if (preprocessorGuard != null)
                builder.AppendPreprocessorIf(preprocessorGuard);

            var nsScope = _namespace != null ? builder.StartNameSpace(_namespace) : null;
            using (nsScope)
            using (isInterface
                ? builder.StartInterface(modifiers, name)
                : builder.StartClass(modifiers, name, inheritance))
            {
                body(builder);
            }

            if (preprocessorGuard != null)
                builder.AppendPreprocessorEndif();

            var code = builder.GenerateFullCode();

            _context.AddSource($"{name}.g.cs",
                SourceText.From(code, Encoding.UTF8));

            WritePhysicalFile($"{name}.g.cs", code);
        }

        private void WritePhysicalFile(string fileName, string content)
        {
            if (_outputDir == null) return;

            try
            {
                if (!Directory.Exists(_outputDir))
                    Directory.CreateDirectory(_outputDir);

                var filePath = Path.Combine(_outputDir, fileName);

                if (File.Exists(filePath) && File.ReadAllText(filePath) == content)
                    return;

                File.WriteAllText(filePath, content);
            }
            catch
            {
                // Silently ignore file I/O errors during generation
            }
        }
    }
}
