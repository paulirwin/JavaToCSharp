/// Expect:
/// - output: "5\n5\n"
package example;

// see issue #83
public class Program {
    public static void main(String[] args) {
        int binary = 0b101;
        long longBinary = 0b101L;
        System.out.println(binary);
        System.out.println(longBinary);
    }
}
