package com.example;

import java.util.ArrayList;
import java.util.List;
import java.util.function.Function;
import java.util.function.Supplier;

/**
 * Java 8 method references (JEP 126), which map to C# method groups rather than invocations.
 */
public class Java8MethodReferences {
    public void printAll(List<String> items) {
        items.forEach(System.out::println);
    }

    public Function<String, Integer> lengthOf() {
        return String::length;
    }

    public Supplier<ArrayList<String>> newList() {
        return ArrayList<String>::new;
    }

    public int count(List<String> items) {
        return items.size();
    }
}
