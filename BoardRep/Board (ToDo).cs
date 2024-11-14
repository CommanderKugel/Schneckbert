
public static class Board
{
    public const int MAX_GAME_LENGTH = 1024;
    public const int MAX_SEARCH_DEPTH = 128;
    public static int Ply;


    public static void init()
    {
        Ply = 0;
    }

    public static void Reset()
    {
        Ply = 0;
    }
}
