using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp
{
    public enum ConversionState
    {
        Starting = 0,
        ParsingJavaAST,
        BuildingCSharpAST,
        Done
    }
}
