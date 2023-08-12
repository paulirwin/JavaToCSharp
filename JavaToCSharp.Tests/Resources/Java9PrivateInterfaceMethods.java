/// Expect:
/// - output: "Hello world!\n"
package example;

public class Program {
    public static void main(String[] args) {
        InterfaceWithPrivateMethod implementation = new Implementation();
        implementation.defaultMethod();
    }
}

class Implementation implements InterfaceWithPrivateMethod {
}

interface InterfaceWithPrivateMethod {
    default void defaultMethod() {
        privateMethod();
    }
    private void privateMethod() {
        System.out.println("Hello world!");
    }
}