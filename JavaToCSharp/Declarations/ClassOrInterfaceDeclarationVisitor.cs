using System.Linq;
using com.github.javaparser.ast;
using com.github.javaparser.ast.body;
using com.github.javaparser.ast.type;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Declarations
{
    public class ClassOrInterfaceDeclarationVisitor : BodyDeclarationVisitor<ClassOrInterfaceDeclaration>
    {
        public override MemberDeclarationSyntax? VisitForClass(ConversionContext context, ClassDeclarationSyntax classSyntax,
                                                               ClassOrInterfaceDeclaration declaration)
        {
            return VisitClassDeclaration(context, declaration);
        }


        public override MemberDeclarationSyntax? VisitForInterface(ConversionContext context, InterfaceDeclarationSyntax interfaceSyntax,
                                                                   ClassOrInterfaceDeclaration declaration)
        {
            return VisitClassDeclaration(context, declaration);
        }

        public static InterfaceDeclarationSyntax? VisitInterfaceDeclaration(ConversionContext context, ClassOrInterfaceDeclaration javai, bool isNested = false)
        {
            string originalTypeName = javai.getName();
            string newTypeName = $"I{originalTypeName}";
            TypeHelper.AddOrUpdateTypeNameConversions(originalTypeName, newTypeName);

            if (!isNested)
                context.RootTypeName = newTypeName;

            context.LastTypeName = newTypeName;

            var classSyntax = SyntaxFactory.InterfaceDeclaration(newTypeName);

            var typeParams = javai.getTypeParameters().ToList<TypeParameter>();

            if (typeParams is {Count: > 0})
            {
                classSyntax = classSyntax.AddTypeParameterListParameters(typeParams.Select(i => SyntaxFactory.TypeParameter(i.getName())).ToArray());
            }

            var mods = javai.getModifiers();

            if (mods.HasFlag(Modifier.PRIVATE))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PUBLIC))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.FINAL))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword));

            var implements = javai.getImplements().ToList<ClassOrInterfaceType>();

            if (implements != null)
            {
                foreach (var implement in implements)
                {
                    classSyntax = classSyntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.GetSyntaxFromType(implement)));
                }
            }

            var members = javai.getMembers()?.ToList<BodyDeclaration>();

            if (members is not null)
                foreach (var member in members)
                {
                    var syntax = VisitBodyDeclarationForInterface(context, classSyntax, member);
                    var memberWithComments = syntax?.WithJavaComments(member);
                    if (memberWithComments != null)
                    {
                        classSyntax = classSyntax.AddMembers(memberWithComments);
                    }
                }

            return classSyntax.WithJavaComments(javai);
        }

        public static ClassDeclarationSyntax? VisitClassDeclaration(ConversionContext context, ClassOrInterfaceDeclaration javac, bool isNested = false)
        {
            string name = javac.getName();

            if (!isNested)
                context.RootTypeName = name;

            context.LastTypeName = name;

            var classSyntax = SyntaxFactory.ClassDeclaration(name);

            var typeParams = javac.getTypeParameters().ToList<TypeParameter>();

            if (typeParams is {Count: > 0})
            {
                classSyntax = classSyntax.AddTypeParameterListParameters(typeParams.Select(i => SyntaxFactory.TypeParameter(i.getName())).ToArray());
            }

            var mods = javac.getModifiers();

            if (mods.HasFlag(Modifier.PRIVATE))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PUBLIC))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.ABSTRACT))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
            if (mods.HasFlag(Modifier.FINAL))
                classSyntax = classSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword));

            var extends = javac.getExtends().ToList<ClassOrInterfaceType>();

            if (extends != null)
            {
                foreach (var extend in extends)
                {
                    classSyntax = classSyntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.GetSyntaxFromType(extend)));
                }
            }

            var implements = javac.getImplements().ToList<ClassOrInterfaceType>();

            if (implements != null)
            {
                foreach (var implement in implements)
                {
                    classSyntax = classSyntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(TypeHelper.GetSyntaxFromType(implement)));
                }
            }

            var members = javac.getMembers()?.ToList<BodyDeclaration>();

            if (members is not null)
                foreach (var member in members)
                {
                    if (member is ClassOrInterfaceDeclaration childType)
                    {
                        if (childType.isInterface())
                        {
                            var childInt = VisitInterfaceDeclaration(context, childType, true);
                            if (childInt is not null)
                            {
                                classSyntax = classSyntax.AddMembers(childInt);
                            }
                        }
                        else
                        {
                            var childClass = VisitClassDeclaration(context, childType, true);
                            if (childClass is not null)
                            {
                                classSyntax = classSyntax.AddMembers(childClass);
                            }
                        }
                    }
                    else
                    {
                        var syntax = VisitBodyDeclarationForClass(context, classSyntax, member);
                        var withJavaComments = syntax?.WithJavaComments(member);
                        if (withJavaComments != null)
                        {
                            classSyntax = classSyntax.AddMembers(withJavaComments);
                        }
                    }

                    while (context.PendingAnonymousTypes.Count > 0)
                    {
                        var anon = context.PendingAnonymousTypes.Dequeue();
                        classSyntax = classSyntax.AddMembers(anon);
                    }
                }

            return classSyntax.WithJavaComments(javac);
        }
    }
}