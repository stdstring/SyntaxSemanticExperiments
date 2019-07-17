﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceCheckUtil.Analyzers
{
    internal class VirtualInheritanceAnalyzer : IFileAnalyzer
    {
        public VirtualInheritanceAnalyzer(TextWriter output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            _output = output;
        }

        public Boolean Process(String filename, SyntaxTree tree, SemanticModel model)
        {
            _output.WriteLine($"Execution of VirtualInheritanceAnalyzer started");
            VirtualInterfaceInheritanceDetector detector = new VirtualInterfaceInheritanceDetector(model);
            detector.Visit(tree.GetRoot());
            Boolean hasErrors = Process(detector.Data);
            _output.WriteLine($"Execution of VirtualInheritanceAnalyzer finished");
            return !hasErrors;
        }

        private Boolean Process(IList<String> errors)
        {
            Console.WriteLine($"Found {errors.Count} base non-system interfaces not marked for virtual inheritance in the ported C++ code");
            foreach (String error in errors)
            {
                Console.WriteLine($"[ERROR]: Found base non-system interface named {error} not marked for virtual inheritance in the ported C++ code");
            }
            return errors.Count > 0;
        }

        private readonly TextWriter _output;

        private class VirtualInterfaceInheritanceDetector : CSharpSyntaxWalker
        {
            public VirtualInterfaceInheritanceDetector(SemanticModel model)
            {
                Model = model;
                Data = new List<String>();
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                INamedTypeSymbol type = Model.GetDeclaredSymbol(node);
                IList<INamedTypeSymbol> baseInterfaces = new List<INamedTypeSymbol>();
                CollectBaseInterfaces(type.Interfaces, baseInterfaces);
                ProcessBaseInterfaces(baseInterfaces);
                base.VisitClassDeclaration(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                INamedTypeSymbol type = Model.GetDeclaredSymbol(node);
                IList<INamedTypeSymbol> baseInterfaces = new List<INamedTypeSymbol>();
                CollectBaseInterfaces(type.Interfaces, baseInterfaces);
                ProcessBaseInterfaces(baseInterfaces);
                base.VisitStructDeclaration(node);
            }

            public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                INamedTypeSymbol type = Model.GetDeclaredSymbol(node);
                IList<INamedTypeSymbol> baseInterfaces = new List<INamedTypeSymbol>();
                CollectBaseInterfaces(type, baseInterfaces);
                ProcessBaseInterfaces(baseInterfaces);
                base.VisitInterfaceDeclaration(node);
            }

            public SemanticModel Model { get; }

            public IList<String> Data { get; }

            // TODO (std_string) : create non-recursive version
            private void CollectBaseInterfaces(IList<INamedTypeSymbol> symbols, IList<INamedTypeSymbol> dest)
            {
                foreach (INamedTypeSymbol symbol in symbols)
                    CollectBaseInterfaces(symbol, dest);
            }

            private void CollectBaseInterfaces(INamedTypeSymbol symbol, IList<INamedTypeSymbol> dest)
            {
                if (symbol.Interfaces.Length == 0)
                    dest.Add(symbol);
                CollectBaseInterfaces(symbol.Interfaces, dest);
            }

            private void ProcessBaseInterfaces(IList<INamedTypeSymbol> baseInterfaces)
            {
                IList<INamedTypeSymbol> systemBaseInterfaces = baseInterfaces.Where(IsSystemType).ToList();
                IList<INamedTypeSymbol> nonSystemBaseInterfaces = baseInterfaces.Where(i => !IsSystemType(i)).ToList();
                if (nonSystemBaseInterfaces.Count == 0)
                    return;
                if (systemBaseInterfaces.Count == 0 && nonSystemBaseInterfaces.Count == 1)
                    return;
                foreach (INamedTypeSymbol type in nonSystemBaseInterfaces.Where(i => !HasVirtualInheritanceAttr(i)))
                {
                    String typename = type.ToDisplayString();
                    if (!Data.Contains(typename))
                        Data.Add(typename);
                }
            }

            // TODO (std_string) : probably move into extension methods
            private Boolean IsSystemType(INamedTypeSymbol type)
            {
                // TODO (std_string) : think about approach
                return type.ToDisplayString().StartsWith(SystemPrefix);
            }

            // TODO (std_string) : probably move into extension methods
            private Boolean HasVirtualInheritanceAttr(INamedTypeSymbol type)
            {
                return type.GetAttributes().Any(attr => String.Equals(attr.AttributeClass.ToDisplayString(), VirtualInheritanceAttribute));
            }

            private const String SystemPrefix = "System.";

            private const String VirtualInheritanceAttribute = "CsToCppPorter.CppVirtualInheritance";
        }
    }
}
