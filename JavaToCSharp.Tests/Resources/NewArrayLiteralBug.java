/// Expect:
/// - output: "0\n1\n"
package example;

// see issue #38
public class Program {
    static float[] mFirstByteFreq;
    public static void main(String[] args) {
        mFirstByteFreq = new float[] { 
            0.000000f,
            1.000000f
        };
        
        System.out.println(mFirstByteFreq[0]);
        System.out.println(mFirstByteFreq[1]);
    }
}