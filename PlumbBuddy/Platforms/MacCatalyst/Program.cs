using ObjCRuntime;
using UIKit;

namespace PlumbBuddy.Platforms.MacCatalyst;

public static class Program
{
    internal static AppLifecycleManager AppLifecycleManager { get; } = new();

    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
