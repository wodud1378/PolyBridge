using PolyBridge.Generator.Builders;
using Xunit;

namespace PolyBridge.Test
{
    public class CodeBuilderTests
    {
        [Fact]
        public void AppendLine_EmptyLine_AppendsBlankLine()
        {
            var builder = new CodeBuilder();
            builder.AppendLine();
            var code = builder.GenerateFullCode();
            Assert.Contains("\n\n", code);
        }

        [Fact]
        public void AppendLine_WithText_AppendsText()
        {
            var builder = new CodeBuilder();
            builder.AppendLine("hello;");
            var code = builder.GenerateFullCode();
            Assert.Contains("hello;", code);
        }

        [Fact]
        public void BeginScope_IncreasesIndent()
        {
            var builder = new CodeBuilder();
            using (builder.BeginScope("test"))
            {
                builder.AppendLine("inner;");
            }
            var code = builder.GenerateFullCode();
            Assert.Contains("    inner;", code);
        }

        [Fact]
        public void NestedScopes_IncreaseIndentFurther()
        {
            var builder = new CodeBuilder();
            using (builder.BeginScope("outer"))
            using (builder.BeginScope("inner"))
            {
                builder.AppendLine("deep;");
            }
            var code = builder.GenerateFullCode();
            Assert.Contains("        deep;", code);
        }

        [Fact]
        public void StartNameSpace_CreatesNamespaceBlock()
        {
            var builder = new CodeBuilder();
            using (builder.StartNameSpace("MyNamespace"))
            {
                builder.AppendLine("// content");
            }
            var code = builder.GenerateFullCode();
            Assert.Contains("namespace MyNamespace", code);
            Assert.Contains("{", code);
            Assert.Contains("}", code);
        }

        [Fact]
        public void StartClass_CreatesClassBlock()
        {
            var builder = new CodeBuilder();
            using (builder.StartClass("public", "MyClass"))
            {
            }
            var code = builder.GenerateFullCode();
            Assert.Contains("public class MyClass", code);
        }

        [Fact]
        public void StartClass_WithInheritance_IncludesBaseClass()
        {
            var builder = new CodeBuilder();
            using (builder.StartClass("internal", "Derived", "IBase"))
            {
            }
            var code = builder.GenerateFullCode();
            Assert.Contains("internal class Derived : IBase", code);
        }

        [Fact]
        public void StartInterface_CreatesInterfaceBlock()
        {
            var builder = new CodeBuilder();
            using (builder.StartInterface("internal", "IMyInterface"))
            {
            }
            var code = builder.GenerateFullCode();
            Assert.Contains("internal interface IMyInterface", code);
        }

        [Fact]
        public void StartConstructor_CreatesConstructorBlock()
        {
            var builder = new CodeBuilder();
            using (builder.StartConstructor("public", "MyClass"))
            {
                builder.AppendLine("// init");
            }
            var code = builder.GenerateFullCode();
            Assert.Contains("public MyClass()", code);
        }

        [Fact]
        public void StartMethod_CreatesMethodBlock()
        {
            var builder = new CodeBuilder();
            using (builder.StartMethod("public", "void", "DoWork", false, "int x"))
            {
            }
            var code = builder.GenerateFullCode();
            Assert.Contains("public void DoWork(int x)", code);
        }

        [Fact]
        public void StartMethod_Async_AddsAsyncKeyword()
        {
            var builder = new CodeBuilder();
            using (builder.StartMethod("public", "Task", "RunAsync", true))
            {
            }
            var code = builder.GenerateFullCode();
            Assert.Contains("public async Task RunAsync()", code);
        }

        [Fact]
        public void AppendField_CreatesFieldDeclaration()
        {
            var builder = new CodeBuilder();
            builder.AppendField("private", true, "string", "_name");
            var code = builder.GenerateFullCode();
            Assert.Contains("private readonly string _name;", code);
        }

        [Fact]
        public void AppendField_NotReadonly()
        {
            var builder = new CodeBuilder();
            builder.AppendField("public", false, "int", "Count");
            var code = builder.GenerateFullCode();
            Assert.Contains("public int Count;", code);
            Assert.DoesNotContain("readonly", code);
        }

        [Fact]
        public void PreprocessorDirectives_EmittedCorrectly()
        {
            var builder = new CodeBuilder();
            builder.AppendPreprocessorIf("UNITY_ANDROID");
            builder.AppendLine("// android code");
            builder.AppendPreprocessorElif("UNITY_IOS");
            builder.AppendLine("// ios code");
            builder.AppendPreprocessorEndif();
            var code = builder.GenerateFullCode();
            Assert.Contains("#if UNITY_ANDROID", code);
            Assert.Contains("#elif UNITY_IOS", code);
            Assert.Contains("#endif", code);
        }

        [Fact]
        public void GenerateFullCode_HasAutoGeneratedHeader()
        {
            var builder = new CodeBuilder();
            var code = builder.GenerateFullCode();
            Assert.StartsWith("// <auto-generated />", code);
        }

        [Fact]
        public void ScopeDispose_ClosesBlockCorrectly()
        {
            var builder = new CodeBuilder();
            using (builder.BeginScope("test"))
            {
                builder.AppendLine("inner;");
            }
            builder.AppendLine("outer;");
            var code = builder.GenerateFullCode();
            // After scope closes, "outer;" should not be indented
            Assert.Contains("outer;", code);
            // But "inner;" should be indented
            Assert.Contains("    inner;", code);
        }
    }
}
