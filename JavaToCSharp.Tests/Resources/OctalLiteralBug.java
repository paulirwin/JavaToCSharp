/// Expect:
/// - output: "63\n42\n"
package example;

// see issue #45
public class Program {
    public static void main(String[] args) {
        int octal = 077;
        long longOctal = 052l;
        System.out.println(octal);
        System.out.println(longOctal);
    }
}
