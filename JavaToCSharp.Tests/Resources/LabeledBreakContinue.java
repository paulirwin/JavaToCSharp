/// - output: "0,0\n1,0\n2,0\nsearch=1,2\ndone\n"
package example;

public class Program {
    public static void main(String[] args) {
        outer: for (int i = 0; i < 4; i++) {
            for (int j = 0; j < 4; j++) {
                if (j == 1) {
                    continue outer;
                }
                if (i == 3) {
                    break outer;
                }
                System.out.println(i + "," + j);
            }
        }

        int foundRow = -1;
        int foundCol = -1;
        search: for (int row = 0; row < 3; row++) {
            for (int col = 0; col < 3; col++) {
                if (row + col == 3) {
                    foundRow = row;
                    foundCol = col;
                    break search;
                }
            }
        }
        System.out.println("search=" + foundRow + "," + foundCol);

        int k = 0;
        loop: while (k < 5) {
            k++;
            if (k < 5) {
                continue loop;
            }
            break loop;
        }

        System.out.println("done");
    }
}
