// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.Compiler.Linter
{
    internal static class SyntaxExtensions
    {
        internal static bool IsPrivate(this string identifierName) =>
            identifierName.StartsWith("_");
        internal static bool IsPrivate(this QsCallable callable) =>
            callable.FullName.Name.GetValue().IsPrivate();
        internal static bool IsPrivate(this QsCustomType udt) =>
            udt.FullName.Name.GetValue().IsPrivate();

        internal static bool IsReserved(this string identifierName) =>
            identifierName.StartsWith("__") && identifierName.EndsWith("__");
        internal static bool IsReserved(this QsCallable callable) =>
            callable.FullName.Name.GetValue().IsReserved();
        internal static bool IsReserved(this QsCustomType udt) =>
            udt.FullName.Name.GetValue().IsReserved();

        internal static bool IsPrivate(this QsNamespaceElement item) =>
            item.IsQsCallable
            ? ((QsNamespaceElement.QsCallable)item).Item.IsPrivate()
            : ((QsNamespaceElement.QsCustomType)item).Item.IsPrivate();

        internal static string ToDottedName(this QsQualifiedName name) =>
            $"{name.Namespace.Value}.{name.Name.Value}";

        internal static bool TryGetSingleton<T>(this QsTuple<T> tuple, out T? item)
        where T: class
        {
            if (tuple.IsQsTupleItem)
            {
                item = ((QsTuple<T>.QsTupleItem)tuple).Item;
                return true;
            }
            else
            {
                item = null;
                return false;
            }
        }

        internal static bool TryGetTuple<T>(this QsTuple<T> tuple, out IEnumerable<QsTuple<T>>? items)
        {
            if (tuple.IsQsTuple)
            {
                items = ((QsTuple<T>.QsTuple)tuple).Item;
                return true;
            }
            else
            {
                items = null;
                return false;
            }
        }

        internal static IEnumerable<T> GetItems<T>(this QsTuple<T> tuple)
        where T: class
        {
            if (tuple.TryGetSingleton(out var item))
            {
                // TryGetSingleton guarantees that its item is non-null when its return
                // value is true.
                Debug.Assert(item != null);
                yield return item;
            }
            else if (tuple.TryGetTuple(out var items))
            {
                // TryGetTuple guarantees that its items enumerable is non-null
                // when its return value is true.
                Debug.Assert(items != null);
                foreach (var nestedItem in items.SelectMany(item => item.GetItems()))
                {
                    yield return nestedItem;
                }
            }
        }


        internal static bool TryGetName(this QsLocalSymbol symbol, out string? name)
        {
            if (symbol.IsValidName)
            {
                name = ((QsLocalSymbol.ValidName)symbol).Item.Value;
                return true;
            }
            else
            {
                name = null;
                return false;
            }
        }

        internal static T? GetValue<T>(this QsNullable<T> qsNullable)
        where T: class =>
            qsNullable.IsNull
            ? null
            : qsNullable.Item;

        internal static T GetValue<T>(this NonNullable<T> nonNullable)
        where T: class =>
            nonNullable.Value;

        internal static (T1, T2) ToValueTuple<T1, T2>(this System.Tuple<T1, T2> tuple) =>
            (tuple.Item1, tuple.Item2);

        internal static SourcePosition GetPosition(this QsPositionInfo position) =>
            new SourcePosition
            {
                Column = position.Column,
                Line = position.Line
            };

        internal static SourceLocation GetLocation(this QsCallable callable)
        {
            var qsLocation = callable.Location.GetValue();
            var range = qsLocation?.Range.ToValueTuple();
            return new SourceLocation
            {
                SourceFile = callable.SourceFile.GetValue(),
                Offset = qsLocation?.Offset.ToValueTuple(),
                Range = range.HasValue
                    ? new Nullable<SourceRange<SourcePosition>>(
                        new SourceRange<SourcePosition>
                        {
                            Start = range.Value.Item1.GetPosition(),
                            End = range.Value.Item2.GetPosition()
                        }
                    )
                    : null
            };
        }

        // FIXME: reduce copy-and-paste programming here.
        internal static SourceLocation GetLocation(this QsCustomType type)
        {
            var qsLocation = type.Location.GetValue();
            var range = qsLocation?.Range.ToValueTuple();
            return new SourceLocation
            {
                SourceFile = type.SourceFile.GetValue(),
                Offset = qsLocation?.Offset.ToValueTuple(),
                Range = range.HasValue
                    ? new Nullable<SourceRange<SourcePosition>>(
                        new SourceRange<SourcePosition>
                        {
                            Start = range.Value.Item1.GetPosition(),
                            End = range.Value.Item2.GetPosition()
                        }
                    )
                    : null
            };
        }

        internal static bool TryGetCallable(this QsNamespaceElement element, out QsCallable? callable)
        {
            if (element.IsQsCallable)
            {
                callable = ((QsNamespaceElement.QsCallable)element).Item;
                return true;
            }
            else
            {
                callable = null;
                return false;
            }
        }

        internal static bool TryGetUdt(this QsNamespaceElement element, out QsCustomType? udt)
        {
            if (element.IsQsCustomType)
            {
                udt = ((QsNamespaceElement.QsCustomType)element).Item;
                return true;
            }
            else
            {
                udt = null;
                return false;
            }
        }

        internal static bool IsDeprecated(this QsCallable callable) =>
            callable.Attributes.IsDeprecated();

        internal static bool IsDeprecated(this QsCustomType udt) =>
            udt.Attributes.IsDeprecated();

        internal static bool IsDeprecated(this IEnumerable<QsDeclarationAttribute> attributes) =>
            attributes
                .Any(attr => {
                    var attrType = attr.TypeId.GetValue();
                    return
                        attrType != null &&
                        attrType.Namespace.GetValue() == "Microsoft.Quantum.Core" &&
                        attrType.Name.GetValue() == "Deprecated";
                });

        internal static bool IsNullOrEmpty(this string? str) =>
            str == null || str.Length == 0;

        internal static bool ContainsMathSymbols(this string str) =>
            str
            .ToCharArray()
            .Any(character =>
                CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.MathSymbol
            );

    }
}