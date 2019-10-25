﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;


namespace Microsoft.Quantum.QsCompiler.Documentation.Testing
{
    using ArgDeclType = LocalVariableDeclaration<QsLocalSymbol>;
    using QsType = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using SigTypeTuple = Tuple<ResolvedType, ResolvedType>;

    public class DocParsingTests
    {
        private readonly Tuple<QsPositionInfo, QsPositionInfo> EmptyRange = 
            new Tuple<QsPositionInfo, QsPositionInfo>(QsPositionInfo.Zero, QsPositionInfo.Zero);
        private readonly QsLocation ZeroLocation = new QsLocation(new Tuple<int, int>(0, 0), 
                                                new Tuple<QsPositionInfo, QsPositionInfo>(QsPositionInfo.Zero, QsPositionInfo.Zero));
        private readonly NonNullable<string> CanonName = NonNullable<string>.New("Microsoft.Quantum.Canon");

        private QsQualifiedName MakeFullName(string name)
        {
            return new QsQualifiedName(CanonName, NonNullable<string>.New(name));
        }

        [Fact]
        public void ParseDocComments()
        {
            string[] comments = { "# Summary",
                                  "Encodes a multi-qubit Pauli operator represented as an array of",
                                  "single-qubit Pauli operators into an integer.",
                                  "",
                                  "Test second paragraph.",
                                  "",
                                  "# Description",
                                  "This is some text",
                                  "",
                                  "This is some more text",
                                  "",
                                  "# Input",
                                  "## paulies",
                                  "An array of at most 31 single-qubit Pauli operators.",
                                  "",
                                  "# Output",
                                  "An integer uniquely identifying `paulies`, as described below.",
                                  "",
                                  "# Remarks",
                                  "Each Pauli operator can be encoded using two bits:",
                                  "$$",
                                  "\\begin{align}",
                                  "\\boldone \\mapsto 00, \\quad X \\mapsto 01, \\quad Y \\mapsto 11,",
                                  "\\quad Z \\mapsto 10.",
                                  "\\end{align}",
                                  "$$",
                                  "",
                                  "Given an array of Pauli operators `[P0, ..., Pn]`, this function returns an",
                                  "integer with binary expansion formed by concatenating",
                                  "the mappings of each Pauli operator in big-endian order",
                                  "`bits(Pn) ... bits(P0)`." };
            var dc = new DocComment(comments);
            Assert.Equal(comments[1] + "\r" + comments[2] + "\r" + comments[3] + "\r" + comments[4], dc.Summary);
            Assert.Equal(comments[1] + "\r" + comments[2], dc.ShortSummary);
            Assert.Equal(comments[1] + "\r" + comments[2], dc.FullSummary);
            Assert.Equal(comments[1] + "\r" + comments[2] + "\r" + comments[3] + "\r" + comments[4] + "\r" + comments[5] + "\r" + comments[7] + "\r" + comments[8] + "\r" + comments[9], dc.Documentation);
            Assert.Single(dc.Input);
            Assert.Equal(comments[13], dc.Input["paulies"]);
            Assert.Equal(comments[16], dc.Output);
            Assert.Empty(dc.TypeParameters);
            Assert.Equal("", dc.Example);
            var remarks = String.Join("\r", comments.AsSpan(19, 12).ToArray());
            Assert.Equal(remarks, dc.Remarks);
        }

        [Fact]
        public void ParseUdt()
        {
            string[] comments = 
            {
                "# Summary",
                "Represents a single primitive term in the set of all dynamical generators, e.g.",
                "Hermitian operators, for which there exists a map from that generator",
                "to time-evolution by that that generator, through \"EvolutionSet\".",
                "",
                "# Description",
                "The first element",
                "(Int[], Double[]) is indexes that single term -- For instance, the Pauli string",
                "XXY with coefficient 0.5 would be indexed by ([1,1,2], [0.5]). Alternatively,",
                "Hamiltonians parameterized by a continuous variable, such as X cos φ + Y sin φ,",
                "might for instance be represented by ([], [φ]). The second",
                "element indexes the subsystem on which the generator acts on.",
                "",
                "# Remarks",
                "> [!WARNING]",
                "> The interpretation of an `GeneratorIndex` is not defined except",
                "> with reference to a particular set of generators.",
                "",
                "# Example",
                "Using  <xref:microsoft.quantum.canon.paulievolutionset>, the operator",
                "$\\pi X_2 X_5 Y_9$ is represented as:",
                "```qsharp",
                "let index = GeneratorIndex(([1; 1; 2], [PI()]), [2; 5; 9]);",
                "```",
                "",
                "# See Also",
                "- @\"microsoft.quantum.canon.paulievolutionset\"",
                "- @\"microsoft.quantum.canon.evolutionset\""
            };
            string expected = @"### YamlMime:QSharpType
uid: microsoft.quantum.canon.generatorindex
name: GeneratorIndex
type: newtype
namespace: Microsoft.Quantum.Canon
summary: |-
  Represents a single primitive term in the set of all dynamical generators, e.g.
  Hermitian operators, for which there exists a map from that generator
  to time-evolution by that that generator, through ""EvolutionSet"".

  The first element
  (Int[], Double[]) is indexes that single term -- For instance, the Pauli string
  XXY with coefficient 0.5 would be indexed by ([1,1,2], [0.5]). Alternatively,
  Hamiltonians parameterized by a continuous variable, such as X cos φ + Y sin φ,
  might for instance be represented by ([], [φ]). The second
  element indexes the subsystem on which the generator acts on.
remarks: |-
  > [!WARNING]
  > The interpretation of an `GeneratorIndex` is not defined except
  > with reference to a particular set of generators.

  ### Examples
  Using  <xref:microsoft.quantum.canon.paulievolutionset>, the operator
  $\pi X_2 X_5 Y_9$ is represented as:

  ```qsharp
  let index = GeneratorIndex(([1; 1; 2], [PI()]), [2; 5; 9]);
  ```
syntax: newtype GeneratorIndex = ((Int[], Double[]), Int[]);
seeAlso:
- microsoft.quantum.canon.paulievolutionset
- microsoft.quantum.canon.evolutionset
...
";
            var intArrayType = ResolvedType.New(QsType.NewArrayType(ResolvedType.New(QsType.Int)));
            var doubleArrayType = ResolvedType.New(QsType.NewArrayType(ResolvedType.New(QsType.Double)));
            var innerTuple = new ResolvedType[] { intArrayType, doubleArrayType }.ToImmutableArray();
            var innerTupleType = ResolvedType.New(QsType.NewTupleType(innerTuple));
            var baseTuple = new ResolvedType[] { innerTupleType, intArrayType }.ToImmutableArray();
            var baseType = ResolvedType.New(QsType.NewTupleType(baseTuple));
            var anonymousItem = QsTuple<QsTypeItem>.NewQsTupleItem(QsTypeItem.NewAnonymous(baseType));
            var typeItems = QsTuple<QsTypeItem>.NewQsTuple(ImmutableArray.Create(anonymousItem));

            var generatorIndexType = new QsCustomType(MakeFullName("GeneratorIndex"),
                                                      ImmutableArray<QsDeclarationAttribute>.Empty,
                                                      NonNullable<string>.New("GeneratorRepresentation.qs"),
                                                      ZeroLocation,
                                                      baseType,
                                                      typeItems,
                                                      comments.ToImmutableArray(),
                                                      QsComments.Empty);
            var udt = new DocUdt("Microsoft.Quantum.Canon", generatorIndexType);

            var stream = new StringWriter();
            udt.WriteToFile(stream);
            var s = stream.ToString();
            Assert.Equal(expected, s);
        }

        [Fact]
        public void ParseOp()
        {
            ArgDeclType BuildArgument(string name, ResolvedType t)
            {
                var validName = QsLocalSymbol.NewValidName(NonNullable<string>.New(name));
                var info = new InferredExpressionInformation(false, false);
                return new ArgDeclType(validName, t, info, QsNullable<Tuple<int, int>>.Null, EmptyRange);
            }

            string[] comments =
            {
                "# Summary",
                "Convenience function that performs state preparation by applying a ",
                "`statePrepUnitary` on the input state, followed by adiabatic state ",
                "preparation using a `adiabaticUnitary`, and finally phase estimation ",
                "with respect to `qpeUnitary`on the resulting state using a ",
                "`phaseEstAlgorithm`.",
                "",
                "# Input",
                "## statePrepUnitary",
                "An oracle representing state preparation for the initial dynamical",
                "generator.",
                "## adiabaticUnitary",
                "An oracle representing the adiabatic evolution algorithm to be used",
                "to implement the sweeps to the final state of the algorithm.",
                "## qpeUnitary",
                "An oracle representing a unitary operator $U$ representing evolution",
                "for time $\\delta t$ under a dynamical generator with ground state",
                "$\\ket{\\phi}$ and ground state energy $E = \\phi\\\\,\\delta t$.",
                "## phaseEstAlgorithm",
                "An operation that performs phase estimation on a given unitary operation.",
                "See [iterative phase estimation](/quantum/libraries/characterization#iterative-phase-estimation)",
                "for more details.",
                "## qubits",
                "A register of qubits to be used to perform the simulation.",
                "",
                "# Output",
                "An estimate $\\hat{\\phi}$ of the ground state energy $\\phi$",
                "of the generator represented by $U$."
            };
            string expected = @"### YamlMime:QSharpType
uid: microsoft.quantum.canon.adiabaticstateenergyunitary
name: AdiabaticStateEnergyUnitary
type: operation
namespace: Microsoft.Quantum.Canon
summary: |-
  Convenience function that performs state preparation by applying a
  `statePrepUnitary` on the input state, followed by adiabatic state
  preparation using a `adiabaticUnitary`, and finally phase estimation
  with respect to `qpeUnitary`on the resulting state using a
  `phaseEstAlgorithm`.
syntax: 'operation AdiabaticStateEnergyUnitary (statePrepUnitary : (Qubit[] => Unit), adiabaticUnitary : (Qubit[] => Unit), qpeUnitary : (Qubit[] => Unit is Adj + Ctl), phaseEstAlgorithm : ((Microsoft.Quantum.Canon.DiscreteOracle, Qubit[]) => Double), qubits : Qubit[]) : Double'
input:
  content: '(statePrepUnitary : (Qubit[] => Unit), adiabaticUnitary : (Qubit[] => Unit), qpeUnitary : (Qubit[] => Unit is Adj + Ctl), phaseEstAlgorithm : ((Microsoft.Quantum.Canon.DiscreteOracle, Qubit[]) => Double), qubits : Qubit[])'
  types:
  - name: statePrepUnitary
    summary: |-
      An oracle representing state preparation for the initial dynamical
      generator.
    isOperation: true
    input:
      types:
      - isArray: true
        isPrimitive: true
        uid: Qubit
    output:
      types:
      - isPrimitive: true
        uid: Unit
  - name: adiabaticUnitary
    summary: |-
      An oracle representing the adiabatic evolution algorithm to be used
      to implement the sweeps to the final state of the algorithm.
    isOperation: true
    input:
      types:
      - isArray: true
        isPrimitive: true
        uid: Qubit
    output:
      types:
      - isPrimitive: true
        uid: Unit
  - name: qpeUnitary
    summary: |-
      An oracle representing a unitary operator $U$ representing evolution
      for time $\delta t$ under a dynamical generator with ground state
      $\ket{\phi}$ and ground state energy $E = \phi\\,\delta t$.
    isOperation: true
    input:
      types:
      - isArray: true
        isPrimitive: true
        uid: Qubit
    output:
      types:
      - isPrimitive: true
        uid: Unit
    functors:
    - Adjoint
    - Controlled
  - name: phaseEstAlgorithm
    summary: |-
      An operation that performs phase estimation on a given unitary operation.
      See [iterative phase estimation](/quantum/libraries/characterization#iterative-phase-estimation)
      for more details.
    isOperation: true
    input:
      types:
      - uid: microsoft.quantum.canon.discreteoracle
      - isArray: true
        isPrimitive: true
        uid: Qubit
    output:
      types:
      - isPrimitive: true
        uid: Double
  - name: qubits
    summary: A register of qubits to be used to perform the simulation.
    isArray: true
    isPrimitive: true
    uid: Qubit
output:
  content: Double
  types:
  - summary: |-
      An estimate $\hat{\phi}$ of the ground state energy $\phi$
      of the generator represented by $U$.
    isPrimitive: true
    uid: Double
...
";

            var qubitArrayType = ResolvedType.New(QsType.NewArrayType(ResolvedType.New(QsType.Qubit)));
            var unitType = ResolvedType.New(QsType.UnitType);
            var doubleType = ResolvedType.New(QsType.Double);
            var oracleType = ResolvedType.New(QsType.NewUserDefinedType(new UserDefinedType(CanonName, 
                                                                        NonNullable<string>.New("DiscreteOracle"), 
                                                                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null)));
            var noInfo = CallableInformation.NoInformation;
            var acFunctors = ResolvedCharacteristics.FromProperties(new[] { OpProperty.Adjointable, OpProperty.Controllable });
            var acInfo = new CallableInformation(acFunctors, InferredCallableInformation.NoInformation); 
            var qubitToUnitOp = ResolvedType.New(QsType.NewOperation(new SigTypeTuple(qubitArrayType, unitType), noInfo));
            var qubitToUnitOpAC = ResolvedType.New(QsType.NewOperation(new SigTypeTuple(qubitArrayType, unitType), acInfo));
            var phaseEstArgs = new ResolvedType[] { oracleType, qubitArrayType }.ToImmutableArray();
            var phaseEstArgTuple = ResolvedType.New(QsType.NewTupleType(phaseEstArgs));
            var phaseEstOp = ResolvedType.New(QsType.NewOperation(new SigTypeTuple(phaseEstArgTuple, doubleType), noInfo));

            var typeParams = new QsLocalSymbol[] { }.ToImmutableArray();
            var argTypes = new ResolvedType[] { qubitToUnitOp, qubitToUnitOp, qubitToUnitOpAC, phaseEstOp, qubitArrayType }.ToImmutableArray();
            var argTupleType = ResolvedType.New(QsType.NewTupleType(argTypes));
            var signature = new ResolvedSignature(typeParams, argTupleType, doubleType, noInfo);

            var args = new List<ArgDeclType> { BuildArgument("statePrepUnitary", qubitToUnitOp),
                                               BuildArgument("adiabaticUnitary", qubitToUnitOp),
                                               BuildArgument("qpeUnitary", qubitToUnitOpAC),
                                               BuildArgument("phaseEstAlgorithm", phaseEstOp),
                                               BuildArgument("qubits", qubitArrayType) }
                            .ConvertAll(arg => QsTuple<ArgDeclType>.NewQsTupleItem(arg))
                            .ToImmutableArray();
            var argTuple = QsTuple<ArgDeclType>.NewQsTuple(args);
            var specs = new QsSpecialization[] { }.ToImmutableArray();

            var qsCallable = new QsCallable(QsCallableKind.Operation,
                                            MakeFullName("AdiabaticStateEnergyUnitary"),
                                            ImmutableArray<QsDeclarationAttribute>.Empty,
                                            NonNullable<string>.New("Techniques.qs"),
                                            ZeroLocation,
                                            signature,
                                            argTuple,
                                            specs,
                                            comments.ToImmutableArray(),
                                            QsComments.Empty);
            var callable = new DocCallable("Microsoft.Quantum.Canon", qsCallable);

            var stream = new StringWriter();
            callable.WriteToFile(stream);
            var s = stream.ToString();
            Assert.Equal(expected, s);
        }

        [Fact]
        public void ParseDeprecated()
        {
            string[] comments = { "# Summary",
                                  "This is some text",
                                  "# Deprecated",
                                  "Some other text"
                                };
            string dep = "newName";
            string warning = "> [!WARNING]\n> Deprecated\n";
            string warningText = "name has been deprecated. Please use @\"newName\" instead.";

            // Test with just the Deprecated comment section
            var dc = new DocComment(comments);
            Assert.Equal(comments[1] + "\r" + warning + "\r" + comments[3], dc.Summary);
            Assert.Equal(warning + "\r" + comments[3], dc.ShortSummary);
            Assert.Equal(comments[3], dc.Documentation);

            // Test with just the deprecated attribute
            dc = new DocComment(comments.Take(2), "name", true, dep);
            Assert.Equal(comments[1] + "\r" + warning + "\r" + warningText, dc.Summary);
            Assert.Equal(warning + "\r" + warningText, dc.ShortSummary);
            Assert.Equal(warningText, dc.Documentation);

            // Test with both
            dc = new DocComment(comments, "name", true, dep);
            Assert.Equal(comments[1] + "\r" + warning + "\r" + warningText + "\r" + comments[3], dc.Summary);
            Assert.Equal(warning + "\r" + warningText, dc.ShortSummary);
            Assert.Equal(warningText, dc.Documentation);
        }
    }
}
