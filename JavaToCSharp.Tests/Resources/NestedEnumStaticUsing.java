/// Expect:
/// - output: "RED\n"
package example;

public class Program {
    public enum Color { RED, GREEN }

    public static void main(String[] args) {
        System.out.println(Color.RED);
    }
}
