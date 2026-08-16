/// Expect:
/// - output: "9\n9\n8\n"
package example;

// https://docs.oracle.com/en/java/javase/14/language/switch-expressions.html#GUID-BA4F63E3-4823-43C6-A5F3-BAA4A2EF3ADC__GUID-4900EB1C-3832-4CB8-ACAE-A87675260B75

enum Day { SUNDAY, MONDAY, TUESDAY,
    WEDNESDAY, THURSDAY, FRIDAY, SATURDAY; }

public class Program {
    public static void main(String[] args) {
        Day day = Day.WEDNESDAY;

        // colon form with yield
        int numLetters = switch (day) {
            case MONDAY:
            case FRIDAY:
            case SUNDAY:
                System.out.println(6);
                yield 6;
            case TUESDAY:
                System.out.println(7);
                yield 7;
            case THURSDAY:
            case SATURDAY:
                System.out.println(8);
                yield 8;
            case WEDNESDAY:
                yield 9;
            default:
                throw new IllegalStateException("Invalid day: " + day);
        };
        System.out.println(numLetters);

        // arrow form with a block body and yield
        int arrowLetters = switch (day) {
            case MONDAY, FRIDAY, SUNDAY -> 6;
            case TUESDAY -> 7;
            case THURSDAY, SATURDAY -> 8;
            case WEDNESDAY -> {
                int nine = 9;
                yield nine;
            }
            default -> throw new IllegalStateException("Invalid day: " + day);
        };
        System.out.println(arrowLetters);

        // arrow form with a block body, yield, and multiple statements
        Day saturday = Day.SATURDAY;
        int blockLetters = switch (saturday) {
            case WEDNESDAY -> 9;
            default -> {
                int eight = 4;
                eight = eight * 2;
                yield eight;
            }
        };
        System.out.println(blockLetters);
    }
}
