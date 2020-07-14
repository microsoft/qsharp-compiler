// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.Documentation.Testing
{
    internal static class Utils
    {
        internal static readonly Tuple<QsPositionInfo, QsPositionInfo> EmptyRange =
            new Tuple<QsPositionInfo, QsPositionInfo>(QsPositionInfo.Zero, QsPositionInfo.Zero);

        internal static readonly QsNullable<QsLocation> ZeroLocation =
            QsNullable<QsLocation>.NewValue(new QsLocation(
                new Tuple<int, int>(0, 0),
                new Tuple<QsPositionInfo, QsPositionInfo>(QsPositionInfo.Zero, QsPositionInfo.Zero)));

        internal static readonly NonNullable<string> CanonName = NonNullable<string>.New("Microsoft.Quantum.Canon");

        internal static QsQualifiedName MakeFullName(string name)
        {
            return new QsQualifiedName(CanonName, NonNullable<string>.New(name));
        }
    }
}
