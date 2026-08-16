/// Expect:
/// - output: "1, 2\n3\nsame=True\ndiff=False\norigin=0\nCircle r=2\nlabel=P\n"
package example;

// https://docs.oracle.com/en/java/javase/16/language/records.html

interface Shape {
    public String describe();
}

record Circle(int radius) implements Shape {
    public String describe() {
        return "Circle r=" + radius;
    }
}

public class Program {
    // Members are declared public because Java's package-private default maps to C# private,
    // which is a pre-existing converter behavior unrelated to records.
    public record Point(int x, int y) {
        public static final Point ORIGIN = new Point(0, 0);

        public int sum() {
            return x + y;
        }
    }

    public record Labeled<T>(String label, T value) {
    }

    public static void main(String[] args) {
        Point p = new Point(1, 2);
        System.out.println(p.x + ", " + p.y);
        System.out.println(p.sum());

        // Records have value equality in both languages.
        System.out.println("same=" + p.equals(new Point(1, 2)));
        System.out.println("diff=" + p.equals(new Point(3, 4)));

        System.out.println("origin=" + Point.ORIGIN.sum());

        Shape s = new Circle(2);
        System.out.println(s.describe());

        Labeled<Integer> labeled = new Labeled<Integer>("P", 42);
        System.out.println("label=" + labeled.label);
    }
}
