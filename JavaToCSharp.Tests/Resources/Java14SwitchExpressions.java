/// Expect:
/// - output: "9\n"
package example;

// https://docs.oracle.com/en/java/javase/14/language/switch-expressions.html#GUID-BA4F63E3-4823-43C6-A5F3-BAA4A2EF3ADC

enum Day { SUNDAY, MONDAY, TUESDAY,
    WEDNESDAY, THURSDAY, FRIDAY, SATURDAY; }

public class Program {
    public static void main(String[] args) {
        Day day = Day.WEDNESDAY;
        System.out.println(
            switch (day) {
                case MONDAY, FRIDAY, SUNDAY -> 6;
                case TUESDAY                -> 7;
                case THURSDAY, SATURDAY     -> 8;
                case WEDNESDAY              -> 9;
                default -> throw new IllegalStateException("Invalid day: " + day);
            }
        );
    }
}
