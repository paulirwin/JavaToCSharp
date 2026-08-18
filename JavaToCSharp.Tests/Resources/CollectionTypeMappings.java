/// Expect:
/// - output: "1\n2\nTrue\nTrue\na\n"
package example;

public class Program {
    public static void main(String[] args) {
        // A variable declared against the java interface must convert to the .NET interface,
        // while the concrete implementation it is assigned from must stay instantiable.
        Map<String, Integer> counts = new HashMap<String, Integer>();
        counts.put("a", 1);
        System.out.println(counts.get("a"));

        Map<String, Integer> sorted = new TreeMap<String, Integer>();
        sorted.put("b", 2);
        System.out.println(sorted.get("b"));

        Set<String> set = new HashSet<String>();
        set.add("x");
        System.out.println(set.contains("x"));

        Set<String> sortedSet = new TreeSet<String>();
        sortedSet.add("y");
        System.out.println(sortedSet.contains("y"));

        List<String> list = new ArrayList<String>();
        list.add("a");
        System.out.println(list.get(0));
    }
}
