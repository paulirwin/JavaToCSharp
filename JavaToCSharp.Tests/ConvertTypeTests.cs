using JavaToCSharp;
using Xunit;

namespace TypeHelperTests
{
    public class ConvertTypeTests
    {
        [Fact]
        public void ConvertType_Int()
        {
            Assert.Equal("int", TypeHelper.ConvertType("int"));
        }

        [Fact]
        public void ConvertType_String()
        {
            Assert.Equal("string", TypeHelper.ConvertType("String"));
        }

        [Fact]
        public void ConvertType_Object()
        {
            Assert.Equal("object", TypeHelper.ConvertType("Object"));
        }

        [Fact]
        public void ConvertType_IntArray_BracketsAfterType()
        {
            Assert.Equal("int[]", TypeHelper.ConvertType("int[]"));
        }

        [Fact]
        public void ConvertType_GenericSingleParameter()
        {
            Assert.Equal("MyType<string>", TypeHelper.ConvertType("MyType<String>"));
        }

        [Fact]
        public void ConvertType_GenericMultipleParameters()
        {
            Assert.Equal("MyType<string, object>", TypeHelper.ConvertType("MyType<String, Object>"));
        }

        [Fact]
        public void ConvertType_WithGenericWildcard_ShouldReplaceToken()
        {
            Assert.Equal("MyType<TWildcardTodo>", TypeHelper.ConvertType("MyType<?>"));
        }
    }
}
