// -----------------------------------------------------------------------
// <copyright file="Context.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Interop;

using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Encapsulates an LLVM context</summary>
    /// <remarks>
    /// <para>A context in LLVM is a container for interning (LLVM refers to this as "uniqueing") various types
    /// and values in the system. This allows running multiple LLVM tool transforms etc.. on different threads
    /// without causing them to collide namespaces and types even if they use the same name (e.g. module one
    /// may have a type Foo, and so does module two but they are completely distinct from each other)</para>
    ///
    /// <para>LLVM Debug information is ultimately all parented to a top level <see cref="DICompileUnit"/> as
    /// the scope, and a compilation unit is bound to a <see cref="BitcodeModule"/>, even though, technically
    /// the types are owned by a Context. Thus to keep things simpler and help make working with debug information
    /// easier. Ubiquity.NET.Llvm encapsulates the native type and the debug type in separate classes that are instances
    /// of the <see cref="IDebugType{NativeT, DebugT}"/> interface </para>
    ///
    /// <note type="note">It is important to be aware of the fact that a Context is not thread safe. The context
    /// itself and the object instances it owns are intended for use by a single thread only. Accessing and
    /// manipulating LLVM objects from multiple threads may lead to race conditions corrupted state and any number
    /// of other undefined issues.</note>
    /// </remarks>
    public unsafe sealed class Context
        : DisposableObject
        , IBitcodeModuleFactory
    {
        /// <summary>Initializes a new instance of the <see cref="Context"/> class.Creates a new context</summary>
        public Context( )
            : this( LLVM.ContextCreate( ) )
        {
            ContextCache.Add( this );
        }

        /// <summary>Gets the LLVM void type for this context</summary>
        public ITypeRef VoidType => TypeRef.FromHandle( LLVM.VoidTypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM boolean type for this context</summary>
        public ITypeRef BoolType => TypeRef.FromHandle( LLVM.Int1TypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM 8 bit integer type for this context</summary>
        public ITypeRef Int8Type => TypeRef.FromHandle( LLVM.Int8TypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM 16 bit integer type for this context</summary>
        public ITypeRef Int16Type => TypeRef.FromHandle( LLVM.Int16TypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM 32 bit integer type for this context</summary>
        public ITypeRef Int32Type => TypeRef.FromHandle( LLVM.Int32TypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM 64 bit integer type for this context</summary>
        public ITypeRef Int64Type => TypeRef.FromHandle( LLVM.Int64TypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM 128 bit integer type for this context</summary>
        public ITypeRef Int128Type => TypeRef.FromHandle( LLVM.Int128TypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM half precision floating point type for this context</summary>
        public ITypeRef HalfFloatType => TypeRef.FromHandle( LLVM.HalfTypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM single precision floating point type for this context</summary>
        public ITypeRef FloatType => TypeRef.FromHandle( LLVM.FloatTypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM double precision floating point type for this context</summary>
        public ITypeRef DoubleType => TypeRef.FromHandle( LLVM.DoubleTypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM token type for this context</summary>
        public ITypeRef TokenType => TypeRef.FromHandle( LLVM.TokenTypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM Metadata type for this context</summary>
        public ITypeRef MetadataType => TypeRef.FromHandle( LLVM.MetadataTypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM X86 80-bit floating point type for this context</summary>
        public ITypeRef X86Float80Type => TypeRef.FromHandle( LLVM.X86FP80TypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM 128-Bit floating point type</summary>
        public ITypeRef Float128Type => TypeRef.FromHandle( LLVM.FP128TypeInContext( ContextHandle ) )!;

        /// <summary>Gets the LLVM PPC 128-bit floating point type</summary>
        public ITypeRef PpcFloat128Type => TypeRef.FromHandle( LLVM.PPCFP128TypeInContext( ContextHandle ) )!;

        /// <summary>Get a type that is a pointer to a value of a given type</summary>
        /// <param name="elementType">Type of value the pointer points to</param>
        /// <returns><see cref="IPointerType"/> for a pointer that references a value of type <paramref name="elementType"/></returns>
        public IPointerType GetPointerTypeFor( ITypeRef elementType )
        {

            if( elementType.Context != this )
            {
                throw new ArgumentException( );
            }

            return TypeRef.FromHandle<IPointerType>( LLVM.PointerType( elementType.GetTypeRef( ), 0 ) );
        }

        /// <summary>Get's an LLVM integer type of arbitrary bit width</summary>
        /// <param name="bitWidth">Width of the integer type in bits</param>
        /// <remarks>
        /// For standard integer bit widths (e.g. 1,8,16,32,64) this will return
        /// the same type as the corresponding specialized property.
        /// (e.g. GetIntType(1) is the same as <see cref="BoolType"/>,
        ///  GetIntType(16) is the same as <see cref="Int16Type"/>, etc... )
        /// </remarks>
        /// <returns>Integer <see cref="ITypeRef"/> for the specified width</returns>
        public ITypeRef GetIntType( uint bitWidth )
        {
            if( bitWidth == 0 )
            {
                throw new ArgumentException( );
            }

            return bitWidth switch
            {
                1 => BoolType,
                8 => Int8Type,
                16 => Int16Type,
                32 => Int32Type,
                64 => Int64Type,
                128 => Int128Type,
                _ => TypeRef.FromHandle( LLVM.IntTypeInContext( ContextHandle, bitWidth ) )!,
            };
        }

        /// <summary>Get an LLVM Function type (e.g. signature)</summary>
        /// <param name="returnType">Return type of the function</param>
        /// <param name="args">Optional set of function argument types</param>
        /// <returns>Signature type for the specified signature</returns>
        public IFunctionType GetFunctionType( ITypeRef returnType, params ITypeRef[ ] args )
            => GetFunctionType( returnType, args, false );

        /// <summary>Get an LLVM Function type (e.g. signature)</summary>
        /// <param name="returnType">Return type of the function</param>
        /// <param name="args">Potentially empty set of function argument types</param>
        /// <returns>Signature type for the specified signature</returns>
        public IFunctionType GetFunctionType( ITypeRef returnType, IEnumerable<ITypeRef> args )
            => GetFunctionType( returnType, args, false );

        /// <summary>Get an LLVM Function type (e.g. signature)</summary>
        /// <param name="returnType">Return type of the function</param>
        /// <param name="args">Potentially empty set of function argument types</param>
        /// <param name="isVarArgs">Flag to indicate if the method supports C/C++ style VarArgs</param>
        /// <returns>Signature type for the specified signature</returns>
        public IFunctionType GetFunctionType( ITypeRef returnType, IEnumerable<ITypeRef> args, bool isVarArgs )
        {
            if( ContextHandle != returnType.Context.ContextHandle )
            {
                throw new ArgumentException( );
            }

            LLVMTypeRef[ ] llvmArgs = args.Select( a => a.GetTypeRef( ) ).ToArray( );
            fixed (LLVMTypeRef* pLlvmArgs = llvmArgs.AsSpan( ))
            {
                var signature = LLVM.FunctionType( returnType.GetTypeRef( ), (LLVMOpaqueType**)pLlvmArgs, ( uint )llvmArgs.Length, isVarArgs ? 1 : 0 );
                return TypeRef.FromHandle<IFunctionType>( signature );
            }
        }

        /// <summary>Creates a constant structure from a set of values</summary>
        /// <param name="packed">Flag to indicate if the structure is packed and no alignment should be applied to the members</param>
        /// <param name="values">Set of values to use in forming the structure</param>
        /// <returns>Newly created <see cref="Constant"/></returns>
        /// <remarks>
        /// The actual concrete return type depends on the parameters provided and will be one of the following:
        /// <list type="table">
        /// <listheader>
        /// <term><see cref="Constant"/> derived type</term><description>Description</description>
        /// </listheader>
        /// <item><term>ConstantAggregateZero</term><description>If all the member values are zero constants</description></item>
        /// <item><term>UndefValue</term><description>If all the member values are UndefValue</description></item>
        /// <item><term>ConstantStruct</term><description>All other cases</description></item>
        /// </list>
        /// </remarks>
        public Constant CreateConstantStruct( bool packed, params Constant[ ] values )
            => CreateConstantStruct( packed, ( IEnumerable<Constant> )values );

        /// <summary>Creates a constant structure from a set of values</summary>
        /// <param name="packed">Flag to indicate if the structure is packed and no alignment should be applied to the members</param>
        /// <param name="values">Set of values to use in forming the structure</param>
        /// <returns>Newly created <see cref="Constant"/></returns>
        /// <remarks>
        /// <note type="note">The actual concrete return type depends on the parameters provided and will be one of the following:
        /// <list type="table">
        /// <listheader>
        /// <term><see cref="Constant"/> derived type</term><description>Description</description>
        /// </listheader>
        /// <item><term>ConstantAggregateZero</term><description>If all the member values are zero constants</description></item>
        /// <item><term>UndefValue</term><description>If all the member values are UndefValue</description></item>
        /// <item><term>ConstantStruct</term><description>All other cases</description></item>
        /// </list>
        /// </note>
        /// </remarks>
        public Constant CreateConstantStruct( bool packed, IEnumerable<Constant> values )
        {
            var valueHandles = values.Select( v => v.ValueHandle ).ToArray( );
            fixed (LLVMValueRef* pValueHandles = valueHandles.AsSpan( ))
            {
                var handle = LLVM.ConstStructInContext( ContextHandle, (LLVMOpaqueValue**)pValueHandles, ( uint )valueHandles.Length, packed ? 1 : 0 );
                return Value.FromHandle<Constant>( handle )!;
            }
        }

        /// <summary>Creates a constant instance of a specified structure type from a set of values</summary>
        /// <param name="type">Type of the structure to create</param>
        /// <param name="values">Set of values to use in forming the structure</param>
        /// <returns>Newly created <see cref="Constant"/></returns>
        /// <remarks>
        /// <note type="note">The actual concrete return type depends on the parameters provided and will be one of the following:
        /// <list type="table">
        /// <listheader>
        /// <term><see cref="Constant"/> derived type</term><description>Description</description>
        /// </listheader>
        /// <item><term>ConstantAggregateZero</term><description>If all the member values are zero constants</description></item>
        /// <item><term>UndefValue</term><description>If all the member values are UndefValue</description></item>
        /// <item><term>ConstantStruct</term><description>All other cases</description></item>
        /// </list>
        /// </note>
        /// </remarks>
        public Constant CreateNamedConstantStruct( IStructType type, params Constant[ ] values )
        {
            return CreateNamedConstantStruct( type, ( IEnumerable<Constant> )values );
        }

        /// <summary>Creates a constant instance of a specified structure type from a set of values</summary>
        /// <param name="type">Type of the structure to create</param>
        /// <param name="values">Set of values to use in forming the structure</param>
        /// <returns>Newly created <see cref="Constant"/></returns>
        /// <remarks>
        /// <note type="note">The actual concrete return type depends on the parameters provided and will be one of the following:
        /// <list type="table">
        /// <listheader>
        /// <term><see cref="Constant"/> derived type</term><description>Description</description>
        /// </listheader>
        /// <item><term>ConstantAggregateZero</term><description>If all the member values are zero constants</description></item>
        /// <item><term>UndefValue</term><description>If all the member values are UndefValue</description></item>
        /// <item><term>ConstantStruct</term><description>All other cases</description></item>
        /// </list>
        /// </note>
        /// </remarks>
        public Constant CreateNamedConstantStruct( IStructType type, IEnumerable<Constant> values )
        {
            if( type.Context != this )
            {
                throw new ArgumentException( );
            }

            var valueList = values as IList<Constant> ?? values.ToList( );
            var valueHandles = valueList.Select( v => v.ValueHandle ).ToArray( );
            if( type.Members.Count != valueHandles.Length )
            {
                throw new ArgumentException( );
            }

            var mismatchedTypes = from indexedVal in valueList.Select( ( v, i ) => new { Value = v, Index = i } )
                                  where indexedVal.Value.NativeType != type.Members[ indexedVal.Index ]
                                  select indexedVal;

            if( mismatchedTypes.Any( ) )
            {
                throw new ArgumentException( );
            }

            fixed (LLVMValueRef* pValueHandles = valueHandles.AsSpan( ))
            {
                var handle = LLVM.ConstNamedStruct( type.GetTypeRef( ), (LLVMOpaqueValue**)pValueHandles, (uint)valueHandles.Length );
                return Value.FromHandle<Constant>( handle )!;
            }
        }

        /// <summary>Create an opaque structure type (e.g. a forward reference)</summary>
        /// <param name="name">Name of the type (use <see cref="string.Empty"/> for anonymous types)</param>
        /// <remarks>
        /// This method creates an opaque type. The <see cref="IStructType.SetBody(bool, ITypeRef[])"/>
        /// method provides a means to add a body, including indication of packed status, to an opaque
        /// type at a later time if the details of the body are required. (If only pointers to the type
        /// are required then the body isn't required)
        /// </remarks>
        /// <returns>New type</returns>
        public IStructType CreateStructType( string name )
        {
            var handle = LLVM.StructCreateNamed( ContextHandle, name.AsMarshaledString() );
            return TypeRef.FromHandle<IStructType>( handle );
        }

        /// <summary>Create an anonymous structure type (e.g. Tuple)</summary>
        /// <param name="packed">Flag to indicate if the structure is "packed"</param>
        /// <param name="elements">Types of the fields of the structure</param>
        /// <returns>
        /// <see cref="IStructType"/> with the specified body defined.
        /// </returns>
        public IStructType CreateStructType( bool packed, params ITypeRef[ ] elements )
        {
            LLVMTypeRef[ ] llvmArgs = elements.Select( e => e.GetTypeRef() ).ToArray( );
            fixed (LLVMTypeRef* pLlvmArgs = llvmArgs.AsSpan( ) )
            {
                var handle = LLVM.StructTypeInContext( ContextHandle, (LLVMOpaqueType**)pLlvmArgs, ( uint )llvmArgs.Length, packed ? 1 : 0 );
                return TypeRef.FromHandle<IStructType>( handle );
            }
        }

        /// <summary>Creates a new structure type in this <see cref="Context"/></summary>
        /// <param name="name">Name of the structure</param>
        /// <param name="packed">Flag indicating if the structure is packed</param>
        /// <param name="elements">Types for the structures elements in layout order</param>
        /// <returns>
        /// <see cref="IStructType"/> with the specified body defined.
        /// </returns>
        /// <remarks>
        /// If the elements argument list is empty then a complete 0 sized struct is created
        /// </remarks>
        public IStructType CreateStructType( string name, bool packed, params ITypeRef[ ] elements )
            => CreateStructType( name, packed, ( IEnumerable<ITypeRef> )elements );

        /// <summary>Creates a new structure type in this <see cref="Context"/></summary>
        /// <param name="name">Name of the structure (use <see cref="string.Empty"/> for anonymous types)</param>
        /// <param name="packed">Flag indicating if the structure is packed</param>
        /// <param name="elements">Types for the structures elements in layout order</param>
        /// <returns>
        /// <see cref="IStructType"/> with the specified body defined.
        /// </returns>
        /// <remarks>
        /// If the elements argument list is empty then a complete 0 sized struct is created
        /// </remarks>
        public IStructType CreateStructType( string name, bool packed, IEnumerable<ITypeRef> elements )
        {
            var retVal = TypeRef.FromHandle<IStructType>( LLVM.StructCreateNamed( ContextHandle, name.AsMarshaledString() ) );
            retVal.SetBody( packed, elements );

            return retVal;
        }

        /// <summary>Create a constant data string value</summary>
        /// <param name="value">string to convert into an LLVM constant value</param>
        /// <returns>new <see cref="ConstantDataArray"/></returns>
        /// <remarks>
        /// This converts the string to ANSI form and creates an LLVM constant array of i8
        /// characters for the data with a terminating null character. To control the enforcement
        /// of a terminating null character, use the <see cref="CreateConstantString(string, bool)"/>
        /// overload to specify the intended behavior.
        /// </remarks>
        public ConstantDataArray CreateConstantString( string value ) => CreateConstantString( value, true );

        /// <summary>Create a constant data string value</summary>
        /// <param name="value">string to convert into an LLVM constant value</param>
        /// <param name="nullTerminate">flag to indicate if the string should include a null terminator</param>
        /// <returns>new <see cref="ConstantDataArray"/></returns>
        /// <remarks>
        /// This converts the string to ANSI form and creates an LLVM constant array of i8
        /// characters for the data. Enforcement of a null terminator depends on the value of <paramref name="nullTerminate"/>
        /// </remarks>
        public ConstantDataArray CreateConstantString( string value, bool nullTerminate )
        {
            var handle = LLVM.ConstStringInContext( ContextHandle, value.AsMarshaledString(), ( uint )value.Length, !nullTerminate ? 1 : 0 );
            return Value.FromHandle<ConstantDataArray>( handle )!;
        }

        /// <summary>Creates a new <see cref="ConstantInt"/> with a bit length of 1</summary>
        /// <param name="constValue">Value for the constant</param>
        /// <returns><see cref="ConstantInt"/> representing the value</returns>
        public ConstantInt CreateConstant( bool constValue )
        {
            var handle = LLVM.ConstInt( BoolType.GetTypeRef( )
                                     , ( ulong )( constValue ? 1 : 0 )
                                     , 0
                                     );
            return Value.FromHandle<ConstantInt>( handle )!;
        }

        /// <summary>Creates a new <see cref="ConstantInt"/> with a bit length of 8</summary>
        /// <param name="constValue">Value for the constant</param>
        /// <returns><see cref="ConstantInt"/> representing the value</returns>
        public ConstantInt CreateConstant( byte constValue )
        {
            var handle = LLVM.ConstInt( Int8Type.GetTypeRef( ), constValue, 0 );
            return Value.FromHandle<ConstantInt>( handle )!;
        }

        /// <summary>Creates a new <see cref="ConstantInt"/> with a bit length of 8</summary>
        /// <param name="constValue">Value for the constant</param>
        /// <returns><see cref="ConstantInt"/> representing the value</returns>
        public Constant CreateConstant( sbyte constValue )
        {
            var handle = LLVM.ConstInt( Int8Type.GetTypeRef( ), ( ulong )constValue, 1 );
            return Value.FromHandle<ConstantInt>( handle )!;
        }

        /// <summary>Creates a new <see cref="ConstantInt"/> with a bit length of 16</summary>
        /// <param name="constValue">Value for the constant</param>
        /// <returns><see cref="ConstantInt"/> representing the value</returns>
        public ConstantInt CreateConstant( Int16 constValue )
        {
            var handle = LLVM.ConstInt( Int16Type.GetTypeRef( ), ( ulong )constValue, 1 );
            return Value.FromHandle<ConstantInt>( handle )!;
        }

        /// <summary>Creates a new <see cref="ConstantInt"/> with a bit length of 16</summary>
        /// <param name="constValue">Value for the constant</param>
        /// <returns><see cref="ConstantInt"/> representing the value</returns>
        public ConstantInt CreateConstant( UInt16 constValue )
        {
            var handle = LLVM.ConstInt( Int16Type.GetTypeRef( ), constValue, 0 );
            return Value.FromHandle<ConstantInt>( handle )!;
        }

        /// <summary>Creates a new <see cref="ConstantInt"/> with a bit length of 32</summary>
        /// <param name="constValue">Value for the constant</param>
        /// <returns><see cref="ConstantInt"/> representing the value</returns>
        public ConstantInt CreateConstant( Int32 constValue )
        {
            var handle = LLVM.ConstInt( Int32Type.GetTypeRef( ), ( ulong )constValue, 1 );
            return Value.FromHandle<ConstantInt>( handle )!;
        }

        /// <summary>Creates a new <see cref="ConstantInt"/> with a bit length of 32</summary>
        /// <param name="constValue">Value for the constant</param>
        /// <returns><see cref="ConstantInt"/> representing the value</returns>
        public ConstantInt CreateConstant( UInt32 constValue )
        {
            var handle = LLVM.ConstInt( Int32Type.GetTypeRef( ), constValue, 0 );
            return Value.FromHandle<ConstantInt>( handle )!;
        }

        /// <summary>Creates a new <see cref="ConstantInt"/> with a bit length of 64</summary>
        /// <param name="constValue">Value for the constant</param>
        /// <returns><see cref="ConstantInt"/> representing the value</returns>
        public ConstantInt CreateConstant( Int64 constValue )
        {
            var handle = LLVM.ConstInt( Int64Type.GetTypeRef( ), ( ulong )constValue, 1 );
            return Value.FromHandle<ConstantInt>( handle )!;
        }

        /// <summary>Creates a new <see cref="ConstantInt"/> with a bit length of 64</summary>
        /// <param name="constValue">Value for the constant</param>
        /// <returns><see cref="ConstantInt"/> representing the value</returns>
        public ConstantInt CreateConstant( UInt64 constValue )
        {
            var handle = LLVM.ConstInt( Int64Type.GetTypeRef( ), constValue, 0 );
            return Value.FromHandle<ConstantInt>( handle )!;
        }

        /// <summary>Creates a new <see cref="ConstantInt"/> with a bit length of 64</summary>
        /// <param name="bitWidth">Bit width of the integer</param>
        /// <param name="constValue">Value for the constant</param>
        /// <param name="signExtend">flag to indicate if the constant value should be sign extended</param>
        /// <returns><see cref="ConstantInt"/> representing the value</returns>
        public ConstantInt CreateConstant( uint bitWidth, UInt64 constValue, bool signExtend )
        {
            var intType = GetIntType( bitWidth );
            return CreateConstant( intType, constValue, signExtend );
        }

        /// <summary>Create a constant value of the specified integer type</summary>
        /// <param name="intType">Integer type</param>
        /// <param name="constValue">value</param>
        /// <param name="signExtend">flag to indicate if <paramref name="constValue"/> is sign extended</param>
        /// <returns>Constant for the specified value</returns>
        public ConstantInt CreateConstant( ITypeRef intType, UInt64 constValue, bool signExtend )
        {
            if( intType.Context != this )
            {
                throw new ArgumentException( );
            }

            if( intType.Kind != TypeKind.Integer )
            {
                throw new ArgumentException( );
            }

            LLVMValueRef valueRef = LLVM.ConstInt( intType.GetTypeRef( ), constValue, signExtend ? 1 : 0 );
            return Value.FromHandle<ConstantInt>( valueRef )!;
        }

        /// <summary>Creates a constant floating point value for a given value</summary>
        /// <param name="constValue">Value to make into a <see cref="ConstantFP"/></param>
        /// <returns>Constant value</returns>
        public ConstantFP CreateConstant( float constValue )
            => Value.FromHandle<ConstantFP>( LLVM.ConstReal( FloatType.GetTypeRef( ), constValue ) )!;

        /// <summary>Creates a constant floating point value for a given value</summary>
        /// <param name="constValue">Value to make into a <see cref="ConstantFP"/></param>
        /// <returns>Constant value</returns>
        public ConstantFP CreateConstant( double constValue )
            => Value.FromHandle<ConstantFP>( LLVM.ConstReal( DoubleType.GetTypeRef( ), constValue ) )!;

        /// <summary>Creates a simple boolean attribute</summary>
        /// <param name="kind">Kind of attribute</param>
        /// <returns><see cref="AttributeValue"/> with the specified Kind set</returns>
        public AttributeValue CreateAttribute( AttributeKind kind )
        {
            if( kind.RequiresIntValue( ) )
            {
                throw new ArgumentException( );
            }

            var handle = LLVM.CreateEnumAttribute( ContextHandle
                                                , kind.GetEnumAttributeId( )
                                                , 0ul
                                                );
            return AttributeValue.FromHandle( this, handle );
        }

        /// <summary>Creates an attribute with an integer value parameter</summary>
        /// <param name="kind">The kind of attribute</param>
        /// <param name="value">Value for the attribute</param>
        /// <remarks>
        /// <para>Not all attributes support a value and those that do don't all support
        /// a full 64bit value. The following table provides the kinds of attributes
        /// accepting a value and the allowed size of the values.</para>
        /// <list type="table">
        /// <listheader><term><see cref="AttributeKind"/></term><term>Bit Length</term></listheader>
        /// <item><term><see cref="AttributeKind.Alignment"/></term><term>32</term></item>
        /// <item><term><see cref="AttributeKind.StackAlignment"/></term><term>32</term></item>
        /// <item><term><see cref="AttributeKind.Dereferenceable"/></term><term>64</term></item>
        /// <item><term><see cref="AttributeKind.DereferenceableOrNull"/></term><term>64</term></item>
        /// </list>
        /// </remarks>
        /// <returns><see cref="AttributeValue"/> with the specified kind and value</returns>
        public AttributeValue CreateAttribute( AttributeKind kind, UInt64 value )
        {
            if( !kind.RequiresIntValue( ) )
            {
                throw new ArgumentException( );
            }

            var handle = LLVM.CreateEnumAttribute( ContextHandle
                                                , kind.GetEnumAttributeId( )
                                                , value
                                                );
            return AttributeValue.FromHandle( this, handle );
        }

        /// <summary>Adds a valueless named attribute</summary>
        /// <param name="name">Attribute name</param>
        /// <returns><see cref="AttributeValue"/> with the specified name</returns>
        public AttributeValue CreateAttribute( string name ) => CreateAttribute( name, string.Empty );

        /// <summary>Adds a Target specific named attribute with value</summary>
        /// <param name="name">Name of the attribute</param>
        /// <param name="value">Value of the attribute</param>
        /// <returns><see cref="AttributeValue"/> with the specified name and value</returns>
        public AttributeValue CreateAttribute( string name, string value )
        {
            var handle = LLVM.CreateStringAttribute( ContextHandle, name.AsMarshaledString(), ( uint )name.Length, value.AsMarshaledString(), ( uint )value.Length );
            return AttributeValue.FromHandle( this, handle );
        }

        /// <summary>Create a named <see cref="BasicBlock"/> without inserting it into a function</summary>
        /// <param name="name">Name of the block to create</param>
        /// <returns><see cref="BasicBlock"/> created</returns>
        public BasicBlock CreateBasicBlock( string name )
        {
            return BasicBlock.FromHandle( LLVM.CreateBasicBlockInContext( ContextHandle, name.AsMarshaledString() ) )!;
        }

        /// <inheritdoc/>
        public BitcodeModule CreateBitcodeModule( )
        {
            return ModuleCache.CreateBitcodeModule( );
        }

        /// <inheritdoc/>
        public BitcodeModule CreateBitcodeModule( string moduleId )
        {
            return ModuleCache.CreateBitcodeModule( moduleId );
        }

        /// <summary>Gets the modules created in this context</summary>
        public IEnumerable<BitcodeModule> Modules => ModuleCache;

        /// <summary>Gets non-zero Metadata kind ID for a given name</summary>
        /// <param name="name">name of the metadata kind</param>
        /// <returns>integral constant for the ID</returns>
        /// <remarks>
        /// These IDs are uniqued across all modules in this context.
        /// </remarks>
        public uint GetMDKindId( string name )
        {
            return LLVM.GetMDKindIDInContext( ContextHandle, name.AsMarshaledString(), name == default ? 0u : ( uint )name.Length );
        }

        internal LLVMContextRef ContextHandle { get; }

        /* These interning methods provide unique mapping between the .NET wrappers and the underlying LLVM instances
        // The mapping ensures that any LibLLVM handle is always re-mappable to exactly one wrapper instance.
        // This helps reduce the number of wrapper instances created and also allows reference equality to work
        // as expected for managed types.
        */

        // looks up an attribute by it's handle, if none is found a new managed wrapper is created
        // The factory contains a reference to this Context to ensure that the proper ownership is
        // maintained. (LLVM has no method of retrieving the context that owns an attribute)
        internal AttributeValue GetAttributeFor( LLVMAttributeRef handle )
        {
            return AttributeValueCache.GetOrCreateItem( handle );
        }

        internal void RemoveModule( BitcodeModule module )
        {
            if( module.ModuleHandle.Handle != default )
            {
                ModuleCache.Remove( module.ModuleHandle );
            }
        }

        internal BitcodeModule GetModuleFor( LLVMModuleRef moduleRef )
        {
            if ( moduleRef == default )
            {
                throw new ArgumentNullException();
            }
            var hModuleContext = LLVM.GetModuleContext( moduleRef );
            if( hModuleContext != ContextHandle )
            {
                throw new ArgumentException( );
            }

            // make sure handle to existing module isn't auto disposed.
            return ModuleCache.GetOrCreateItem( moduleRef );
        }

        internal Value GetValueFor( LLVMValueRef valueRef )
        {
            return ValueCache.GetOrCreateItem( valueRef );
        }

        internal ITypeRef GetTypeFor( LLVMTypeRef typeRef )
        {
            return TypeCache.GetOrCreateItem( typeRef );
        }

        internal Context( LLVMContextRef contextRef )
        {
            if( contextRef.Handle == default )
            {
                throw new ArgumentNullException( nameof( contextRef ) );
            }

            ContextHandle = contextRef;
            ActiveHandler = new WrappedNativeCallback<LLVMDiagnosticHandler>( DiagnosticHandler );
            ValueCache = new Value.InterningFactory( this );
            ModuleCache = new BitcodeModule.InterningFactory( this );
            TypeCache = new TypeRef.InterningFactory( this );
            AttributeValueCache = new AttributeValue.InterningFactory( this );

            LLVM.ContextSetDiagnosticHandler( ContextHandle, ActiveHandler, (void*)default );
        }

        /// <summary>Disposes the context to release unmanaged resources deterministically</summary>
        /// <param name="disposing">Indicates whether this is from a call to Dispose (<see langword="true"/>) or if from a finalizer</param>
        /// <remarks>
        /// If <paramref name="disposing"/> is <see langword="true"/> then this will release managed and unmanaged resources.
        /// Otherwise, this will only release the native/unmanaged resources.
        /// </remarks>
        protected override void Dispose( bool disposing )
        {
            // disconnect all modules so that any future critical finalization has no impact
            var handles = from m in Modules
                          where !(m.ModuleHandle.Handle == default)
                          select m.ModuleHandle;

            foreach( var handle in handles )
            {
                handle.Dispose( );
            }

            LLVM.ContextSetDiagnosticHandler( ContextHandle, default, (void*)default );
            ActiveHandler.Dispose( );

            ContextCache.TryRemove( ContextHandle );

            ContextHandle.Dispose( );
        }

        private static void DiagnosticHandler( LLVMOpaqueDiagnosticInfo* param0, void* param1 )
        {
            var msg = LLVM.GetDiagInfoDescription( param0 );
            var span = new ReadOnlySpan<byte>(msg, int.MaxValue);
            var level = LLVM.GetDiagInfoSeverity( param0 );
            Debug.WriteLine( "{0}: {1}", level, span.Slice(0, span.IndexOf((byte)'\0')).AsString() );
            LLVM.DisposeErrorMessage(msg);
        }

        private readonly WrappedNativeCallback<LLVMDiagnosticHandler> ActiveHandler;

        // child item wrapper factories
        private readonly Value.InterningFactory ValueCache;
        private readonly BitcodeModule.InterningFactory ModuleCache;
        private readonly TypeRef.InterningFactory TypeCache;
        private readonly AttributeValue.InterningFactory AttributeValueCache;
    }
}
