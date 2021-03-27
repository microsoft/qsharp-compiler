// -----------------------------------------------------------------------
// <copyright file="AttributeValue.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Single attribute for functions, function returns and function parameters.</summary>
    /// <remarks>
    /// This is the equivalent to the underlying llvm::AttributeImpl class. The name was changed to
    /// AttributeValue in .NET to prevent confusion with the standard <see cref="Attribute"/> class
    /// that is used throughout .NET libraries.
    /// </remarks>
    public unsafe struct AttributeValue
        : IEquatable<AttributeValue>
    {
        private AttributeValue(Context context, LLVMAttributeRef nativeValue)
        {
            this.Context = context;
            this.NativeAttribute = nativeValue;
        }

        /// <summary>Gets the Name of the attribute.</summary>
        public string Name
        {
            get
            {
                if (this.IsString)
                {
                    uint len;
                    var pStr = LLVM.GetStringAttributeKind(this.NativeAttribute, &len);
                    if (pStr == default)
                    {
                        return string.Empty;
                    }

                    return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
                }
                else
                {
                    return AttributeKindExtensions.LookupId(LLVM.GetEnumAttributeKind(this.NativeAttribute)).GetAttributeName();
                }
            }
        }

        /// <summary>Gets the value for named attributes with values.</summary>
        /// <value>The value as a string or <see lang="default"/> if the attribute has no value.</value>
        public string? StringValue
        {
            get
            {
                if (this.IsString)
                {
                    uint len;
                    var pStr = LLVM.GetStringAttributeValue(this.NativeAttribute, &len);
                    if (pStr == default)
                    {
                        return string.Empty;
                    }

                    return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
                }

                return default;
            }
        }

        /// <summary>Gets the kind of the attribute.</summary>
        /// <value>The <see cref="AttributeKind"/> or <see cref="AttributeKind.None"/> for named attributes.</value>
        public AttributeKind Kind
            => LLVM.IsStringAttribute(this.NativeAttribute) == 1
                 ? AttributeKind.None
                 : AttributeKindExtensions.LookupId(LLVM.GetEnumAttributeKind(this.NativeAttribute));

        /// <summary>Gets the context that owns this <see cref="AttributeValue"/>.</summary>
        public Context Context { get; }

        /// <summary>Gets the Integer value of the attribute or <see lang="default"/> if the attribute doesn't have a value.</summary>
        public ulong? IntegerValue => this.HasIntegerVaue ? LLVM.GetEnumAttributeValue(this.NativeAttribute) : (ulong?)default;

        /// <summary>Gets a value indicating whether this attribute is a target specific string value.</summary>
        public bool IsString => LLVM.IsStringAttribute(this.NativeAttribute) == 1;

        /// <summary>Gets a value indicating whether this attribute has an integer attribute.</summary>
        public bool HasIntegerVaue => this.Kind.RequiresIntValue();

        /// <summary>Gets a value indicating whether this attribute is a simple enumeration value.</summary>
        public bool IsEnum => LLVM.IsEnumAttribute(this.NativeAttribute) == 1;

        internal LLVMAttributeRef NativeAttribute { get; }

        /// <summary>Tests attributes for equality.</summary>
        /// <param name="left">Left side of the comparison.</param>
        /// <param name="right">Right side of the comparison.</param>
        /// <returns><see lang="true"/> if the attributes are equal.</returns>
        public static bool operator ==(AttributeValue left, AttributeValue right) => Equals(left, right);

        /// <summary>Tests attributes for inequality.</summary>
        /// <param name="left">Left side of the comparison.</param>
        /// <param name="right">Right side of the comparison.</param>
        /// <returns><see lang="true"/> if the attributes are not equal.</returns>
        public static bool operator !=(AttributeValue left, AttributeValue right) => !Equals(left, right);

        /// <summary>Gets a hash code for this instance.</summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode() => this.NativeAttribute.GetHashCode();

        /// <summary>Performs equality checks against an <see cref="object"/>.</summary>
        /// <param name="obj">object to test for equality with this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is equal to this instance.</returns>
        public override bool Equals(object obj)
        {
            return obj is AttributeValue attrib ? this.Equals(attrib) : obj is UIntPtr && this.NativeAttribute.Equals(obj);
        }

        /// <summary>Performs equality checks against an <see cref="AttributeValue"/>.</summary>
        /// <param name="other">object to test for equality with this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="other"/> is equal to this instance.</returns>
        public bool Equals(AttributeValue other)
        {
            return this.NativeAttribute.Equals(other.NativeAttribute);
        }

        /// <summary>Tests if the attribute is valid for a <see cref="Value"/> on a given <see cref="FunctionAttributeIndex"/>.</summary>
        /// <param name="index">Attribute index to test if the attribute is valid on.</param>
        /// <param name="value"><see cref="Value"/>. </param>
        /// <returns><see lang="true"/> if the attribute is valid on the specified <paramref name="index"/> of the given <paramref name="value"/>.</returns>
        public bool IsValidOn(FunctionAttributeIndex index, Value value)
        {
            if (value == default)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // for now all string attributes are valid everywhere as they are target dependent
            // (e.g. no way to verify the validity of an arbitrary without knowing the target)
            if (this.IsString)
            {
                return value.IsFunction;
            }

            return this.Kind.CheckAttributeUsage(index, value);
        }

        /// <summary>Verifies the attribute is valid for a <see cref="Value"/> on a given <see cref="FunctionAttributeIndex"/>.</summary>
        /// <param name="index">Index to verify.</param>
        /// <param name="value">Value to check this attribute on.</param>
        /// <exception cref="ArgumentException">The attribute is not valid on <paramref name="value"/> for the <paramref name="index"/>.</exception>
        public void VerifyValidOn(FunctionAttributeIndex index, Value value)
        {
            if (value == default)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!value.IsFunction)
            {
                throw new ArgumentException("Attributes only allowed on functions and call sites");
            }

            // for now all string attributes are valid everywhere as they are target dependent
            // (e.g. no way to verify the validity of an arbitrary attribute without knowing the target)
            if (this.IsString)
            {
                return;
            }

            this.Kind.VerifyAttributeUsage(index, value);
        }

        /// <summary>Gets a string representation of the attribute.</summary>
        /// <returns>Attribute as a string.</returns>
        public override string ToString() => this.NativeAttribute.ToString();

        internal static AttributeValue FromHandle(Context context, LLVMAttributeRef handle)
        {
            return context.GetAttributeFor(handle);
        }

        internal class InterningFactory
            : HandleInterningMap<LLVMAttributeRef, AttributeValue>
        {
            internal InterningFactory(Context context)
                : base(context)
            {
            }

            private protected override AttributeValue ItemFactory(LLVMAttributeRef handle)
            {
                return new AttributeValue(this.Context, handle);
            }
        }
    }
}
