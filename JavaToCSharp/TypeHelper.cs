using japa.parser.ast.type;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp
{
    public static class TypeHelper
    {
        public static string ConvertType(string typeName)
        {
            switch (typeName)
            {
                case "RuntimeException":
                case "Error":
                    return "Exception";
                case "Integer":
                    return "int";
                case "boolean":
                case "Boolean":
                    return "bool";
                case "String":
                    return "string";
                case "UnsupportedOperationException":
                    return "NotSupportedException";
                case "IllegalArgumentException":
                    return "ArgumentException";
                case "ICloseable":
                    return "IDisposable";
                case "AlreadyClosedException":
                    return "ObjectDisposedException";
                case "IllegalStateException":
                    return "InvalidOperationException";
                default:
                    return typeName;
            }
        }

        public static string Capitalize(string name)
        {
            var parts = name.Split('.');

            var joined = string.Join(".", parts.Select(i =>
            {
                if (i.Length == 1)
                    return i.ToUpper();
                else
                    return i[0].ToString().ToUpper() + i.Substring(1);
            }));

            return joined;
        }

        public static string ConvertIdentifierName(string name)
        {
            switch (name)
            {
                case "string":
                case "ref":
                case "object":
                case "int":
                case "short":
                case "float":
                case "long":
                case "double":
                case "in":
                case "out":
                case "byte":
                case "class":
                case "delegate":
                case "params":
                    return name + "_renamed";
                default:
                    return name;
            }
        }

        public static string ReplaceCommonMethodNames(string name)
        {
            switch (name.ToLower())
            {
                case "hashcode":
                    return "GetHashCode";
                case "getclass":
                    return "GetType";
                default:
                    return name;
            }
        }

        public static TypeSyntax GetSyntaxFromType(ClassOrInterfaceType type, bool addI = false)
        {
            string typeName = type.getName();

            if (addI)
                typeName = "I" + typeName;

            typeName = ConvertType(typeName);

            var typeArgs = type.getTypeArgs().ToList<japa.parser.ast.type.Type>();

            TypeSyntax typeSyntax;

            if (typeArgs != null && typeArgs.Count > 0)
            {
                typeSyntax = Syntax.GenericName(typeName)
                    .AddTypeArgumentListArguments(typeArgs.Select(i => Syntax.ParseTypeName(i.toString())).ToArray());
            }
            else
            {
                typeSyntax = Syntax.ParseTypeName(typeName);
            }

            return typeSyntax;
        }
    }
}
