using ObjCRuntime;
using UIKit;

namespace PlumbBuddy.App.Platforms.MacCatalyst;

public class Program
{
    internal static AppLifecycleManager AppLifecycleManager { get; private set; }

    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        AppLifecycleManager = new();
        
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
