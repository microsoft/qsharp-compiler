namespace Fake.Namespace {

}

namespace Microsoft.Quantum.Qir.Development {

    @EntryPoint()
    operation RunExample() : Int {
        if (true)
        {
            return MainFunc();
        }
        return -1;
    }

    operation MainFunc() : Int {
        let var_x = GetTwentySix();
        let add_four = Add(4, _);
        let var_y = add_four(var_x); // 30

        let add_three = Add(3, _);
        return TakesCallable(var_y, add_three); // 33
    }

    operation Add(var_left: Int, var_right: Int) : Int {
        return var_left + var_right;
    }

    operation TakesCallable(given_int: Int, callable: Int => Int) : Int {
        return callable(given_int);
    }

    operation GetTwentySix() : Int {
        return 26;
    }
}

