package example;

import java.util.ArrayList;
import java.util.List;
import java.util.function.BiFunction;
import java.util.function.Function;
import java.util.function.Supplier;

/**
 * Java 8 method references (JEP 126).
 *
 * <p>These convert to C# method groups rather than invocations. This file is a conversion-only
 * test: java.util.function has no mapping to BCL delegate types, so the generated C# cannot be
 * compiled and run by the integration harness. See ConvertJava21GapTests for the assertions on
 * the generated syntax.
 */
public class Java8MethodReferences {
    public static int add(int a, int b) {
        return a + b;
    }

    public String value;

    public String upper() {
        return value.toUpperCase();
    }

    // Reference to an instance method of a type, applied to a supplied receiver.
    public Function<String, Integer> lengthOf() {
        return String::length;
    }

    // Constructor reference, which has no C# equivalent and becomes a lambda.
    public Supplier<ArrayList<String>> newList() {
        return ArrayList<String>::new;
    }

    // Reference to a static method.
    public BiFunction<Integer, Integer, Integer> adder() {
        return Java8MethodReferences::add;
    }

    // Reference to an instance method of a particular object.
    public Supplier<String> upperOf() {
        return this::upper;
    }

    public void printAll(List<String> items) {
        items.forEach(System.out::println);
    }
}
