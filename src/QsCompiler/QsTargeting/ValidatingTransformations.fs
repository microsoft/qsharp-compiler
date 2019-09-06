// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Targeting

open Microsoft.Quantum.QsCompiler.SymbolManagement
open System.Collections.Generic
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Diagnostics

type internal TVExpressionTransformation(capabilities : TargetCapabilities) =
    inherit ExpressionTransformation()

type internal TVStatementKindTransformation(capabilities : TargetCapabilities) =
    inherit StatementKindTransformation()

    let scopeXformer = new TVScopeTransformation(capabilities)
    let exprXformer = new TVExpressionTransformation(capabilities)

    let diagnostics = new List<DiagnosticItem>()
    member this.Diagnostics with get () = diagnostics

    override this.ScopeTransformation x = scopeXformer.Transform x
    override this.ExpressionTransformation x = exprXformer.Transform x
    override this.TypeTransformation x = x
    override this.LocationTransformation x = x

    override this.onFailStatement x =
        if not capabilities.CanFail then
            diagnostics.Add(DiagnosticItem.Error(ErrorCode.TargetExecutionFailed))
        base.onFailStatement x

and internal TVScopeTransformation(capabilities : TargetCapabilities) =
    inherit ScopeTransformation()

    let diagnostics = [] : DiagnosticItem list

    member this.Diagnostics with get () = new List<DiagnosticItem>(diagnostics)

type internal TVTreeTransformation(capabilities : TargetCapabilities) =
    inherit SyntaxTreeTransformation()

    let diagnostics = [] : DiagnosticItem list

    member this.Diagnostics with get () = new List<DiagnosticItem>(diagnostics)

