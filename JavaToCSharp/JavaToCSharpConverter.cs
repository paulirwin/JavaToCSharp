using japa.parser;
using japa.parser.ast;
using japa.parser.ast.body;
using japa.parser.ast.expr;
using japa.parser.ast.stmt;
using japa.parser.ast.type;
using japa.parser.ast.visitor;
using java.io;
using java.lang.reflect;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Formatting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaToCSharp
{
    public static class JavaToCSharpConverter
    {
        public static string ConvertText(string javaText, JavaConversionOptions options = null)
        {
            if (options == null)
                options = new JavaConversionOptions();

            var context = new ConversionContext(options);

            var textBytes = Encoding.UTF8.GetBytes(javaText ?? string.Empty);

            using (var stringreader = new MemoryStream(textBytes))
            using (var wrapper = new ikvm.io.InputStreamWrapper(stringreader))
            {
                var parsed = JavaParser.parse(wrapper);

                var types = parsed.getTypes().ToList<TypeDeclaration>();
                var imports = parsed.getImports().ToList<ImportDeclaration>();
                var package = parsed.getPackage();

                var usings = new List<UsingDirectiveSyntax>();

                //foreach (var import in imports)
                //{
                //    var usingSyntax = Syntax.UsingDirective(Syntax.ParseName(import.getName().toString()));
                //    usings.Add(usingSyntax);
                //}

                if (options.IncludeUsings)
                {
                    foreach (var ns in options.Usings)
                    {
                        var usingSyntax = Syntax.UsingDirective(Syntax.ParseName(ns));
                        usings.Add(usingSyntax);
                    }
                }

                var rootMembers = new List<MemberDeclarationSyntax>();
                NamespaceDeclarationSyntax namespaceSyntax = null;

                if (options.IncludeNamespace)
                {
                    string packageName = package.getName().toString();

                    foreach (var packageReplacement in options.PackageReplacements)
                    {
                        packageName = packageReplacement.Replace(packageName);
                    }

                    packageName = Capitalize(packageName);

                    namespaceSyntax = Syntax.NamespaceDeclaration(Syntax.ParseName(packageName));
                }

                foreach (var type in types)
                {
                    if (type is ClassOrInterfaceDeclaration)
                    {
                        var classOrIntType = type as ClassOrInterfaceDeclaration;

                        if (classOrIntType.isInterface())
                        {
                            var interfaceSyntax = VisitInterfaceDeclaration(context, classOrIntType, false);

                            if (options.IncludeNamespace)
                                namespaceSyntax = namespaceSyntax.AddMembers(interfaceSyntax);
                            else
                                rootMembers.Add(interfaceSyntax);
                        }
                        else
                        {
                            var classSyntax = VisitClassDeclaration(context, classOrIntType, false);

                            if (options.IncludeNamespace)
                                namespaceSyntax = namespaceSyntax.AddMembers(classSyntax);
                            else
                                rootMembers.Add(classSyntax);
                        }
                    }
                }

                if (options.IncludeNamespace)
                    rootMembers.Add(namespaceSyntax);

                var root = Syntax.CompilationUnit(
                    externs: null,
                    usings: Syntax.List(usings.ToArray()),
                    attributeLists: null,
                    members: Syntax.List<MemberDeclarationSyntax>(rootMembers))
                    .NormalizeWhitespace();

                var tree = SyntaxTree.Create(root);

                return tree.GetText().ToString();
            }
        }

        private static InterfaceDeclarationSyntax VisitInterfaceDeclaration(ConversionContext context, ClassOrInterfaceDeclaration javai, bool isNested = false)
        {
            string name = "I" + javai.getName();

            if (!isNested)
                context.RootTypeName = name;

            context.LastTypeName = name;

            var classSyntax = Syntax.InterfaceDeclaration(name);

            var typeParams = javai.getTypeParameters().ToList<TypeParameter>();

            if (typeParams != null && typeParams.Count > 0)
            {
                classSyntax = classSyntax.AddTypeParameterListParameters(typeParams.Select(i => Syntax.TypeParameter(i.getName())).ToArray());
            }
            
            var mods = javai.getModifiers();

            if (mods.HasFlag(Modifier.PRIVATE))
                classSyntax = classSyntax.AddModifiers(Syntax.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                classSyntax = classSyntax.AddModifiers(Syntax.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PUBLIC))
                classSyntax = classSyntax.AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.FINAL))
                classSyntax = classSyntax.AddModifiers(Syntax.Token(SyntaxKind.SealedKeyword));

            var implements = javai.getImplements().ToList<ClassOrInterfaceType>();

            if (implements != null)
            {
                foreach (var implement in implements)
                {
                    classSyntax = classSyntax.AddBaseListTypes(GetSyntaxFromType(implement));
                }
            }

            var members = javai.getMembers().ToList<BodyDeclaration>();

            foreach (var member in members)
            {
                if (member is MethodDeclaration)
                {
                    classSyntax = VisitInterfaceMethodDeclaration(context, classSyntax, (MethodDeclaration)member);
                }
                else 
                    throw new NotImplementedException("Have not implemented for interface member type " + member.GetType());
            }

            return classSyntax;
        }

        private static InterfaceDeclarationSyntax VisitInterfaceMethodDeclaration(ConversionContext context, InterfaceDeclarationSyntax classSyntax, MethodDeclaration methodDecl)
        {
            var returnType = methodDecl.getType();
            var returnTypeName = ConvertType(returnType.toString());

            var methodName = Capitalize(methodDecl.getName());
            methodName = ReplaceCommonMethodNames(methodName);

            var methodSyntax = Syntax.MethodDeclaration(Syntax.ParseTypeName(returnTypeName), methodName);

            var parameters = methodDecl.getParameters().ToList<Parameter>();

            if (parameters != null && parameters.Count > 0)
            {
                var paramSyntax = parameters.Select(i =>
                    Syntax.Parameter(
                        attributeLists: null,
                        modifiers: Syntax.TokenList(),
                        type: Syntax.ParseTypeName(ConvertType(i.getType().toString())),
                        identifier: Syntax.ParseToken(ConvertIdentifierName(i.getId().toString())),
                        @default: null))
                    .ToArray();

                methodSyntax = methodSyntax.AddParameterListParameters(paramSyntax.ToArray());
            }

            methodSyntax = methodSyntax.WithSemicolonToken(Syntax.Token(SyntaxKind.SemicolonToken));
            
            classSyntax = classSyntax.AddMembers(methodSyntax);
            return classSyntax;
        }

        private static ClassDeclarationSyntax VisitClassDeclaration(ConversionContext context, ClassOrInterfaceDeclaration javac, bool isNested = false)
        {
            string name = javac.getName();
            
            if (!isNested)
                context.RootTypeName = name;
            
            context.LastTypeName = name;

            var classSyntax = Syntax.ClassDeclaration(name);

            var typeParams = javac.getTypeParameters().ToList<TypeParameter>();

            if (typeParams != null && typeParams.Count > 0)
            {
                classSyntax = classSyntax.AddTypeParameterListParameters(typeParams.Select(i => Syntax.TypeParameter(i.getName())).ToArray());
            }

            var mods = javac.getModifiers();

            if (mods.HasFlag(Modifier.PRIVATE))
                classSyntax = classSyntax.AddModifiers(Syntax.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                classSyntax = classSyntax.AddModifiers(Syntax.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PUBLIC))
                classSyntax = classSyntax.AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.ABSTRACT))
                classSyntax = classSyntax.AddModifiers(Syntax.Token(SyntaxKind.AbstractKeyword));
            if (mods.HasFlag(Modifier.FINAL))
                classSyntax = classSyntax.AddModifiers(Syntax.Token(SyntaxKind.SealedKeyword));

            var extends = javac.getExtends().ToList<ClassOrInterfaceType>();

            if (extends != null)
            {
                foreach (var extend in extends)
                {
                    classSyntax = classSyntax.AddBaseListTypes(GetSyntaxFromType(extend));
                }
            }

            var implements = javac.getImplements().ToList<ClassOrInterfaceType>();

            if (implements != null)
            {
                foreach (var implement in implements)
                {
                    classSyntax = classSyntax.AddBaseListTypes(GetSyntaxFromType(implement, true));
                }
            }

            var members = javac.getMembers().ToList<BodyDeclaration>();

            foreach (var member in members)
            {
                if (member is FieldDeclaration)
                {
                    classSyntax = VisitFieldDeclaration(context, classSyntax, (FieldDeclaration)member);
                }
                else if (member is ConstructorDeclaration)
                {
                    classSyntax = VisitConstructor(context, classSyntax, (ConstructorDeclaration)member);
                }
                else if (member is MethodDeclaration)
                {
                    classSyntax = VisitMethodDeclaration(context, classSyntax, (MethodDeclaration)member);
                }
                else if (member is ClassOrInterfaceDeclaration)
                {
                    var childc = (ClassOrInterfaceDeclaration)member;

                    if (childc.isInterface())
                    {
                        var childInt = VisitInterfaceDeclaration(context, childc, true);

                        classSyntax = classSyntax.AddMembers(childInt);
                    }
                    else
                    {
                        var childClass = VisitClassDeclaration(context, childc, true);

                        classSyntax = classSyntax.AddMembers(childClass);
                    }
                }
                else if (member is EnumDeclaration)
                {
                    classSyntax = VisitEnumDeclaration(context, classSyntax, (EnumDeclaration)member);
                }
                else
                {
                    throw new NotImplementedException("Member type not implemented: " + member.GetType().Name);
                }

                while (context.PendingAnonymousTypes.Count > 0)
                {
                    var anon = context.PendingAnonymousTypes.Dequeue();
                    classSyntax = classSyntax.AddMembers(anon);
                }
            }

            return classSyntax;
        }

        private static ClassDeclarationSyntax VisitEnumDeclaration(ConversionContext context, ClassDeclarationSyntax classSyntax, EnumDeclaration enumDecl)
        {
            var name = enumDecl.getName();

            var members = enumDecl.getMembers().ToList<BodyDeclaration>();

            var entries = enumDecl.getEntries().ToList<EnumConstantDeclaration>();
            var memberSyntaxes = new List<EnumMemberDeclarationSyntax>();

            foreach (var entry in entries)
            {
                // TODO: support "equals" value
                memberSyntaxes.Add(Syntax.EnumMemberDeclaration(entry.getName()));
            }

            if (members != null && members.Count > 0)
                context.Options.Warning("Members found in enum " + name + " will not be ported. Check for correctness.", enumDecl.getBeginLine());

            var enumSyntax = Syntax.EnumDeclaration(name)
                .AddMembers(memberSyntaxes.ToArray());

            var mods = enumDecl.getModifiers();

            if (mods.HasFlag(Modifier.PRIVATE))
                enumSyntax = enumSyntax.AddModifiers(Syntax.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                enumSyntax = enumSyntax.AddModifiers(Syntax.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PUBLIC))
                enumSyntax = enumSyntax.AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));

            classSyntax = classSyntax.AddMembers(enumSyntax);
            return classSyntax;
        }

        private static ClassDeclarationSyntax VisitMethodDeclaration(ConversionContext context, ClassDeclarationSyntax classSyntax, MethodDeclaration methodDecl)
        {
            var returnType = methodDecl.getType();
            var returnTypeName = ConvertType(returnType.toString());

            var methodName = Capitalize(methodDecl.getName());
            methodName = ReplaceCommonMethodNames(methodName);

            var methodSyntax = Syntax.MethodDeclaration(Syntax.ParseTypeName(returnTypeName), methodName);

            var mods = methodDecl.getModifiers();

            if (mods.HasFlag(Modifier.PUBLIC))
                methodSyntax = methodSyntax.AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                methodSyntax = methodSyntax.AddModifiers(Syntax.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PRIVATE))
                methodSyntax = methodSyntax.AddModifiers(Syntax.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.STATIC))
                methodSyntax = methodSyntax.AddModifiers(Syntax.Token(SyntaxKind.StaticKeyword));
            if (mods.HasFlag(Modifier.ABSTRACT))
                methodSyntax = methodSyntax.AddModifiers(Syntax.Token(SyntaxKind.AbstractKeyword));

            var annotations = methodDecl.getAnnotations().ToList<AnnotationExpr>();
            bool isOverride = false;

            // TODO: figure out how to check for a non-interface base type
            if (annotations != null 
                && annotations.Count > 0)
            {
                foreach (var annotation in annotations)
                {
                    string name = annotation.getName().getName();
                    if (name == "Override")
                    {
                        methodSyntax = methodSyntax.AddModifiers(Syntax.Token(SyntaxKind.OverrideKeyword));
                        isOverride = true;
                    }
                }
            }

            if (!mods.HasFlag(Modifier.FINAL) 
                && !mods.HasFlag(Modifier.ABSTRACT) 
                && !mods.HasFlag(Modifier.STATIC) 
                && !mods.HasFlag(Modifier.PRIVATE)
                && !isOverride
                && !classSyntax.Modifiers.Any(i => i.Kind == SyntaxKind.SealedKeyword))
                methodSyntax = methodSyntax.AddModifiers(Syntax.Token(SyntaxKind.VirtualKeyword));

            var parameters = methodDecl.getParameters().ToList<Parameter>();

            if (parameters != null && parameters.Count > 0)
            {
                var paramSyntaxes = new List<ParameterSyntax>();

                foreach (var param in parameters)
                {
                    string typeName = ConvertType(param.getType().toString());
                    string identifier = ConvertIdentifierName(param.getId().getName());

                    if ((param.getId().getArrayCount() > 0 && !typeName.EndsWith("[]")) || param.isVarArgs())
                        typeName += "[]";

                    SyntaxTokenList modifiers = Syntax.TokenList();

                    if (param.isVarArgs())
                        modifiers = Syntax.TokenList(Syntax.Token(SyntaxKind.ParamsKeyword));

                    var paramSyntax = Syntax.Parameter(attributeLists: null,
                        modifiers: modifiers,
                        type: Syntax.ParseTypeName(typeName),
                        identifier: Syntax.ParseToken(identifier),
                        @default: null);

                    paramSyntaxes.Add(paramSyntax);
                }

                methodSyntax = methodSyntax.AddParameterListParameters(paramSyntaxes.ToArray());
            }

            var block = methodDecl.getBody();

            if (block == null)
            {
                // i.e. abstract method
                methodSyntax = methodSyntax.WithSemicolonToken(Syntax.Token(SyntaxKind.SemicolonToken));
                classSyntax = classSyntax.AddMembers(methodSyntax);
                return classSyntax;
            }

            var statements = block.getStmts().ToList<Statement>();

            var statementSyntax = VisitStatements(context, statements);

            if (mods.HasFlag(Modifier.SYNCHRONIZED))
            {
                LockStatementSyntax lockSyntax;
                BlockSyntax lockBlock = Syntax.Block(statementSyntax);

                if (mods.HasFlag(Modifier.STATIC))
                {
                    lockSyntax = Syntax.LockStatement(Syntax.TypeOfExpression(Syntax.ParseTypeName(classSyntax.Identifier.Value.ToString())), lockBlock);
                }
                else
                {
                    lockSyntax = Syntax.LockStatement(Syntax.ThisExpression(), lockBlock);
                }

                methodSyntax = methodSyntax.AddBodyStatements(lockSyntax);
            }
            else
            {
                methodSyntax = methodSyntax.AddBodyStatements(statementSyntax.ToArray());
            }

            classSyntax = classSyntax.AddMembers(methodSyntax);
            return classSyntax;
        }

        private static ClassDeclarationSyntax VisitConstructor(ConversionContext context, ClassDeclarationSyntax classSyntax, ConstructorDeclaration ctorDecl)
        {
            var ctorSyntax = Syntax.ConstructorDeclaration(classSyntax.Identifier.Value.ToString())
                .WithLeadingTrivia(Syntax.CarriageReturnLineFeed);

            var mods = ctorDecl.getModifiers();

            if (mods.HasFlag(Modifier.PUBLIC))
                ctorSyntax = ctorSyntax.AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                ctorSyntax = ctorSyntax.AddModifiers(Syntax.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PRIVATE))
                ctorSyntax = ctorSyntax.AddModifiers(Syntax.Token(SyntaxKind.PrivateKeyword));

            var parameters = ctorDecl.getParameters().ToList<Parameter>();

            if (parameters != null && parameters.Count > 0)
            {
                var paramSyntaxes = new List<ParameterSyntax>();

                foreach (var param in parameters)
                {
                    var paramSyntax = Syntax.Parameter(Syntax.ParseToken(ConvertIdentifierName(param.getId().toString())));

                    if (param.isVarArgs())
                    {
                        paramSyntax = paramSyntax.WithType(Syntax.ParseTypeName(ConvertType(param.getType().toString()) + "[]"))
                            .WithModifiers(Syntax.TokenList(Syntax.Token(SyntaxKind.ParamsKeyword)));
                    }
                    else
                    {
                        paramSyntax = paramSyntax.WithType(Syntax.ParseTypeName(ConvertType(param.getType().toString())));
                    }

                    paramSyntaxes.Add(paramSyntax);
                }

                ctorSyntax = ctorSyntax.AddParameterListParameters(paramSyntaxes.ToArray());
            }

            var block = ctorDecl.getBlock();
            var statements = block.getStmts().ToList<Statement>();

            // handle special case for constructor invocation
            if (statements != null && statements.Count > 0 && statements[0] is ExplicitConstructorInvocationStmt)
            {
                var ctorInvStmt = (ExplicitConstructorInvocationStmt)statements[0];
                statements.RemoveAt(0);

                ArgumentListSyntax argsSyntax = null;

                var initargs = ctorInvStmt.getArgs().ToList<Expression>();

                if (initargs != null && initargs.Count > 0)
                {
                    var initargslist = new List<ArgumentSyntax>();

                    foreach (var arg in initargs)
                    {
                        var argsyn = VisitExpression(context, arg);
                        initargslist.Add(Syntax.Argument(argsyn));
                    }

                    argsSyntax = Syntax.ArgumentList(Syntax.SeparatedList(initargslist, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), initargslist.Count - 1)));
                }

                ConstructorInitializerSyntax ctorinitsyn;
                
                if (ctorInvStmt.isThis())
                    ctorinitsyn = Syntax.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, argsSyntax);
                else
                    ctorinitsyn = Syntax.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, argsSyntax);

                ctorSyntax = ctorSyntax.WithInitializer(ctorinitsyn);
            }

            var statementSyntax = VisitStatements(context, statements);

            ctorSyntax = ctorSyntax.AddBodyStatements(statementSyntax.ToArray());

            classSyntax = classSyntax.AddMembers(ctorSyntax);
            return classSyntax;
        }

        private static List<StatementSyntax> VisitStatements(ConversionContext context, IEnumerable<Statement> statements)
        {
            if (statements == null)
                return new List<StatementSyntax>();

            var syntaxes = new List<StatementSyntax>();

            foreach (var statement in statements)
            {
                StatementSyntax syntax = VisitStatement(context, statement);

                if (syntax != null)
                    syntaxes.Add(syntax);
            }

            return syntaxes;
        }

        private static StatementSyntax VisitStatement(ConversionContext context, Statement statement)
        {
            if (statement is TryStmt)
            {
                return VisitTryStatement(context, (TryStmt)statement);
            }
            else if (statement is ExpressionStmt)
            {
                return VisitExpressionStatement(context, (ExpressionStmt)statement);
            }
            else if (statement is ThrowStmt)
            {
                return VisitThrowStatement(context, (ThrowStmt)statement);
            }
            else if (statement is ReturnStmt)
            {
                return VisitReturnStatement(context, (ReturnStmt)statement);
            }
            else if (statement is IfStmt)
            {
                return VisitIfStatement(context, (IfStmt)statement);
            }
            else if (statement is BlockStmt)
            {
                return VisitBlockStatement(context, (BlockStmt)statement);
            }
            else if (statement is ForStmt)
            {
                return VisitForStatement(context, (ForStmt)statement);
            }
            else if (statement is ForeachStmt)
            {
                return VisitForEachStatement(context, (ForeachStmt)statement);
            }
            else if (statement is WhileStmt)
            {
                return VisitWhileStatement(context, (WhileStmt)statement);
            }
            else if (statement is ContinueStmt)
            {
                var cnt = (ContinueStmt)statement;

                if (!string.IsNullOrEmpty(cnt.getId()))
                    context.Options.Warning("Continue with label detected, using plain continue instead. Check for correctness.", cnt.getBeginLine());

                return Syntax.ContinueStatement();
            }
            else if (statement is BreakStmt)
            {
                var brk = (BreakStmt)statement;

                if (!string.IsNullOrEmpty(brk.getId()))
                    context.Options.Warning("Break with label detected, using plain break instead. Check for correctness.", brk.getBeginLine());

                return Syntax.BreakStatement();
            }
            else if (statement is SynchronizedStmt)
            {
                return VisitSynchronizedStatement(context, (SynchronizedStmt)statement);
            }
            else if (statement is AssertStmt)
            {
                if (context.Options.UseDebugAssertForAsserts)
                {
                    return VisitAssertStatement(context, (AssertStmt)statement);
                }
                else
                {
                    return null; // no statement
                }
            }
            else if (statement is LabeledStmt)
            {
                return VisitLabeledStatement(context, (LabeledStmt)statement);
            }
            else if (statement is SwitchStmt)
            {
                return VisitSwitchStatement(context, (SwitchStmt)statement);
            }
            else if (statement is DoStmt)
            {
                return VisitDoStatement(context, (DoStmt)statement);
            }

            throw new NotImplementedException("Statement translation not implemented for " + statement.GetType().Name);
        }

        private static StatementSyntax VisitDoStatement(ConversionContext context, DoStmt doStmt)
        {
            var condition = doStmt.getCondition();
            var conditionSyntax = VisitExpression(context, condition);

            var body = doStmt.getBody();
            var bodySyntax = VisitStatement(context, body);

            return Syntax.DoStatement(bodySyntax, conditionSyntax);
        }
        
        private static StatementSyntax VisitAssertStatement(ConversionContext context, AssertStmt assertStmt)
        {
            var check = assertStmt.getCheck();
            var checkSyntax = VisitExpression(context, check);

            var message = assertStmt.getMessage();

            if (message == null)
                return Syntax.ExpressionStatement(
                    Syntax.InvocationExpression(
                        Syntax.IdentifierName("Debug.Assert"), 
                        Syntax.ArgumentList(Syntax.SeparatedList(Syntax.Argument(checkSyntax)))));

            var messageSyntax = VisitExpression(context, message);

            return Syntax.ExpressionStatement(
                    Syntax.InvocationExpression(
                        Syntax.IdentifierName("Debug.Assert"),
                        Syntax.ArgumentList(Syntax.SeparatedList(new[] { Syntax.Argument(checkSyntax), Syntax.Argument(messageSyntax) }, new[] { Syntax.Token(SyntaxKind.CommaToken) }))));
        }

        private static StatementSyntax VisitSwitchStatement(ConversionContext context, SwitchStmt switchStmt)
        {
            var selector = switchStmt.getSelector();
            var selectorSyntax = VisitExpression(context, selector);

            var cases = switchStmt.getEntries().ToList<SwitchEntryStmt>();

            if (cases == null)
                return Syntax.SwitchStatement(selectorSyntax, Syntax.List<SwitchSectionSyntax>());

            var caseSyntaxes = new List<SwitchSectionSyntax>();

            foreach (var cs in cases)
            {
                var label = cs.getLabel();

                var statements = cs.getStmts().ToList<Statement>();
                var syntaxes = VisitStatements(context, statements);

                if (label == null)
                {
                    // default case
                    var defaultSyntax = Syntax.SwitchSection(Syntax.List(Syntax.SwitchLabel(SyntaxKind.DefaultSwitchLabel)), Syntax.List(syntaxes.AsEnumerable()));
                    caseSyntaxes.Add(defaultSyntax);
                }
                else
                {
                    var labelSyntax = VisitExpression(context, label);

                    var caseSyntax = Syntax.SwitchSection(Syntax.List(Syntax.SwitchLabel(SyntaxKind.CaseSwitchLabel, labelSyntax)), Syntax.List(syntaxes.AsEnumerable()));
                    caseSyntaxes.Add(caseSyntax);
                }
            }

            return Syntax.SwitchStatement(selectorSyntax, Syntax.List<SwitchSectionSyntax>(caseSyntaxes));
        }

        private static StatementSyntax VisitLabeledStatement(ConversionContext context, LabeledStmt labeledStmt)
        {
            var statement = labeledStmt.getStmt();
            var syntax = VisitStatement(context, statement);

            if (syntax == null)
                return null;

            return Syntax.LabeledStatement(labeledStmt.getLabel(), syntax);
        }

        private static StatementSyntax VisitSynchronizedStatement(ConversionContext context, SynchronizedStmt synchronizedStmt)
        {
            var lockExpr = synchronizedStmt.getExpr();
            var lockSyntax = VisitExpression(context, lockExpr);

            var body = synchronizedStmt.getBlock();
            var bodySyntax = VisitBlockStatement(context, body);

            if (bodySyntax == null)
                return null;

            return Syntax.LockStatement(lockSyntax, bodySyntax);
        }

        private static StatementSyntax VisitWhileStatement(ConversionContext context, WhileStmt whileStmt)
        {
            var expr = whileStmt.getCondition();
            var syntax = VisitExpression(context, expr);

            var body = whileStmt.getBody();
            var bodySyntax = VisitStatement(context, body);

            if (bodySyntax == null)
                return null;

            return Syntax.WhileStatement(syntax, bodySyntax);
        }

        private static StatementSyntax VisitForEachStatement(ConversionContext context, ForeachStmt foreachStmt)
        {
            var iterableExpr = foreachStmt.getIterable();
            var iterableSyntax = VisitExpression(context, iterableExpr);

            var varExpr = foreachStmt.getVariable();
            var type = ConvertType(varExpr.getType().toString());

            var vars = varExpr.getVars()
                .ToList<VariableDeclarator>()
                .Select(i => Syntax.VariableDeclarator(i.toString()))
                .ToArray();

            var body = foreachStmt.getBody();
            var bodySyntax = VisitStatement(context, body);

            if (bodySyntax == null)
                return null;

            return Syntax.ForEachStatement(Syntax.ParseTypeName(type), vars[0].Identifier.ValueText, iterableSyntax, bodySyntax);
        }

        private static StatementSyntax VisitForStatement(ConversionContext context, ForStmt forStmt)
        {
            var inits = forStmt.getInit().ToList<Expression>();

            var initSyntaxes = new List<ExpressionSyntax>();
            VariableDeclarationSyntax varSyntax = null;

            foreach (var init in inits)
	        {
                if (init is VariableDeclarationExpr)
                {
                    var varExpr = init as VariableDeclarationExpr;

                    var type = ConvertType(varExpr.getType().toString());

                    var vars = varExpr.getVars()
                        .ToList<VariableDeclarator>()
                        .Select(i => Syntax.VariableDeclarator(i.toString()))
                        .ToArray();

                    varSyntax = Syntax.VariableDeclaration(Syntax.ParseTypeName(type), Syntax.SeparatedList(vars, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), vars.Length - 1)));
                }
                else
                {
                    var initSyntax = VisitExpression(context, init);
                    initSyntaxes.Add(initSyntax);
                }
	        }

            var condition = forStmt.getCompare();
            var conditionSyntax = VisitExpression(context, condition);

            var increments = forStmt.getUpdate().ToList<Expression>();
            var incrementSyntaxes = new List<ExpressionSyntax>();

            if (increments != null)
            {
                foreach (var increment in increments)
                {
                    var incrementSyntax = VisitExpression(context, increment);
                    incrementSyntaxes.Add(incrementSyntax);
                }
            }

            var body = forStmt.getBody();
            var bodySyntax = VisitStatement(context, body);

            if (bodySyntax == null)
                return null;

            return Syntax.ForStatement(bodySyntax)
                .WithDeclaration(varSyntax)
                .AddInitializers(initSyntaxes.ToArray())
                .WithCondition(conditionSyntax)
                .AddIncrementors(incrementSyntaxes.ToArray());
        }

        private static StatementSyntax VisitBlockStatement(ConversionContext context, BlockStmt blockStmt)
        {
            var stmts = blockStmt.getStmts().ToList<Statement>();

            var syntaxes = VisitStatements(context, stmts);

            return Syntax.Block(syntaxes);
        }

        private static StatementSyntax VisitIfStatement(ConversionContext context, IfStmt ifStmt)
        {
            var condition = ifStmt.getCondition();
            var conditionSyntax = VisitExpression(context, condition);

            var thenStmt = ifStmt.getThenStmt();
            var thenSyntax = VisitStatement(context, thenStmt);

            if (thenSyntax == null)
                return null;

            var elseStmt = ifStmt.getElseStmt();

            if (elseStmt == null)
                return Syntax.IfStatement(conditionSyntax, thenSyntax);

            var elseStatementSyntax = VisitStatement(context, elseStmt);
            var elseSyntax = Syntax.ElseClause(elseStatementSyntax);

            if (elseSyntax == null)
                return Syntax.IfStatement(conditionSyntax, thenSyntax);

            return Syntax.IfStatement(conditionSyntax, thenSyntax, elseSyntax);
        }

        private static StatementSyntax VisitReturnStatement(ConversionContext context, ReturnStmt returnStmt)
        {
            var expr = returnStmt.getExpr();

            if (expr == null)
                return Syntax.ReturnStatement(); // i.e. "return" in a void method

            var exprSyntax = VisitExpression(context, expr);

            return Syntax.ReturnStatement(exprSyntax);
        }

        private static StatementSyntax VisitThrowStatement(ConversionContext context, ThrowStmt throwStmt)
        {
            var expr = throwStmt.getExpr();

            var exprSyntax = VisitExpression(context, expr);

            return Syntax.ThrowStatement(exprSyntax);
        }

        private static StatementSyntax VisitExpressionStatement(ConversionContext context, ExpressionStmt exprStmt)
        {
            var expression = exprStmt.getExpression();

            // handle special case where AST is different
            if (expression is VariableDeclarationExpr)
                return VisitVariableDeclarationStatement(context, (VariableDeclarationExpr)expression);

            var expressionSyntax = VisitExpression(context, expression);

            return Syntax.ExpressionStatement(expressionSyntax);
        }

        private static ExpressionSyntax VisitAssignmentExpression(ConversionContext context, AssignExpr assignExpr)
        {
            var left = assignExpr.getTarget();
            var leftSyntax = VisitExpression(context, left);

            var right = assignExpr.getValue();
            var rightSyntax = VisitExpression(context, right);

            var op = assignExpr.getOperator();
            var kind = SyntaxKind.None;

            if (op == AssignExpr.Operator.and)
                kind = SyntaxKind.AndAssignExpression;
            else if (op == AssignExpr.Operator.assign)
                kind = SyntaxKind.AssignExpression;
            else if (op == AssignExpr.Operator.lShift)
                kind = SyntaxKind.LeftShiftAssignExpression;
            else if (op == AssignExpr.Operator.minus)
                kind = SyntaxKind.SubtractAssignExpression;
            else if (op == AssignExpr.Operator.or)
                kind = SyntaxKind.OrAssignExpression;
            else if (op == AssignExpr.Operator.plus)
                kind = SyntaxKind.AddAssignExpression;
            else if (op == AssignExpr.Operator.rem)
                kind = SyntaxKind.ModuloAssignExpression;
            else if (op == AssignExpr.Operator.rSignedShift)
                kind = SyntaxKind.RightShiftAssignExpression;
            else if (op == AssignExpr.Operator.rUnsignedShift)
            {
                context.Options.Warning("Use of unsigned right shift assignment. Using signed right shift assignment instead. Check for correctness.", assignExpr.getBeginLine());
                kind = SyntaxKind.RightShiftAssignExpression;
            }
            else if (op == AssignExpr.Operator.slash)
                kind = SyntaxKind.DivideAssignExpression;
            else if (op == AssignExpr.Operator.star)
                kind = SyntaxKind.MultiplyAssignExpression;
            else if (op == AssignExpr.Operator.xor)
                kind = SyntaxKind.ExclusiveOrAssignExpression;

            return Syntax.BinaryExpression(kind, leftSyntax, rightSyntax);
        }

        private static ExpressionSyntax VisitExpression(ConversionContext context, Expression expr)
        {
            if (expr is NameExpr)
            {
                return VisitNameExpression((NameExpr)expr);
            }
            else if (expr is AssignExpr)
            {
                return VisitAssignmentExpression(context, (AssignExpr)expr);
            }
            else if (expr is MethodCallExpr)
            {
                return VisitMethodCallExpression(context, (MethodCallExpr)expr);
            }
            else if (expr is FieldAccessExpr)
            {
                return VisitFieldAccessExpression(context, (FieldAccessExpr)expr);
            }
            else if (expr is ThisExpr)
            {
                return Syntax.ThisExpression();
            }
            else if (expr is SuperExpr)
            {
                return Syntax.BaseExpression();
            }
            else if (expr is ObjectCreationExpr)
            {
                return VisitObjectCreationExpression(context, (ObjectCreationExpr)expr);
            }
            else if (expr is ConditionalExpr)
            {
                return VisitConditionalExpression(context, (ConditionalExpr)expr);
            }
            else if (expr is NullLiteralExpr)
            {
                return Syntax.LiteralExpression(SyntaxKind.NullLiteralExpression);
            }
            else if (expr is BinaryExpr)
            {
                return VisitBinaryExpression(context, (BinaryExpr)expr);
            }
            else if (expr is IntegerLiteralExpr)
            {
                string value = expr.toString();

                if (value.StartsWith("0x"))
                    return Syntax.LiteralExpression(SyntaxKind.NumericLiteralExpression, Syntax.Literal(value, Convert.ToInt32(value.Substring(2), 16)));
                else
                    return Syntax.LiteralExpression(SyntaxKind.NumericLiteralExpression, Syntax.Literal(int.Parse(expr.toString())));
            }
            else if (expr is CharLiteralExpr)
            {
                return Syntax.LiteralExpression(SyntaxKind.CharacterLiteralExpression, Syntax.Literal(expr.toString().Trim('\'')[0]));
            }
            else if (expr is BooleanLiteralExpr)
            {
                if (((BooleanLiteralExpr)expr).getValue())
                    return Syntax.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                else
                    return Syntax.LiteralExpression(SyntaxKind.FalseLiteralExpression);
            }
            else if (expr is DoubleLiteralExpr)
            {
                // note: this must come before the check for StringLiteralExpr because DoubleLiteralExpr : StringLiteralExpr
                var dbl = (DoubleLiteralExpr)expr;

                if (dbl.getValue().EndsWith("f", StringComparison.OrdinalIgnoreCase))
                    return Syntax.LiteralExpression(SyntaxKind.NumericLiteralExpression, Syntax.Literal(float.Parse(dbl.getValue().TrimEnd('f', 'F'))));
                else
                    return Syntax.LiteralExpression(SyntaxKind.NumericLiteralExpression, Syntax.Literal(double.Parse(dbl.getValue().TrimEnd('d', 'D'))));
            }
            else if (expr is StringLiteralExpr)
            {
                return Syntax.LiteralExpression(SyntaxKind.StringLiteralExpression, Syntax.Literal(expr.toString().Trim('\"')));
            }
            else if (expr is VariableDeclarationExpr)
            {
                throw new InvalidOperationException("Should not get here!");
            }
            else if (expr is UnaryExpr)
            {
                return VisitUnaryExpr(context, (UnaryExpr)expr);
            }
            else if (expr is CastExpr)
            {
                return VisitCastExpr(context, (CastExpr)expr);
            }
            else if (expr is EnclosedExpr)
            {
                return VisitEnclosedExpr(context, (EnclosedExpr)expr);
            }
            else if (expr is InstanceOfExpr)
            {
                return VisitInstanceOfExpr(context, (InstanceOfExpr)expr);
            }
            else if (expr is ArrayAccessExpr)
            {
                return VisitArrayAccessExpr(context, (ArrayAccessExpr)expr);
            }
            else if (expr is ClassExpr)
            {
                return VisitClassExpression(context, (ClassExpr)expr);
            }
            else if (expr is ArrayCreationExpr)
            {
                return VisitArrayCreationExpression(context, (ArrayCreationExpr)expr);
            }

            throw new NotImplementedException("Expression translation not implemented for " + expr.GetType().Name);
        }

        private static ExpressionSyntax VisitArrayCreationExpression(ConversionContext context, ArrayCreationExpr arrayExpr)
        {
            var type = ConvertType(arrayExpr.getType().toString());

            var rankDimensions = arrayExpr.getDimensions().ToList<Expression>();

            var initializer = arrayExpr.getInitializer();

            var rankSyntaxes = new List<ExpressionSyntax>();

            if (rankDimensions != null)
            {
                foreach (var dimension in rankDimensions)
                {
                    var rankSyntax = VisitExpression(context, dimension);
                    rankSyntaxes.Add(rankSyntax);
                }
            }

            if (initializer == null)
                return Syntax.ArrayCreationExpression(Syntax.ArrayType(Syntax.ParseTypeName(type)))
                    .AddTypeRankSpecifiers(Syntax.ArrayRankSpecifier(Syntax.SeparatedList(rankSyntaxes, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), rankSyntaxes.Count - 1))));

            // todo: support multi-dimensional and jagged arrays

            var values = initializer.getValues().ToList<Expression>();

            var syntaxes = new List<ExpressionSyntax>();

            foreach (var value in values)
            {
                var syntax = VisitExpression(context, value);
                syntaxes.Add(syntax);
            }

            var initSyntax = Syntax.InitializerExpression(SyntaxKind.ArrayInitializerExpression, Syntax.SeparatedList(syntaxes, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), syntaxes.Count - 1)));

            return Syntax.ArrayCreationExpression(Syntax.ArrayType(Syntax.ParseTypeName(type)), initSyntax);
        }

        private static ExpressionSyntax VisitClassExpression(ConversionContext context, ClassExpr classExpr)
        {
            var type = ConvertType(classExpr.getType().toString());

            return Syntax.TypeOfExpression(Syntax.ParseTypeName(type));
        }

        private static ExpressionSyntax VisitArrayAccessExpr(ConversionContext context, ArrayAccessExpr arrayAccessExpr)
        {
            var nameExpr = arrayAccessExpr.getName();
            var nameSyntax = VisitExpression(context, nameExpr);

            var indexExpr = arrayAccessExpr.getIndex();
            var indexSyntax = VisitExpression(context, indexExpr);

            return Syntax.ElementAccessExpression(nameSyntax, Syntax.BracketedArgumentList(Syntax.SeparatedList(Syntax.Argument(indexSyntax))));
        }
        
        private static ExpressionSyntax VisitInstanceOfExpr(ConversionContext context, InstanceOfExpr instanceOfExpr)
        {
            var expr = instanceOfExpr.getExpr();
            var exprSyntax = VisitExpression(context, expr);

            var type = ConvertType(instanceOfExpr.getType().toString());

            return Syntax.BinaryExpression(SyntaxKind.IsExpression, exprSyntax, Syntax.IdentifierName(type));
        }

        private static ExpressionSyntax VisitEnclosedExpr(ConversionContext context, EnclosedExpr enclosedExpr)
        {
            var expr = enclosedExpr.getInner();
            var exprSyntax = VisitExpression(context, expr);

            return Syntax.ParenthesizedExpression(exprSyntax);
        }

        private static ExpressionSyntax VisitCastExpr(ConversionContext context, CastExpr castExpr)
        {
            var expr = castExpr.getExpr();
            var exprSyntax = VisitExpression(context, expr);

            var type = ConvertType(castExpr.getType().toString());

            return Syntax.CastExpression(Syntax.ParseTypeName(type), exprSyntax);
        }

        private static ExpressionSyntax VisitUnaryExpr(ConversionContext context, UnaryExpr unaryExpr)
        {
            var expr = unaryExpr.getExpr();
            var exprSyntax = VisitExpression(context, expr);

            var op = unaryExpr.getOperator();
            SyntaxKind kind = SyntaxKind.None;
            bool isPostfix = false;

            if (op == UnaryExpr.Operator.inverse)
                kind = SyntaxKind.BitwiseNotExpression;
            else if (op == UnaryExpr.Operator.negative)
                kind = SyntaxKind.NegateExpression;
            else if (op == UnaryExpr.Operator.not)
                kind = SyntaxKind.LogicalNotExpression;
            else if (op == UnaryExpr.Operator.posDecrement)
            {
                kind = SyntaxKind.PostDecrementExpression;
                isPostfix = true;
            }
            else if (op == UnaryExpr.Operator.posIncrement)
            {
                kind = SyntaxKind.PostIncrementExpression;
                isPostfix = true;
            }
            else if (op == UnaryExpr.Operator.positive)
                kind = SyntaxKind.PlusExpression;
            else if (op == UnaryExpr.Operator.preDecrement)
                kind = SyntaxKind.PreDecrementExpression;
            else if (op == UnaryExpr.Operator.preIncrement)
                kind = SyntaxKind.PreIncrementExpression;

            if (isPostfix)
                return Syntax.PostfixUnaryExpression(kind, exprSyntax);
            else
                return Syntax.PrefixUnaryExpression(kind, exprSyntax);
        }

        private static StatementSyntax VisitVariableDeclarationStatement(ConversionContext context, VariableDeclarationExpr varExpr)
        {
            var type = ConvertType(varExpr.getType().toString());

            var variables = new List<VariableDeclaratorSyntax>();

            foreach (var item in varExpr.getVars().ToList<VariableDeclarator>())
            {
                var id = item.getId();
                string name = id.getName();

                if (id.getArrayCount() > 0)
                {
                    if (!type.EndsWith("[]"))
                        type += "[]";
                    if (name.EndsWith("[]"))
                        name = name.Substring(0, name.Length - 2);
                }

                var initexpr = item.getInit();

                if (initexpr != null)
                {
                    var initsyn = VisitExpression(context, initexpr);
                    var vardeclsyn = Syntax.VariableDeclarator(name).WithInitializer(Syntax.EqualsValueClause(initsyn));
                    variables.Add(vardeclsyn);
                }
                else
                    variables.Add(Syntax.VariableDeclarator(name));
            }

            return Syntax.LocalDeclarationStatement(
                Syntax.VariableDeclaration(Syntax.ParseTypeName(type), Syntax.SeparatedList(variables, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), variables.Count - 1))));
        }

        private static ExpressionSyntax VisitBinaryExpression(ConversionContext context, BinaryExpr binaryExpr)
        {
            var leftExpr = binaryExpr.getLeft();
            var leftSyntax = VisitExpression(context, leftExpr);

            var rightExpr = binaryExpr.getRight();
            var rightSyntax = VisitExpression(context, rightExpr);

            var op = binaryExpr.getOperator();
            SyntaxKind kind = SyntaxKind.None;

            if (op == BinaryExpr.Operator.and)
                kind = SyntaxKind.LogicalAndExpression;
            else if (op == BinaryExpr.Operator.binAnd)
                kind = SyntaxKind.BitwiseAndExpression;
            else if (op == BinaryExpr.Operator.binOr)
                kind = SyntaxKind.BitwiseOrExpression;
            else if (op == BinaryExpr.Operator.divide)
                kind = SyntaxKind.DivideExpression;
            else if (op == BinaryExpr.Operator.equals)
                kind = SyntaxKind.EqualsExpression;
            else if (op == BinaryExpr.Operator.greater)
                kind = SyntaxKind.GreaterThanExpression;
            else if (op == BinaryExpr.Operator.greaterEquals)
                kind = SyntaxKind.GreaterThanOrEqualExpression;
            else if (op == BinaryExpr.Operator.less)
                kind = SyntaxKind.LessThanExpression;
            else if (op == BinaryExpr.Operator.lessEquals)
                kind = SyntaxKind.LessThanOrEqualExpression;
            else if (op == BinaryExpr.Operator.lShift)
                kind = SyntaxKind.LeftShiftExpression;
            else if (op == BinaryExpr.Operator.minus)
                kind = SyntaxKind.SubtractExpression;
            else if (op == BinaryExpr.Operator.notEquals)
                kind = SyntaxKind.NotEqualsExpression;
            else if (op == BinaryExpr.Operator.or)
                kind = SyntaxKind.LogicalOrExpression;
            else if (op == BinaryExpr.Operator.plus)
                kind = SyntaxKind.AddExpression;
            else if (op == BinaryExpr.Operator.remainder)
                kind = SyntaxKind.ModuloExpression;
            else if (op == BinaryExpr.Operator.rSignedShift)
                kind = SyntaxKind.RightShiftExpression;
            else if (op == BinaryExpr.Operator.rUnsignedShift)
            {
                kind = SyntaxKind.RightShiftExpression;
                context.Options.Warning("Use of unsigned right shift in original code; verify correctness.", binaryExpr.getBeginLine());
            }
            else if (op == BinaryExpr.Operator.times)
                kind = SyntaxKind.MultiplyExpression;
            else if (op == BinaryExpr.Operator.xor)
                kind = SyntaxKind.ExclusiveOrExpression;

            return Syntax.BinaryExpression(kind, leftSyntax, rightSyntax);
        }

        private static ExpressionSyntax VisitConditionalExpression(ConversionContext context, ConditionalExpr conditionalExpr)
        {
            var condition = conditionalExpr.getCondition();
            var conditionSyntax = VisitExpression(context, condition);

            var thenExpr = conditionalExpr.getThenExpr();
            var thenSyntax = VisitExpression(context, thenExpr);

            var elseExpr = conditionalExpr.getElseExpr();
            var elseSyntax = VisitExpression(context, elseExpr);

            return Syntax.ConditionalExpression(conditionSyntax, thenSyntax, elseSyntax);
        }

        private static ExpressionSyntax VisitObjectCreationExpression(ConversionContext context, ObjectCreationExpr newExpr)
        {
            var anonBody = newExpr.getAnonymousClassBody().ToList<BodyDeclaration>();

            if (anonBody != null && anonBody.Count > 0)
            {
                return VisitAnonymousClassCreationExpression(context, newExpr, anonBody);
            }

            var scope = newExpr.getScope();
            ExpressionSyntax scopeSyntax = null;

            if (scope != null)
            {
                scopeSyntax = VisitExpression(context, scope);
            }

            // TODO: what to do with scope?

            var type = newExpr.getType();

            var typeSyntax = GetSyntaxFromType(type);

            var args = newExpr.getArgs().ToList<Expression>();            

            if (args == null || args.Count == 0)
                return Syntax.ObjectCreationExpression(typeSyntax).WithArgumentList(Syntax.ArgumentList());

            var argSyntaxes = new List<ArgumentSyntax>();

            foreach (var arg in args)
            {
                var argSyntax = VisitExpression(context, arg);
                argSyntaxes.Add(Syntax.Argument(argSyntax));
            }

            return Syntax.ObjectCreationExpression(typeSyntax, Syntax.ArgumentList(Syntax.SeparatedList(argSyntaxes, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), argSyntaxes.Count - 1))), null);
        }

        private static ExpressionSyntax VisitAnonymousClassCreationExpression(ConversionContext context, ObjectCreationExpr newExpr, List<BodyDeclaration> anonBody)
        {
            string baseTypeName = ConvertType(newExpr.getType().getName());
            string anonTypeName = string.Empty;

            for (int i = 0; i <= 100; i++)
            {
                if (i == 100)
                    throw new InvalidOperationException("Too many anonymous types");

                anonTypeName = string.Format("Anonymous{0}{1}", baseTypeName, i == 0 ? string.Empty : i.ToString());

                if (!context.UsedAnonymousTypeNames.Contains(anonTypeName))
                {
                    context.UsedAnonymousTypeNames.Add(anonTypeName);
                    break; // go with this one
                }
            }

            var classSyntax = Syntax.ClassDeclaration(anonTypeName)
                .AddModifiers(
                    Syntax.Token(SyntaxKind.PrivateKeyword),
                    Syntax.Token(SyntaxKind.SealedKeyword))
                .WithBaseList(Syntax.BaseList(Syntax.SeparatedList(Syntax.ParseTypeName(baseTypeName))));

            var parentField = Syntax.FieldDeclaration(
                Syntax.VariableDeclaration(Syntax.ParseTypeName(context.LastTypeName)).AddVariables(Syntax.VariableDeclarator("parent")))
                .AddModifiers(Syntax.Token(SyntaxKind.PrivateKeyword), Syntax.Token(SyntaxKind.ReadOnlyKeyword));

            var ctorSyntax = Syntax.ConstructorDeclaration(anonTypeName)
                .AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(Syntax.Parameter(Syntax.ParseToken("parent")).WithType(Syntax.ParseTypeName(context.LastTypeName)))
                .AddBodyStatements(Syntax.ExpressionStatement(Syntax.BinaryExpression(SyntaxKind.AssignExpression, Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression, Syntax.ThisExpression(), Syntax.IdentifierName("parent")), Syntax.IdentifierName("parent"))));
            
            classSyntax = classSyntax.AddMembers(ctorSyntax, parentField);

            foreach (var member in anonBody)
            {
                if (member is FieldDeclaration)
                {
                    classSyntax = VisitFieldDeclaration(context, classSyntax, (FieldDeclaration)member);
                }
                else if (member is MethodDeclaration)
                {
                    classSyntax = VisitMethodDeclaration(context, classSyntax, (MethodDeclaration)member);
                }
            }

            context.PendingAnonymousTypes.Enqueue(classSyntax);

            var args = newExpr.getArgs().ToList<Expression>();

            if (args == null || args.Count == 0)
                return Syntax.ObjectCreationExpression(Syntax.ParseTypeName(anonTypeName))
                    .AddArgumentListArguments(Syntax.Argument(Syntax.ThisExpression()));

            var argSyntaxes = new List<ArgumentSyntax>();

            argSyntaxes.Add(Syntax.Argument(Syntax.ThisExpression()));

            foreach (var arg in args)
            {
                var argSyntax = VisitExpression(context, arg);
                argSyntaxes.Add(Syntax.Argument(argSyntax));
            }

            return Syntax.ObjectCreationExpression(Syntax.ParseTypeName(anonTypeName), Syntax.ArgumentList(Syntax.SeparatedList(argSyntaxes, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), argSyntaxes.Count - 1))), null);
        }

        private static ExpressionSyntax VisitFieldAccessExpression(ConversionContext context, FieldAccessExpr fieldAccessExpr)
        {
            var scope = fieldAccessExpr.getScope();
            ExpressionSyntax scopeSyntax = null;

            if (scope != null)
            {
                scopeSyntax = VisitExpression(context, scope);
            }

            var field = fieldAccessExpr.getField();

            return Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression, scopeSyntax, Syntax.IdentifierName(field));
        }

        private static ExpressionSyntax VisitMethodCallExpression(ConversionContext context, MethodCallExpr methodCallExpr)
        {
            var scope = methodCallExpr.getScope();
            ExpressionSyntax scopeSyntax = null;

            if (scope != null)
            {
                scopeSyntax = VisitExpression(context, scope);
            }

            var methodName = Capitalize(methodCallExpr.getName());
            methodName = ReplaceCommonMethodNames(methodName);

            ExpressionSyntax methodExpression;

            if (scopeSyntax == null)
            {
                methodExpression = Syntax.IdentifierName(methodName);
            }
            else
            {
                methodExpression = Syntax.MemberAccessExpression(SyntaxKind.MemberAccessExpression, scopeSyntax, Syntax.IdentifierName(methodName));
            }

            var args = methodCallExpr.getArgs().ToList<Expression>();

            if (args == null || args.Count == 0)
                return Syntax.InvocationExpression(methodExpression);

            var argSyntaxes = new List<ArgumentSyntax>();

            foreach (var arg in args)
            {
                var argSyntax = VisitExpression(context, arg);
                argSyntaxes.Add(Syntax.Argument(argSyntax));
            }

            return Syntax.InvocationExpression(methodExpression, Syntax.ArgumentList(Syntax.SeparatedList(argSyntaxes, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), argSyntaxes.Count - 1))));
        }

        private static ExpressionSyntax VisitNameExpression(NameExpr nameExpr)
        {
            return Syntax.IdentifierName(nameExpr.getName());
        }

        private static StatementSyntax VisitTryStatement(ConversionContext context, TryStmt tryStmt)
        {
            var tryBlock = tryStmt.getTryBlock();
            var tryStatements = tryBlock.getStmts().ToList<Statement>();

            var tryConverted = VisitStatements(context, tryStatements);

            var catches = tryStmt.getCatchs().ToList<CatchClause>();

            var trySyn = Syntax.TryStatement()
                .AddBlockStatements(tryConverted.ToArray());

            if (catches != null)
            {
                foreach (var ctch in catches)
                {
                    var types = ctch.getExcept().getTypes().ToList<ReferenceType>();
                    var block = ctch.getCatchBlock();
                    var catchStatements = block.getStmts().ToList<Statement>();
                    var catchConverted = VisitStatements(context, catchStatements);
                    var catchBlockSyntax = Syntax.Block(catchConverted);

                    var type = ConvertType(types[0].getType().ToString());

                    trySyn = trySyn.AddCatches(Syntax.CatchClause(Syntax.CatchDeclaration(Syntax.ParseTypeName(type), Syntax.ParseToken(ctch.getExcept().getId().toString())), catchBlockSyntax));
                }
            }

            var finallyBlock = tryStmt.getFinallyBlock();

            if (finallyBlock != null)
            {
                var finallyStatements = finallyBlock.getStmts().ToList<Statement>();
                var finallyConverted = VisitStatements(context, finallyStatements);
                var finallyBlockSyntax = Syntax.Block(finallyConverted);

                trySyn = trySyn.WithFinally(Syntax.FinallyClause(finallyBlockSyntax));
            }

            return trySyn;
        }

        private static ClassDeclarationSyntax VisitFieldDeclaration(ConversionContext context, ClassDeclarationSyntax classSyntax, FieldDeclaration fieldDecl)
        {
            var variables = new List<VariableDeclaratorSyntax>();
                        
            string typeName = fieldDecl.getType().toString();

            foreach (var item in fieldDecl.getVariables().ToList<VariableDeclarator>())
            {
                var id = item.getId();
                string name = id.getName();

                if (id.getArrayCount() > 0)
                {
                    if (!typeName.EndsWith("[]"))
                        typeName += "[]";
                    if (name.EndsWith("[]"))
                        name = name.Substring(0, name.Length - 2);
                }

                var initexpr = item.getInit();

                if (initexpr != null)
                {
                    var initsyn = VisitExpression(context, initexpr);
                    var vardeclsyn = Syntax.VariableDeclarator(name).WithInitializer(Syntax.EqualsValueClause(initsyn));
                    variables.Add(vardeclsyn);
                }
                else
                    variables.Add(Syntax.VariableDeclarator(name));
            }

            typeName = ConvertType(typeName);

            var fieldSyntax = Syntax.FieldDeclaration(
                Syntax.VariableDeclaration(
                    Syntax.ParseTypeName(typeName),
                    Syntax.SeparatedList(variables, Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), variables.Count - 1))));

            var mods = fieldDecl.getModifiers();

            if (mods.HasFlag(Modifier.PUBLIC))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.PublicKeyword));
            if (mods.HasFlag(Modifier.PROTECTED))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.ProtectedKeyword));
            if (mods.HasFlag(Modifier.PRIVATE))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.PrivateKeyword));
            if (mods.HasFlag(Modifier.STATIC))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.StaticKeyword));
            if (mods.HasFlag(Modifier.FINAL))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.ReadOnlyKeyword));
            if (mods.HasFlag(Modifier.VOLATILE))
                fieldSyntax = fieldSyntax.AddModifiers(Syntax.Token(SyntaxKind.VolatileKeyword));

            classSyntax = classSyntax.AddMembers(fieldSyntax);
            return classSyntax;
        }

        private static string ConvertType(string typeName)
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

        private static string Capitalize(string name)
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

        private static string ConvertIdentifierName(string name)
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
                    return name + "_renamed";
                default:
                    return name;
            }
        }

        private static string ReplaceCommonMethodNames(string name)
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

        private static TypeSyntax GetSyntaxFromType(ClassOrInterfaceType type, bool addI = false)
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
