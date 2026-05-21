public static class GameInputGate
{
    public static bool AllowPlayerInput { get; private set; } = true;

    public static void Lock()
    {
        AllowPlayerInput = false;
    }

    public static void Unlock()
    {
        AllowPlayerInput = true;
    }
}
