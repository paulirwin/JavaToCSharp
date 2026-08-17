/// Expect:
/// - output: "caught: boom\n"
package example;

public class Program {
    public static void main(String[] args) {
        try {
            throw new IllegalArgumentException("boom");
        } catch (IllegalArgumentException e) {
            System.out.println("caught: " + e.getMessage());
        }
    }
}
