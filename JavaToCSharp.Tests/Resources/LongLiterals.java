/// Expect:
/// - output: "-1 9223372036854775807 10 2147483648 255 8 1000000\n"
package example;

public class Program {
    public static void main(String[] args) {
        // An all-ones hex long is -1 in Java's two's-complement representation. Without the
        // L suffix the generated C# literal is a ulong and fails to compile (CS0266).
        long allOnes = 0xFFFFFFFFFFFFFFFFL;
        long maxValue = 0x7FFFFFFFFFFFFFFFL;
        long small = 10L;
        // Above int.MaxValue, so a bare literal would not be typed as int in C#.
        long aboveIntMax = 2147483648L;
        long hex = 0xFFL;
        long octal = 010L;
        long separated = 1_000_000L;

        System.out.println(allOnes + " " + maxValue + " " + small + " " + aboveIntMax
            + " " + hex + " " + octal + " " + separated);
    }
}
