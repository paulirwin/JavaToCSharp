/// Expect:
/// - output: "point 1,2\nline 0,0\nstring hi\nunknown\nwide point\npoint 1,2\nline\nstring hi\nunknown\narrow point\narrow line\narrow unknown\ncolon point\ncolon hi\ncolon unknown\nnested 4\n"
package example;

// https://openjdk.org/jeps/440

public class Program {
    // Members are declared public because Java's package-private default maps to C# private,
    // which is a pre-existing converter behavior unrelated to record patterns.
    public record Point(int x, int y) {
    }

    public record Line(Point start, Point end) {
    }

    // instanceof with type and record patterns, including a nested deconstruction.
    public static String describeInstanceOf(Object obj) {
        if (obj instanceof Point(int x, int y)) {
            return "point " + x + "," + y;
        }

        if (obj instanceof Line(Point(var ax, var ay), Point end)) {
            return "line " + ax + "," + ay;
        }

        if (obj instanceof String s) {
            return "string " + s;
        }

        return "unknown";
    }

    // Switch expression with pattern labels and a guard.
    public static String describeSwitchExpression(Object obj) {
        return switch (obj) {
            case Point(int x, int y) when x > y -> "wide point";
            case Point(int x, int y) -> "point " + x + "," + y;
            case Line(Point start, Point end) -> "line";
            case String s -> "string " + s;
            default -> "unknown";
        };
    }

    // Arrow-form switch statement: each case has an implicit break in Java.
    public static String describeArrowSwitch(Object obj) {
        String result;

        switch (obj) {
            case Point(int x, int y) -> result = "arrow point";
            case Line(Point start, Point end) -> result = "arrow line";
            default -> result = "arrow unknown";
        }

        return result;
    }

    // Colon-form switch statement with pattern labels.
    public static String describeColonSwitch(Object obj) {
        switch (obj) {
            case Point(int x, int y):
                return "colon point";
            case String s:
                return "colon " + s;
            default:
                return "colon unknown";
        }
    }

    public static void main(String[] args) {
        Point point = new Point(1, 2);
        Line line = new Line(new Point(0, 0), new Point(4, 5));

        System.out.println(describeInstanceOf(point));
        System.out.println(describeInstanceOf(line));
        System.out.println(describeInstanceOf("hi"));
        System.out.println(describeInstanceOf(42));

        // The guard selects the first arm only when x > y.
        System.out.println(describeSwitchExpression(new Point(9, 1)));
        System.out.println(describeSwitchExpression(point));
        System.out.println(describeSwitchExpression(line));
        System.out.println(describeSwitchExpression("hi"));
        System.out.println(describeSwitchExpression(42));

        System.out.println(describeArrowSwitch(point));
        System.out.println(describeArrowSwitch(line));
        System.out.println(describeArrowSwitch(42));

        System.out.println(describeColonSwitch(point));
        System.out.println(describeColonSwitch("hi"));
        System.out.println(describeColonSwitch(42));

        // Bindings from a nested pattern are usable in the matched branch.
        if (line instanceof Line(Point(var ax, var ay), Point(var bx, var by))) {
            System.out.println("nested " + (bx - ax));
        }
    }
}
