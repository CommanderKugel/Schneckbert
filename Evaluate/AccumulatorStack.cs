using static Constants;
using static Accumulator;

public static class AccStack
{
    public static Accumulator[] stack;

    public static void init() => stack = new Accumulator[MAX_SEARCH_PLY];

    public static void Reset() => Array.Clear(stack);
}
