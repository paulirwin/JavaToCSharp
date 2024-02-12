// NOTE: this test case only parses and converts successfully, it does not yet work.
// The multidimensional array indexers do not translate to the correct syntax.
package example;

public class Program {
    private static int[][] multiField = new int[2][2];
    private static int[] singleField = new int[2];

    static {
        multiField[0][0] = 10;
        multiField[0][1] = 20;
        multiField[1][0] = 30;
        multiField[1][1] = 40;

        singleField[0] = 11;
        singleField[1] = 21;
    }

    public static void main(String[] args) {
        int multi[][] = new int[2][2];
        int single[] = new int[2];

        multi[0][0] = 1;
        multi[0][1] = 2;
        multi[1][0] = 3;
        multi[1][1] = 4;

        System.out.println(multi[0][0]);
        System.out.println(multi[0][1]);
        System.out.println(multi[1][0]);
        System.out.println(multi[1][1]);

        single[0] = 1;
        single[1] = 2;

        System.out.println(single[0]);
        System.out.println(single[1]);

        System.out.println(multiField[0][0]);
        System.out.println(multiField[0][1]);
        System.out.println(multiField[1][0]);
        System.out.println(multiField[1][1]);

        System.out.println(singleField[0]);
        System.out.println(singleField[1]);
    }
}
