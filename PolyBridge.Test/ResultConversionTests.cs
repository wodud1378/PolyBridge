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
        public void CustomType_ReturnsJsonUtilityFromJson()
        {
            var result = MethodModel.ResultConversion("result", "MyApp.UserInfo");
            Assert.Equal("UnityEngine.JsonUtility.FromJson<MyApp.UserInfo>(result)", result);
        }

        [Fact]
        public void DifferentVariableName_UsedInOutput()
        {
            var result = MethodModel.ResultConversion("data", "int");
            Assert.Equal("int.Parse(data)", result);
        }
    }
}
