package example;

public class Java17SealedClasses {
    public sealed class Shape
        permits Circle, Square, Rectangle {
        public double area() {
            return 0.0;
        }
    }

    public final class Circle extends Shape {
        public double radius;
    }

    public non-sealed class Square extends Shape {
        public double side;
    }

    public sealed class Rectangle extends Shape permits FilledRectangle {
        public double width;
        public double height;
    }

    public final class FilledRectangle extends Rectangle {
        public String color;
    }

    public sealed interface Service permits Alpha, Beta {
        void run();
    }

    public final class Alpha implements Service {
        public void run() {
        }
    }

    public final class Beta implements Service {
        public void run() {
        }
    }
}
