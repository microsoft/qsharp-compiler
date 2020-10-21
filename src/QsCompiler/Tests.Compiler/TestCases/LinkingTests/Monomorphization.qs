// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Quantum.Testing.Monomorphization {
    open Microsoft.Quantum.Testing.Generics;

    @ EntryPoint()
    operation Test1() : Unit {
        Test1Main();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.Monomorphization {
    open Microsoft.Quantum.Testing.Generics;

    @ EntryPoint()
    operation Test2() : Unit {
        Test2Main();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.Monomorphization {
    open Microsoft.Quantum.Testing.Generics;

    @ EntryPoint()
    operation Test3() : Unit {
        Test3Main();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.Monomorphization {
    open Microsoft.Quantum.Testing.Generics;

    @ EntryPoint()
    operation Test4() : Unit {
        Test4Main();
    }
}

// =================================

// Monomorphization Access Modifier Resolution Args
namespace Microsoft.Quantum.Testing.Monomorphization {
    internal newtype MyInternalUDT = Int;
    newtype MyPublicUDT = String;
    
    internal operation IsInternalUsesInternal<'A>(x : 'A) : Unit { }
    internal operation IsInternalUsesPublic<'A>(x : 'A) : Unit { }
    operation IsPublicUsesInternal<'A>(x : 'A) : Unit { }
    operation IsPublicUsesPublic<'A>(x : 'A) : Unit { }

    @ EntryPoint()
    operation TestAccessModifiers() : Unit {
        IsInternalUsesInternal(MyInternalUDT(12));
        IsInternalUsesPublic(MyPublicUDT("Yes"));
        IsPublicUsesInternal(MyInternalUDT(3));
        IsPublicUsesPublic(MyPublicUDT("No"));
    }
}

// =================================

// Monomorphization Access Modifier Resolution Returns
namespace Microsoft.Quantum.Testing.Monomorphization {
    internal newtype MyInternalUDT = Int;
    newtype MyPublicUDT = String;
    
    internal operation IsInternalUsesInternal<'A>() : 'A { return Default<'A>(); }
    internal operation IsInternalUsesPublic<'A>() : 'A { return Default<'A>(); }
    operation IsPublicUsesInternal<'A>() : 'A { return Default<'A>(); }
    operation IsPublicUsesPublic<'A>() : 'A { return Default<'A>(); }

    @ EntryPoint()
    operation TestAccessModifiers() : Unit {
        let temp1 = IsInternalUsesInternal<MyInternalUDT>();
        let temp2 = IsInternalUsesPublic<MyPublicUDT>();
        let temp3 = IsPublicUsesInternal<MyInternalUDT>();
        let temp4 = IsPublicUsesPublic<MyPublicUDT>();
    }
}

// =================================

// Monomorphization Access Modifier Resolution Array Args
namespace Microsoft.Quantum.Testing.Monomorphization {
    internal newtype MyInternalUDT = Int;
    newtype MyPublicUDT = String;
    
    internal operation IsInternalUsesInternal<'A>(x : 'A) : Unit { }
    internal operation IsInternalUsesPublic<'A>(x : 'A) : Unit { }
    operation IsPublicUsesInternal<'A>(x : 'A) : Unit { }
    operation IsPublicUsesPublic<'A>(x : 'A) : Unit { }

    @ EntryPoint()
    operation TestAccessModifiers() : Unit {
        IsInternalUsesInternal(new MyInternalUDT[1]);
        IsInternalUsesPublic(new MyPublicUDT[1]);
        IsPublicUsesInternal(new MyInternalUDT[1]);
        IsPublicUsesPublic(new MyPublicUDT[1]);
    }
}

// =================================

// Monomorphization Access Modifier Resolution Array Returns
namespace Microsoft.Quantum.Testing.Monomorphization {
    internal newtype MyInternalUDT = Int;
    newtype MyPublicUDT = String;
    
    internal operation IsInternalUsesInternal<'A>() : 'A { return Default<'A>(); }
    internal operation IsInternalUsesPublic<'A>() : 'A { return Default<'A>(); }
    operation IsPublicUsesInternal<'A>() : 'A { return Default<'A>(); }
    operation IsPublicUsesPublic<'A>() : 'A { return Default<'A>(); }

    @ EntryPoint()
    operation TestAccessModifiers() : Unit {
        let temp1 = IsInternalUsesInternal<MyInternalUDT[]>();
        let temp2 = IsInternalUsesPublic<MyPublicUDT[]>();
        let temp3 = IsPublicUsesInternal<MyInternalUDT[]>();
        let temp4 = IsPublicUsesPublic<MyPublicUDT[]>();
    }
}

// =================================

// Monomorphization Access Modifier Resolution Tuple Args
namespace Microsoft.Quantum.Testing.Monomorphization {
    internal newtype MyInternalUDT = Int;
    newtype MyPublicUDT = String;
    
    internal operation IsInternalUsesInternal<'A>(x : 'A) : Unit { }
    internal operation IsInternalUsesPublic<'A>(x : 'A) : Unit { }
    operation IsPublicUsesInternal<'A>(x : 'A) : Unit { }
    operation IsPublicUsesPublic<'A>(x : 'A) : Unit { }

    @ EntryPoint()
    operation TestAccessModifiers() : Unit {
        IsInternalUsesInternal((MyInternalUDT(12), 0));
        IsInternalUsesPublic((MyPublicUDT("Yes"), 0));
        IsPublicUsesInternal((MyInternalUDT(3), 0));
        IsPublicUsesPublic((MyPublicUDT("No"), 0));
    }
}

// =================================

// Monomorphization Access Modifier Resolution Tuple Returns
namespace Microsoft.Quantum.Testing.Monomorphization {
    internal newtype MyInternalUDT = Int;
    newtype MyPublicUDT = String;
    
    internal operation IsInternalUsesInternal<'A>() : 'A { return Default<'A>(); }
    internal operation IsInternalUsesPublic<'A>() : 'A { return Default<'A>(); }
    operation IsPublicUsesInternal<'A>() : 'A { return Default<'A>(); }
    operation IsPublicUsesPublic<'A>() : 'A { return Default<'A>(); }

    @ EntryPoint()
    operation TestAccessModifiers() : Unit {
        let temp1 = IsInternalUsesInternal<(MyInternalUDT, Int)>();
        let temp2 = IsInternalUsesPublic<(MyPublicUDT, Int)>();
        let temp3 = IsPublicUsesInternal<(MyInternalUDT, Int)>();
        let temp4 = IsPublicUsesPublic<(MyPublicUDT, Int)>();
    }
}

// =================================

// Monomorphization Access Modifier Resolution Op Args
namespace Microsoft.Quantum.Testing.Monomorphization {
    internal newtype MyInternalUDT = Int;
    newtype MyPublicUDT = String;
    
    internal operation IsInternalUsesInternal<'A>(x : 'A) : Unit { }
    internal operation IsInternalUsesPublic<'A>(x : 'A) : Unit { }
    operation IsPublicUsesInternal<'A>(x : 'A) : Unit { }
    operation IsPublicUsesPublic<'A>(x : 'A) : Unit { }

    internal operation InternalFoo(x : MyInternalUDT) : Unit { }
    operation PublicFoo(x : MyPublicUDT) : Unit { }

    @ EntryPoint()
    operation TestAccessModifiers() : Unit {
        IsInternalUsesInternal(InternalFoo);
        IsInternalUsesPublic(PublicFoo);
        IsPublicUsesInternal(InternalFoo);
        IsPublicUsesPublic(PublicFoo);
    }
}

// =================================

// Monomorphization Access Modifier Resolution Op Returns
namespace Microsoft.Quantum.Testing.Monomorphization {
    internal newtype MyInternalUDT = Int;
    newtype MyPublicUDT = String;
    
    internal operation IsInternalUsesInternal<'A>() : 'A { return Default<'A>(); }
    internal operation IsInternalUsesPublic<'A>() : 'A { return Default<'A>(); }
    operation IsPublicUsesInternal<'A>() : 'A { return Default<'A>(); }
    operation IsPublicUsesPublic<'A>() : 'A { return Default<'A>(); }

    internal operation InternalFoo(x : MyInternalUDT) : Unit { }
    operation PublicFoo(x : MyPublicUDT) : Unit { }

    @ EntryPoint()
    operation TestAccessModifiers() : Unit {
        let temp1 = IsInternalUsesInternal<(MyInternalUDT => Unit)>();
        let temp2 = IsInternalUsesPublic<(MyPublicUDT => Unit)>();
        let temp3 = IsPublicUsesInternal<(MyInternalUDT => Unit)>();
        let temp4 = IsPublicUsesPublic<(MyPublicUDT => Unit)>();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.Monomorphization {
    open Microsoft.Quantum.Arrays;

    @ EntryPoint()
    operation TestTypeParameterResolutions(qs : Int[]) : Unit {
        let res = new Result[Length(qs)];
        let idx = IndexRange(qs);
    }
}
