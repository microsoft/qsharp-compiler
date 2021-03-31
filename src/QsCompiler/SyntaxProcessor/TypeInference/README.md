# Q# type inference

Q# uses a type inference algorithm based on the [Hindley-Milner type inference algorithm](https://en.wikipedia.org/wiki/Hindley%E2%80%93Milner_type_system#An_inference_algorithm).
The core ideas are:

1. *Instantiation.* When a type is used, any universal quantifiers are *instantiated* as fresh type variables with unique names.
2. *Unification.* When two types must match, they are *unified* by comparing each type side-by-side.
   If an instantiated type variable is encountered in one type, it is bound to the other type.

For example, consider the identity function:

```qsharp
function Identity<'a>(x : 'a) : 'a {
    return x;
}
```

When the identity function is referenced, its type parameters are instantiated to type variables with previously unused names.

```qsharp
// Identity.'a is instantiated to a fresh variable, like 'a0.
// f is of type 'a0 -> 'a0.
let f = Identity;
```

When the function is applied, the argument type is unified with the input of the function type.
To apply a function to an argument of type `Int`, the function type must match `Int -> 'b0`, where `'b0` is another freshly instantiated type variable.

```qsharp
// Int -> 'b0
// must unify with
// 'a0 -> 'a0
//
// result is of type 'b0.
let result = f(7);
```

Unification yields `'a0 ↦ Int` and `'b0 ↦ 'a0`.
We can conclude that `'b0 ↦ Int`, so `result` is of type `Int`, as expected.
By the same reasoning, we have also learned that `f` is of type `Int -> Int`.

Hindley-Milner type inference normally performs *generalization*, the opposite of instantiation, when a `let` binding is encountered.
However, since `let` bindings are monomorphic in Q#, generalization is not supported.
A special case of generalization exists for polymorphic top-level callables, like `Identity`, but this is not part of the type inference algorithm.

## Extensions

Q#'s type system has two additional features that extend the Hindley-Milner type system: type constraints (a limited form of bounded polymorphism), and subtyping of operation characteristics.

### Constraints

Constraints are used to require that some types satisfy certain properties.
For example, using the `==` operator requires that the type supports equality comparisons, and using a `for` loop requires that the type supports iteration.
Constraints are not themselves first-class types in Q#, so all constrained types must resolve to a specific type that satisfies the constraint.

### Subtyping

Q# operation types have subtyping over their characteristics.
For example, `Qubit => Unit is Adj + Ctl` is a subtype of `Qubit => Unit is Adj`, which is a subtype of `Qubit => Unit`.
The subtyping relationship extends to compound types containing operation types, so that `(Qubit => Unit is Adj, Int)` is a subtype of `(Qubit => Unit, Int)`.

Since Q#'s subtyping is limited, unification is still very similar to standard Hindley-Milner unification.
However, an additional *ordering* is given that states whether one type should be a supertype of, equal to, or a subtype of the other type.
Unification fails if the ordering is not satisfied.

Some expressions, like binary operators or array literals, have a type that is the *intersection* or *greatest common base type* of its constituent types.
For simplicity, an intersection is computed using only typing information known up to the point where the intersection is requested.
