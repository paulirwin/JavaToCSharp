/// Expect:
/// - output: "20\n99\n"
package example;

enum Mode { ON, OFF; }

public class Program {
    public static void main(String[] args) {
        int result = 0;
        Mode mode = Mode.ON;

        // assignment to an already-declared variable
        result = switch (mode) {
            case ON: {
                int doubled = 10 * 2;
                yield doubled;
            }
            default:
                yield -1;
        };
        System.out.println(result);

        // reassignment, exercising that the lowering can run more than once
        mode = Mode.OFF;
        result = switch (mode) {
            case ON:
                yield 1;
            default: {
                int big = 100;
                yield big - 1;
            }
        };
        System.out.println(result);
    }
}
