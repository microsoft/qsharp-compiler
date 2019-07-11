// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using YamlDotNet.RepresentationModel;


namespace Microsoft.Quantum.QsCompiler.Documentation
{
    /// <summary>
    /// Class to represent the documentation YAML file for a callable (operation or function).
    /// </summary>
    internal class DocCallable : DocItem
    {
        private readonly string syntax;
        private readonly string inputContent;
        private readonly string outputType;
        private readonly List<string> functors = new List<string>();
        private readonly QsCallable callable;

        /// <summary>
        /// Constructs a DocCallable instance given a namespace name and a callable object.
        /// </summary>
        /// <param name="ns">The name of the namespace where this callable is defined</param>
        /// <param name="callableObj">The compiled callable</param>
        internal DocCallable(string ns, QsCallable callableObj) 
            : base(ns, callableObj.FullName.Name.Value, 
                  callableObj.Kind.IsFunction ? Utils.FunctionKind : Utils.OperationKind, 
                  callableObj.Documentation)
        {
            syntax = Utils.CallableToSyntax(callableObj);
            inputContent = Utils.CallableToArguments(callableObj);
            outputType = Utils.ResolvedTypeToString(callableObj.Signature.ReturnType);
            functors = new List<string>();
            foreach (var functor in callableObj.Signature.Information.Characteristics.SupportedFunctors.ValueOr(ImmutableHashSet<QsFunctor>.Empty))
            {
                if (functor.IsAdjoint)
                {
                    functors.Add(Functors.Adjoint);
                }
                else if (functor.IsControlled)
                {
                    functors.Add(Functors.Controlled);
                }
            }
            callable = callableObj;
        }

        /// <summary>
        /// Writes a YAML representation of this callable to a text writer.
        /// </summary>
        /// <param name="text">The writer to output to</param>
        internal override void WriteToFile(TextWriter text)
        {
            YamlNode BuildInputNode()
            {
                var inputNode = new YamlMappingNode();

                inputNode.AddStringMapping(Utils.ContentsKey, inputContent);

                var typesNode = new YamlSequenceNode();
                inputNode.Add(Utils.TypesListKey, typesNode);

                foreach (var declaration in SyntaxGenerator.ExtractItems(callable.ArgumentTuple))
                {
                    var argNode = new YamlMappingNode();
                    var argName = ((QsLocalSymbol.ValidName)declaration.VariableName).Item.Value;
                    argNode.AddStringMapping(Utils.NameKey, argName);
                    if (comments.Input.TryGetValue(argName, out string summary))
                    {
                        argNode.AddStringMapping(Utils.SummaryKey, summary);
                    }
                    Utils.ResolvedTypeToYaml(declaration.Type, argNode);
                    typesNode.Add(argNode);
                }

                return inputNode;
            }

            YamlNode BuildOutputNode()
            {
                var outputNode = new YamlMappingNode();

                outputNode.AddStringMapping(Utils.ContentsKey, outputType);

                var typesNode = new YamlSequenceNode();
                outputNode.Add(Utils.TypesListKey, typesNode);

                var outputTypeNode = new YamlMappingNode();
                typesNode.Add(outputTypeNode);

                if (!string.IsNullOrEmpty(this.comments.Output))
                {
                    outputTypeNode.AddStringMapping(Utils.SummaryKey, this.comments.Output);
                }
                Utils.ResolvedTypeToYaml(callable.Signature.ReturnType, outputTypeNode);

                return outputNode;
            }

            var rootNode = new YamlMappingNode();
            rootNode.AddStringMapping(Utils.UidKey, uid);
            rootNode.AddStringMapping(Utils.NameKey, name);
            rootNode.AddStringMapping(Utils.TypeKey, itemType);
            rootNode.AddStringMapping(Utils.NamespaceKey, namespaceName);
            if (!string.IsNullOrWhiteSpace(comments.Documentation))
            {
                rootNode.AddStringMapping(Utils.SummaryKey, comments.Documentation);
            }
            if (!string.IsNullOrWhiteSpace(comments.Remarks))
            {
                rootNode.AddStringMapping(Utils.RemarksKey, comments.Remarks);
            }
            if (!string.IsNullOrWhiteSpace(comments.Example))
            {
                rootNode.AddStringMapping(Utils.ExamplesKey, comments.Example);
            }
            rootNode.AddStringMapping(Utils.SyntaxKey, syntax);
            if (!string.IsNullOrWhiteSpace(comments.References))
            {
                rootNode.AddStringMapping(Utils.ReferencesKey, comments.References);
            }
            rootNode.Add(Utils.InputKey, BuildInputNode());
            rootNode.Add(Utils.OutputKey, BuildOutputNode());
            if (comments.TypeParameters.Count > 0)
            {
                rootNode.Add(Utils.TypeParamsKey, Utils.BuildSequenceMappingNode(comments.TypeParameters));
            }
            if (functors.Count > 0)
            {
                rootNode.Add(Utils.FunctorsKey, Utils.BuildSequenceNode(functors));
            }
            if (comments.SeeAlso.Count > 0)
            {
                rootNode.Add(Utils.SeeAlsoKey, Utils.BuildSequenceNode(comments.SeeAlso));
            }

            var doc = new YamlDocument(rootNode);
            var stream = new YamlStream(doc);

            text.WriteLine("### " + Utils.QsYamlMime);
            stream.Save(text, false);
        }
    }
}
