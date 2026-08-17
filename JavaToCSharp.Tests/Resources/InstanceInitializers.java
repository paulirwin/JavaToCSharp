package com.example;

import java.util.ArrayList;
import java.util.List;

/**
 * Instance initializer blocks, which Java runs at the start of every constructor that does not
 * chain to another constructor of the same class.
 */
public class InstanceInitializers {
    private final List<String> names;
    private int count;

    {
        names = new ArrayList<String>();
        count = 1;
    }

    public InstanceInitializers() {
    }

    public InstanceInitializers(String first) {
        names.add(first);
        count = 2;
    }

    public InstanceInitializers(String first, String second) {
        this(first);
        names.add(second);
    }

    public int getCount() {
        return count;
    }
}
