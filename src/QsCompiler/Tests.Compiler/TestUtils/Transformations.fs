// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Microsoft.Quantum.QsCompiler.Transformations.Core


type private GlobalDeclarations(parent: CheckDeclarations) =
    inherit NamespaceTransformation(parent, TransformationOptions.NoRebuild)

    override this.OnCallableDeclaration c = parent.CheckCallableDeclaration c

    override this.OnTypeDeclaration t = parent.CheckTypeDeclaration t

    override this.OnSpecializationDeclaration s = parent.CheckSpecializationDeclaration s

and private CheckDeclarations private (_internal_, onTypeDecl, onCallableDecl, onSpecDecl) =
    inherit SyntaxTreeTransformation()

    member internal this.CheckTypeDeclaration = onTypeDecl
    member internal this.CheckCallableDeclaration = onCallableDecl
    member internal this.CheckSpecializationDeclaration = onSpecDecl

    new(?onTypeDecl, ?onCallableDecl, ?onSpecDecl) as this =
        let onTypeDecl = defaultArg onTypeDecl id
        let onCallableDecl = defaultArg onCallableDecl id
        let onSpecDecl = defaultArg onSpecDecl id
        CheckDeclarations("_internal_", onTypeDecl, onCallableDecl, onSpecDecl) then
            this.Types <- new TypeTransformation(this, TransformationOptions.Disabled)
            this.Expressions <- new ExpressionTransformation(this, TransformationOptions.Disabled)
            this.Statements <- new StatementTransformation(this, TransformationOptions.Disabled)
            this.Namespaces <- new GlobalDeclarations(this)
