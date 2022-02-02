namespace Test16 {
    operation Lambda() : Unit {
        let x1 = (x1 -> $"{x1}")(1.0);

        use q1 = Qubit[(x -> x + 1)(1)];

        use q2 = (Qubit[(x -> x + 1)(1)], Qubit(), Qubit[(x -> x == 1.0 ? 0 | 1)(1.0)]);

        let f1 = (foo) -> foo + 1 + foo;

        let f2 = (foo, bar) -> foo + bar + 1;

        let f3 = foo -> foo + 1;

        for i in (i -> [$"{i}"])(1.0) {}

        if (x -> x or true)(true) {
        } elif (y -> y or y or true)(true) {
        } elif (y -> y == 1)(1) {
        } elif (x -> x + 1)(1) == 2 {
        }

        let f4 = (foo, foo) -> foo + foo;
    }
}
