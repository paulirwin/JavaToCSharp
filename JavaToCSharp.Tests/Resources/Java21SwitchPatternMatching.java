/// Expect:
/// - output: "was null\nstr hi\nother\nint 5\nnull-or-default\nnull-or-default\nstmt int 5\nstmt null-or-default\nstmt null-or-default\ncircle 1\nsquare 2\nsmall circle\nbig circle\nsquare\ninteger 7\nstring hi\narray 3\nother\n"
package example;

// https://openjdk.org/jeps/441

public class Program {
    // Members are declared public because Java's package-private default maps to C# private,
    // which is a pre-existing converter behavior unrelated to switch patterns.
    public sealed interface Shape permits Circle, Square {
    }

    public record Circle(int r) implements Shape {
    }

    public record Square(int s) implements Shape {
    }

    // A standalone `case null` arm keeps null out of the default.
    public static String nullLabel(Object o) {
        return switch (o) {
            case null -> "was null";
            case String s -> "str " + s;
            default -> "other";
        };
    }

    // `case null, default` binds null and everything else to a single arm.
    public static String nullDefault(Object o) {
        return switch (o) {
            case Integer i -> "int " + i;
            case null, default -> "null-or-default";
        };
    }

    // The same combined label in a switch statement rather than an expression.
    public static String nullDefaultStatement(Object o) {
        switch (o) {
            case Integer i -> {
                return "stmt int " + i;
            }
            case null, default -> {
                return "stmt null-or-default";
            }
        }
    }

    // Exhaustive over a sealed hierarchy, so Java needs no default arm. The bindings come from
    // deconstruction rather than accessor calls, which are converted separately.
    public static String exhaustive(Shape shape) {
        return switch (shape) {
            case Circle(int r) -> "circle " + r;
            case Square(int s) -> "square " + s;
        };
    }

    // Guards select between arms that share a type pattern.
    public static String guarded(Shape shape) {
        return switch (shape) {
            case Circle(int r) when r < 10 -> "small circle";
            case Circle c -> "big circle";
            case Square q -> "square";
        };
    }

    // Type patterns over unrelated types, which is the core of JEP 441.
    public static String byType(Object o) {
        return switch (o) {
            case Integer i -> "integer " + i;
            case String s -> "string " + s;
            case int[] arr -> "array " + arr.length;
            default -> "other";
        };
    }

    public static void main(String[] args) {
        System.out.println(nullLabel(null));
        System.out.println(nullLabel("hi"));
        System.out.println(nullLabel(1));

        System.out.println(nullDefault(5));
        System.out.println(nullDefault(null));
        System.out.println(nullDefault("x"));

        System.out.println(nullDefaultStatement(5));
        System.out.println(nullDefaultStatement(null));
        System.out.println(nullDefaultStatement("x"));

        System.out.println(exhaustive(new Circle(1)));
        System.out.println(exhaustive(new Square(2)));

        System.out.println(guarded(new Circle(5)));
        System.out.println(guarded(new Circle(50)));
        System.out.println(guarded(new Square(1)));

        System.out.println(byType(7));
        System.out.println(byType("hi"));
        System.out.println(byType(new int[] { 1, 2, 3 }));
        System.out.println(byType(1.5));
    }
}
