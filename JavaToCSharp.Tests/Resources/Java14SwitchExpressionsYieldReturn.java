/// Expect:
/// - output: "7\n42\n11\n"
package example;

enum Size { SMALL, MEDIUM, LARGE; }

public class Program {
    // switch expression yielded directly from a return statement
    static int describe(Size size) {
        return switch (size) {
            case SMALL: {
                int base = 3;
                yield base + 4;
            }
            case MEDIUM:
                yield 42;
            default: {
                int a = 5;
                int b = 6;
                yield a + b;
            }
        };
    }

    public static void main(String[] args) {
        System.out.println(describe(Size.SMALL));
        System.out.println(describe(Size.MEDIUM));
        System.out.println(describe(Size.LARGE));
    }
}
