package com.example;

import java.util.ArrayList;
import java.util.List;

/**
 * Java 16 local record declarations (JEP 395), which may be declared in a method body.
 */
public class Java16LocalRecords {
    public int sumAreas(List<int[]> pairs) {
        record Rect(int width, int height) {
        }

        List<Rect> rects = new ArrayList<Rect>();

        for (int[] pair : pairs) {
            rects.add(new Rect(pair[0], pair[1]));
        }

        int total = 0;

        for (Rect rect : rects) {
            total += rect.width * rect.height;
        }

        return total;
    }

    public String describe(int a, int b) {
        record Pair(int left, int right) {
        }

        Pair pair = new Pair(a, b);

        return pair.left + "," + pair.right;
    }
}
