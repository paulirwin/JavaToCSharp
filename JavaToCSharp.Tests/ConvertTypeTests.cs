using JavaToCSharp;
using Xunit;

namespace TypeHelperTests
{
    public class ConvertTypeTests
    {
        [Fact]
        public void ConvertType_Int()
        {
            string typeName = "int";
            string type = TypeHelper.ConvertType(typeName);
            Assert.Equal("int", type);
        }

        [Fact]
        public void ConvertType_String()
        {
            string typeName = "string";
            string type = TypeHelper.ConvertType(typeName);
            Assert.Equal("string", type);
        }

        [Fact]
        public void ConvertType_IntArray_BracketsAfterType()
        {
            string typeName = "int[]";
            string type = TypeHelper.ConvertType(typeName);
            Assert.Equal("int[]", type);
        }
    }
}
