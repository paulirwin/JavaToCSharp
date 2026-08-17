/// Expect:
/// - output: "5\n7\n9\n2\n0\n"
package example;

public class Program {
    public static void main(String[] args) {
        // Java allows C-style array brackets per declarator, so one declaration can mix ranks.
        // These must split into separate C# declarations, preserving declaration order.
        int single[] = new int[2], scalar = 7, other[] = {8, 9};

        single[0] = 5;

        System.out.println(single[0]);
        System.out.println(scalar);
        System.out.println(other[1]);

        // A rank group with more than one declarator, and an uninitialized declarator.
        int a[] = {1, 2}, b = 0, c[];
        c = new int[1];

        System.out.println(a[1]);
        System.out.println(c[0] + b);
    }
}
