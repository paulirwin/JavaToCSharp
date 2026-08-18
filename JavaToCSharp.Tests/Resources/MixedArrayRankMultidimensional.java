// NOTE: this test case only parses and converts successfully, it does not yet run.
// The mixed-rank declaration is split correctly by this test's coverage, but jagged arrays are
// still emitted as rectangular C# arrays (`int[,]`) while indexing stays `multi[0][0]`, so the
// generated code does not compile. That is the pre-existing limitation tracked by
// MultidimensionalArrays.java, not by the mixed-rank split.
package example;

public class Program {
    public static void main(String[] args) {
        // The example from issue #100: mixing a 2-D and a 1-D declarator in one declaration.
        int multi[][] = new int[2][2],
            single[] = new int[2];
        multi[0][0] = 1;
        multi[0][1] = 2;
        multi[1][0] = 3;
        multi[1][1] = 4;
        single[0] = 5;

        System.out.println(multi[0][0]);
        System.out.println(multi[0][1]);
        System.out.println(multi[1][0]);
        System.out.println(multi[1][1]);
        System.out.println(single[0]);
    }
}
