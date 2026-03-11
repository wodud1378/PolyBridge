using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace PolyBridge.Generator.Builders
{
    internal class SourceEmitter
    {
        private readonly SourceProductionContext _context;
        private readonly string _namespace;

        public SourceEmitter(SourceProductionContext context, string ns)
        {
            _context = context;
            _namespace = ns;
        }

        public void Emit(string name, string modifiers,
            bool isInterface = false, string inheritance = null,
            string preprocessorGuard = null, Action<CodeBuilder> body = null)
        {
            var builder = new CodeBuilder();

            if (preprocessorGuard != null)
                builder.AppendPreprocessorIf(preprocessorGuard);

            using (builder.StartNameSpace(_namespace))
            using (isInterface
                ? builder.StartInterface(modifiers, name)
                : builder.StartClass(modifiers, name, inheritance))
            {
                body(builder);
            }

            if (preprocessorGuard != null)
                builder.AppendPreprocessorEndif();

            _context.AddSource($"{name}.g.cs",
                SourceText.From(builder.GenerateFullCode(), Encoding.UTF8));
        }
    }
}
