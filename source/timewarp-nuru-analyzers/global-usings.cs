global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Syntax;
global using Microsoft.CodeAnalysis.Diagnostics;
global using System;
global using System.Collections.Generic;
global using System.Collections.Immutable;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using TimeWarp.Nuru;
global using TimeWarp.Nuru.Generators;

// Allow MSBuild task project to access internal extractors
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TimeWarp.Nuru.Build")]
