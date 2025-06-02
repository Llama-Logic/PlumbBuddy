using UIKit;

namespace PlumbBuddy.Platforms.MacCatalyst;

public class AppLifecycleManager :
    IAppLifecycleManager
{
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_getClass")]
    static extern IntPtr objc_getClass(string className);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    static extern IntPtr sel_registerName(string selectorName);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    static extern void void_objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool arg);

    public bool HideMainWindowAtLaunch =>
        false;

    public bool IsForeground =>
        UIApplication.SharedApplication.ApplicationState is UIApplicationState.Active;

    public bool IsVisible =>
        true;

    public bool PreventCasualClosing { get; set; }

    public Task UiReleaseSignal =>
        Task.CompletedTask;

    public void HideWindow()
    {
    }

    public void ShowWindow()
    {
        var nsAppClass = objc_getClass("NSApplication");
        var sharedAppSel = sel_registerName("sharedApplication");
        var activateSel = sel_registerName("activateIgnoringOtherApps:");

        var nsApp = IntPtr_objc_msgSend(nsAppClass, sharedAppSel);
        void_objc_msgSend_bool(nsApp, activateSel, true);
    }

    public void WindowFirstShown(Window window)
    {
    }
}