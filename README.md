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

## .NET Support

Trunk will generally always target the latest LTS version of .NET for the core library and the CLI/GUI apps.
If you require running the app on a prior version of .NET, please see the historical releases.
Where STS BCL or preview (as of latest LTS SDK) C# language features are used in the converter, they will be options that are disabled by default.
These options may become enabled by default when the next LTS ships, as a major version bump.

.NET Framework is no longer supported and will not be supported going forward.
This includes not just for the core library (i.e. .NET Standard will not be supported) and running the CLI/GUI apps, 
but also for the C# code generation where applicable.
You should be running .NET LTS nowadays anyways (.NET 6+ at the time of writing).

Bug fixes and features may be backported to the latest major version that targets an actively-supported, non-latest .NET LTS,
but only based on community interest.
Please submit an issue if you want a bugfix or feature backported; better yet, please submit a PR.
Only clean cherry-picks or minor merges will be supported for backports; significant/messy merges or PRs will likely not be supported or merged.

## License for JavaParser

Licensed under the Apache License available at https://github.com/javaparser/javaparser/blob/master/LICENSE.APACHE
