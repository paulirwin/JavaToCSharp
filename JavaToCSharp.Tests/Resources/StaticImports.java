package com.example;

import static java.lang.Math.max;
import static java.lang.Math.min;
import static java.util.Arrays.asList;

import java.util.List;

/**
 * Static imports, which map to C# <c>using static</c> directives rather than namespace usings.
 */
public class StaticImports {
    public int clamp(int value, int low, int high) {
        return min(max(value, low), high);
    }

    public List<String> pair(String a, String b) {
        return asList(a, b);
    }
}
