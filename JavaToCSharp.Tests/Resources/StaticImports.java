/// Expect:
/// - output: "clamped=5\nmax=9\ndoubled=8\nsingle=3\n"
package example;

import static example.MathHelpers.doubled;
import static example.MathHelpers.max;
import static example.MathHelpers.min;
import static example.Constants.*;

// A static import names the declaring type, so it must become `using static`. Treating it as a
// namespace import stripped the type off and left the imported members unresolvable.
//
// java.lang.Math is deliberately not used here: it maps to Java.Lang.Math, which does not exist in
// the BCL, so the generated code could not be compiled and run by this harness.
class MathHelpers {
    public static int max(int a, int b) {
        return a > b ? a : b;
    }

    public static int min(int a, int b) {
        return a < b ? a : b;
    }

    public static int doubled(int a) {
        return a * 2;
    }
}

class Constants {
    public static final int SINGLE = 3;
}

public class Program {
    public static int clamp(int value, int low, int high) {
        return min(max(value, low), high);
    }

    public static void main(String[] args) {
        System.out.println("clamped=" + clamp(12, 1, 5));
        System.out.println("max=" + max(9, 3));
        System.out.println("doubled=" + doubled(4));

        // An on-demand static import (`import static example.Constants.*`) already names the type,
        // so nothing is stripped from it.
        System.out.println("single=" + SINGLE);
    }
}
