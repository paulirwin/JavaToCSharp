/// Expect:
/// - output: "Creating TryableResource 1\nCreating TryableResource 2\nHello world!\nClosing TryableResource 2\nClosing TryableResource 1\nFinally\n"
package example;

public class Program {
    public static void main(String[] args) {
        try (TryableResource resource1 = new TryableResource();
             TryableResource resource2 = new TryableResource()) {
            System.out.println("Hello world!");
        } catch (Exception ignored) {
        } finally {
            System.out.println("Finally");
        }
    }

    public static class TryableResource implements AutoCloseable {
        private static int counter = 1;
        private final int id = counter++;
        public TryableResource() {
            System.out.println("Creating TryableResource " + id);
        }
        @Override
        public void close() throws Exception {
            System.out.println("Closing TryableResource " + id);
        }
    }
}