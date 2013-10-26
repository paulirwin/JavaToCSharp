Java to C# Converter
====================

Java to C# converter, work in progress. Uses javaparser with 1.7 
support from https://github.com/before/javaparser, IKVM.NET to 
convert the javaparser .jar into a .NET .dll, and the Roslyn CTP
for C# AST generation. 

This was a quick hack only, it is still very much a work in progress.
Pull requests and issue submission welcome.

Getting Started
===============

Clone the repo, build, and launch the Gui WPF app. Click the "..." button on
the left side to load a Java file, and then click Convert to convert to
C# on the right side. Please note that conversion may take up to a few
minutes on large files.

Alternatively, launch the command line (Cli) version to process files
from the command line.

MIT License
===========

Copyright (C) 2013, Paul Irwin

Permission is hereby granted, free of charge, to any person obtaining 
a copy of this software and associated documentation files (the "Software"), 
to deal in the Software without restriction, including without limitation 
the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
DEALINGS IN THE SOFTWARE.
