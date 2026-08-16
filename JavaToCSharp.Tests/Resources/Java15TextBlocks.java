/// Expect:
/// - output: "<html>\n    <body>hi</body>\n</html>\n|he said \"\"quoted\"\" ok\n|a b\tc\n"
package example;

// https://docs.oracle.com/en/java/javase/15/language/text-blocks.html

public class Program {
    public static void main(String[] args) {
        String html = """
                <html>
                    <body>hi</body>
                </html>
                """;

        // Two adjacent quotes are legal inside a text block, and require a longer
        // delimiter when converted to a C# raw string literal.
        String quotes = """
                he said ""quoted"" ok
                """;

        // \s keeps a trailing space, a trailing backslash joins lines, and \t is a tab.
        String escapes = """
                a\s\
                b\tc
                """;

        System.out.print(html);
        System.out.print("|");
        System.out.print(quotes);
        System.out.print("|");
        System.out.print(escapes);
    }
}
