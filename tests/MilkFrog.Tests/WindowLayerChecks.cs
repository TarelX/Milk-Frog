using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using MilkFrog.App;

internal static class WindowLayerChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();

    internal static void Run()
    {
        using var window = new HwndSource(new HwndSourceParameters("MilkFrog hidden layer regression")
        {
            WindowStyle = unchecked((int)0x80000000),
            ExtendedWindowStyle = NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate,
            PositionX = -10000, PositionY = -10000, Width = 120, Height = 120
        });
        bool enabled = true;
        window.AddHook((nint hwnd, int message, nint w, nint l, ref bool handled) =>
        {
            if (message == NativeMethods.WmWindowPosChanging) NativeMethods.PreserveTopmost(l, enabled);
            return 0;
        });

        nint foreground = GetForegroundWindow();
        NativeMethods.GetWindowRect(window.Handle, out var before);
        NativeMethods.ApplyTopmost(window.Handle, true);
        Require(NativeMethods.IsTopmost(window.Handle), "topmost did not enable");

        NativeMethods.SetWindowPos(window.Handle, NativeMethods.HwndNotTopmost, 0, 0, 0, 0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate);
        Require(NativeMethods.IsTopmost(window.Handle), "layer change removed enabled topmost");

        enabled = false;
        NativeMethods.ApplyTopmost(window.Handle, false);
        Require(!NativeMethods.IsTopmost(window.Handle), "topmost setting could not be disabled");

        // Simulate native state loss, then the same recovery path used by the pet clock.
        enabled = true;
        NativeMethods.ApplyTopmost(window.Handle, true);
        Require(NativeMethods.IsTopmost(window.Handle), "native topmost recovery failed");
        NativeMethods.GetWindowRect(window.Handle, out var after);
        Require(before.Left == after.Left && before.Top == after.Top && before.Width == after.Width && before.Height == after.Height,
            "layer synchronization moved or resized the pet");
        Require(GetForegroundWindow() == foreground, "layer synchronization stole application focus");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
