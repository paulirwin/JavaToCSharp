package com.example;

/**
 * Exercises Java 21 record patterns (JEP 440) in both instanceof tests and switch constructs.
 */
public class Java21RecordPatterns {
    record Point(int x, int y) {}

    record Line(Point start, Point end) {}

    public String describeInstanceOf(Object obj) {
        if (obj instanceof Point(int x, int y)) {
            return "point " + x + "," + y;
        }

        if (obj instanceof Line(Point(var ax, var ay), Point end)) {
            return "line from " + ax + "," + ay;
        }

        if (obj instanceof String s) {
            return s;
        }

        return "unknown";
    }

    public String describeSwitchExpression(Object obj) {
        return switch (obj) {
            case Point(int x, int y) when x > y -> "wide point";
            case Point(int x, int y) -> "point " + x + "," + y;
            case Line(Point start, Point end) -> "line";
            case String s -> s;
            default -> "unknown";
        };
    }

    public void describeArrowSwitchStatement(Object obj) {
        switch (obj) {
            case Point(int x, int y) -> System.out.println("point");
            case Line(Point start, Point end) -> System.out.println("line");
            default -> System.out.println("unknown");
        }
    }

    public String describeColonSwitchStatement(Object obj) {
        switch (obj) {
            case Point(int x, int y):
                return "point";
            case String s:
                return s;
            default:
                return "unknown";
        }
    }
}
