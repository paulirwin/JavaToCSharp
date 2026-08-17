/// Expect:
/// - output: "a: count=1 tag=init\nb: count=2 tag=x\nc: count=2 tag=x-y\nd: count=1 tag=init\nstatic=7\n"
package example;

// Instance initializer blocks run at the start of every constructor that does not chain to another
// constructor of the same class. A constructor that chains via this(...) must not re-run them.
public class Program {
    static int staticValue;

    static {
        staticValue = 7;
    }

    public static class Counter {
        public String tag;
        public int count;

        // Two separate initializer blocks, to verify both run and in declaration order.
        {
            tag = "init";
        }

        {
            count = 1;
        }

        public Counter() {
        }

        public Counter(String first) {
            tag = first;
            count = 2;
        }

        // Chains to Counter(String), so the initializers already ran there and must not run again.
        public Counter(String first, String second) {
            this(first);
            tag = tag + "-" + second;
        }
    }

    // A class with an initializer but no declared constructor needs one synthesized, or the
    // initializer would be dropped entirely.
    public static class Implicit {
        public String tag;
        public int count;

        {
            tag = "init";
            count = 1;
        }
    }

    public static void describe(String label, int count, String tag) {
        System.out.println(label + ": count=" + count + " tag=" + tag);
    }

    public static void main(String[] args) {
        Counter a = new Counter();
        describe("a", a.count, a.tag);

        Counter b = new Counter("x");
        describe("b", b.count, b.tag);

        // count stays 2 rather than resetting to 1, because the chained constructor did not re-run
        // the initializer.
        Counter c = new Counter("x", "y");
        describe("c", c.count, c.tag);

        Implicit d = new Implicit();
        describe("d", d.count, d.tag);

        System.out.println("static=" + staticValue);
    }
}
