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

License for JavaParser
======================

Java 1.7 parser and Abstract Syntax Tree.

Copyright (C) 2007 Júlio Vilmar Gesser
jgesser@gmail.com
http://code.google.com/p/javaparser/

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.


