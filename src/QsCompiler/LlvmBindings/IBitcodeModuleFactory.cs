// -----------------------------------------------------------------------
// <copyright file="IBitcodeModuleFactory.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

namespace Ubiquity.NET.Llvm
{
    /// <summary>Interface for a <see cref="BitcodeModule"/> factory</summary>
    /// <remarks>
    /// Modules are owned by the context and thus not created freestanding.
    /// This interface provides factory methods for constructing modules. It
    /// is implemented by the <see cref="Context"/> and also internally by
    /// the handle caches that ultimately call the underlying LLVM module
    /// creation APIs.
    /// </remarks>
    public interface IBitcodeModuleFactory
    {
        /// <summary>Creates a new instance of the <see cref="BitcodeModule"/> class in this context</summary>
        /// <returns><see cref="BitcodeModule"/></returns>
        BitcodeModule CreateBitcodeModule( );

        /// <summary>Creates a new instance of the <see cref="BitcodeModule"/> class in a given context</summary>
        /// <param name="moduleId">Module's ID</param>
        /// <returns><see cref="BitcodeModule"/></returns>
        BitcodeModule CreateBitcodeModule( string moduleId );
    }
}
