# Java to C# Converter

[![.NET Build](https://github.com/paulirwin/JavaToCSharp/actions/workflows/build.yml/badge.svg)](https://github.com/paulirwin/JavaToCSharp/actions/workflows/build.yml) [![Nuget](https://img.shields.io/nuget/v/JavaToCSharp)](https://www.nuget.org/packages/JavaToCSharp/)


Java to C# converter. 
Uses [JavaParser](https://github.com/javaparser/javaparser) to parse the Java source code text, 
[IKVM.NET](https://github.com/ikvmnet/ikvm/) to convert the javaparser .jar into a .NET .dll, 
and [Roslyn](https://github.com/dotnet/roslyn) for C# AST generation. 

Pull requests and issue submission welcome.

## Getting Started

Clone the repo, build, and launch the Gui WPF app. Click the "..." button on
the left side to load a Java file, and then click Convert to convert to
C# on the right side. Please note that conversion may take up to a few
minutes on large files.

Alternatively, launch the command line (Cli) version to process files
from the command line.

The core library is installable via NuGet at https://www.nuget.org/packages/JavaToCSharp/

## License for JavaParser

Licensed under the Apache License available at https://github.com/javaparser/javaparser/blob/master/LICENSE.APACHE
