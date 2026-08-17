/// Expect:
/// - output: "area=6\npair=1,2\nsame=True\nnested=9\n"
package example;

// Java 16 local records (JEP 395) may be declared in a method body. C# has no local record
// declaration, so they are hoisted to the enclosing type.
public class Program {
    public static int scale(int value) {
        // A local record in a second method, to verify each is hoisted independently.
        record Factor(int amount) {
        }

        Factor f = new Factor(3);
        return value * f.amount;
    }

    public static void main(String[] args) {
        record Rect(int width, int height) {
        }

        Rect r = new Rect(2, 3);
        System.out.println("area=" + (r.width * r.height));

        // A second local record in the same method, to verify both are hoisted.
        record Pair(int left, int right) {
        }

        Pair p = new Pair(1, 2);
        System.out.println("pair=" + p.left + "," + p.right);

        // Records have value equality in both languages.
        System.out.println("same=" + p.equals(new Pair(1, 2)));

        System.out.println("nested=" + scale(3));
    }
}
