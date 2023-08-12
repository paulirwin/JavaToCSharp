// NOTE: this is currently just a successfully-parses test. It does handle `var` successfully through to the C# code,
// but the `import`, `BiFunction`, and `.apply` calls are not successfully translated. Some additional options in the
// conversion process such as skipping incoming imports, BiFunction -> Func, and somehow converting `.Apply` to
// `.Invoke` (without being a global always-on translation) would make this work in the full integration test. 
package example;

import java.util.function.BiFunction;

public class Java11LambdaParameterTypeInference {
    public static void main(String[] args) {
        BiFunction<Integer, Integer, Integer> add = (var a, var b) -> a + b;
        System.out.println(add.apply(1, 2));
    }
}
