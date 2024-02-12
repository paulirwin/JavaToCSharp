/// Expect:
/// - output: "2\n"
package example;

// see issue #98
public class Program {
    static boolean[] myBooleans = new boolean[] { true, false, true, false };
    public static void main(String[] args) {
        int trueCount = 0;
        for (boolean b : myBooleans) {
            if (b) {
                trueCount++;
            }
        }
        System.out.println(trueCount);
    }
}
