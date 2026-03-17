using PolyBridge.Generator.Models;
using Xunit;

namespace PolyBridge.Test
{
    public class ResultConversionTests
    {
        [Fact]
        public void String_ReturnsVariableDirectly()
        {
            var result = MethodModel.ResultConversion("result", "string");
            Assert.Equal("result", result);
        }

        [Fact]
        public void Int_ReturnsIntParse()
        {
            var result = MethodModel.ResultConversion("result", "int");
            Assert.Equal("int.Parse(result)", result);
        }

        [Fact]
        public void Bool_ReturnsBoolParse()
        {
            var result = MethodModel.ResultConversion("result", "bool");
            Assert.Equal("bool.Parse(result)", result);
        }

        [Fact]
        public void Float_ReturnsFloatParse()
        {
            var result = MethodModel.ResultConversion("result", "float");
            Assert.Equal("float.Parse(result)", result);
        }

        [Fact]
        public void Double_ReturnsDoubleParse()
        {
            var result = MethodModel.ResultConversion("result", "double");
            Assert.Equal("double.Parse(result)", result);
        }

        [Fact]
        public void Long_ReturnsLongParse()
        {
            var result = MethodModel.ResultConversion("result", "long");
            Assert.Equal("long.Parse(result)", result);
        }

        [Fact]
        public void CustomType_ReturnsSerializerDeserialize()
        {
            var result = MethodModel.ResultConversion("result", "MyApp.UserInfo");
            Assert.Equal("PolyBridge.Core.Serialization.PolyBridgeSerializerRegistry.Serializer.Deserialize<MyApp.UserInfo>(result)", result);
        }

        [Fact]
        public void DifferentVariableName_UsedInOutput()
        {
            var result = MethodModel.ResultConversion("data", "int");
            Assert.Equal("int.Parse(data)", result);
        }

        // --- ParameterConversion Tests ---

        [Fact]
        public void ParameterConversion_PrimitiveString_PassThrough()
        {
            var result = MethodModel.ParameterConversion("name", "string");
            Assert.Equal("name", result);
        }

        [Fact]
        public void ParameterConversion_PrimitiveInt_PassThrough()
        {
            var result = MethodModel.ParameterConversion("count", "int");
            Assert.Equal("count", result);
        }

        [Fact]
        public void ParameterConversion_ComplexType_UsesSerializer()
        {
            var result = MethodModel.ParameterConversion("user", "MyApp.UserInfo");
            Assert.Equal("PolyBridge.Core.Serialization.PolyBridgeSerializerRegistry.Serializer.Serialize(user)", result);
        }

        [Fact]
        public void IsPrimitiveType_KnownPrimitives_ReturnsTrue()
        {
            Assert.True(MethodModel.IsPrimitiveType("string"));
            Assert.True(MethodModel.IsPrimitiveType("int"));
            Assert.True(MethodModel.IsPrimitiveType("bool"));
            Assert.True(MethodModel.IsPrimitiveType("float"));
            Assert.True(MethodModel.IsPrimitiveType("double"));
            Assert.True(MethodModel.IsPrimitiveType("long"));
        }

        [Fact]
        public void IsPrimitiveType_CustomType_ReturnsFalse()
        {
            Assert.False(MethodModel.IsPrimitiveType("MyApp.UserInfo"));
            Assert.False(MethodModel.IsPrimitiveType("SomeClass"));
        }
    }
}
