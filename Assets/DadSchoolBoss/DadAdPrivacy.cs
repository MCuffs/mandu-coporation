using System;

// UI stays SDK-independent so the offline/headless builds remain supported.
public static class DadAdPrivacy
{
    public static bool Busy;
    public static Func<bool> Required;
    public static Action Open;
    public static bool Available { get { return Required != null && Required(); } }
    public static void Show() { if (!Busy && Available && Open != null) Open(); }
}
