// NOTE: This example when transformed to C# does not successfully compile yet, however we can somewhat successfully
// parse it. Anonymous types always need a little bit of hand-holding after conversion anyways.
package example;

public class Program {
   public static void main(String[] args) {
      Handler<Integer> intHandler = new Handler<>(1) {
         @Override
         public void handle() {
            System.out.println(content);
         }
      };
      intHandler.handle();
      Handler<? extends Number> intHandler1 = new Handler<>(2) {
         @Override
         public void handle() {
            System.out.println(content);
         }
      };
      intHandler1.handle();
      Handler<?> handler = new Handler<>("test") {
         @Override
         public void handle() {
            System.out.println(content);
         }
      };

      handler.handle();    
   }  
}

abstract class Handler<T> {
   public T content;

   public Handler(T content) {
      this.content = content; 
   }
   
   abstract void handle();
}