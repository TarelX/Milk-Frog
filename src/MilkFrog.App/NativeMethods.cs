using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace MilkFrog.App;

internal static class NativeMethods
{
    internal const int GwlExStyle = -20, WsExTopmost = 0x8, WsExToolWindow = 0x80, WsExNoActivate = 0x08000000;
    internal const int WmDpiChanged = 0x02E0, WmDisplayChange = 0x007E, WmMouseActivate = 0x0021, WmWindowPosChanging = 0x0046;
    internal const uint SwpNoSize = 1, SwpNoMove = 2, SwpNoZOrder = 4, SwpNoActivate = 0x10;
    internal static readonly nint HwndTopmost = new(-1), HwndNotTopmost = new(-2);
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct Rect
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct WindowPos
    {
        public nint Window, InsertAfter;
        public int X, Y, Width, Height;
        public uint Flags;
    }
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(nint window, out Rect rect);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(nint window, nint after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll", EntryPoint="GetWindowLongW")] internal static extern int GetWindowLong(nint window, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongW")] internal static extern int SetWindowLong(nint window, int index, int value);
    [DllImport("user32.dll")] internal static extern int SetWindowRgn(nint window, nint region, bool redraw);
    [DllImport("gdi32.dll")] private static extern nint ExtCreateRegion(nint transform, uint count, byte[] data);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint handle);
    [DllImport("user32.dll")] internal static extern bool DestroyIcon(nint icon);

    internal static void ApplyTopmost(nint window, bool enabled)
    {
        if (window == 0) return;
        SetWindowPos(window, enabled ? HwndTopmost : HwndNotTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    internal static bool IsTopmost(nint window) => window != 0 && (GetWindowLong(window, GwlExStyle) & WsExTopmost) != 0;

    internal static void PreserveTopmost(nint positionPointer, bool enabled)
    {
        if (!enabled || positionPointer == 0) return;
        var position = Marshal.PtrToStructure<WindowPos>(positionPointer);
        // Ordinary moves keep their Z order; only guard requests that actually reorder the window.
        if ((position.Flags & SwpNoZOrder) != 0 || position.InsertAfter == HwndTopmost) return;
        position.InsertAfter = HwndTopmost;
        Marshal.StructureToPtr(position, positionPointer, false);
    }

    // Give Windows a silhouette rather than a rectangular invisible mouse-blocking window.
    internal static void ApplyRegion(nint window, SpriteBitmap sprite, bool rectangle = false, bool includeSleepMarks = false)
    {
        if (!GetWindowRect(window, out var bounds) || bounds.Width <= 0 || bounds.Height <= 0) return;
        if (rectangle) { SetWindowRgn(window, 0, false); return; }
        int width = bounds.Width, height = bounds.Height;
        var rectangles = new List<Rect>();
        for (int y = 0; y < height; y += 2)
        {
            int sy = Math.Clamp(y * sprite.Height / height, 0, sprite.Height - 1), start = -1;
            for (int x = 0; x <= width; x++)
            {
                bool filled = x < width && sprite.Alpha[sy * sprite.Width + Math.Min(sprite.Width - 1, x * sprite.Width / width)] > 16;
                if (filled && start < 0) start = x;
                else if (!filled && start >= 0) { rectangles.Add(new() { Left = start, Top = y, Right = x, Bottom = Math.Min(height, y + 2) }); start = -1; }
            }
        }
        if (includeSleepMarks)
        {
            rectangles.Add(new() { Left = width * 61 / 100, Top = height * 31 / 100, Right = width * 71 / 100, Bottom = height * 43 / 100 });
            rectangles.Add(new() { Left = width * 70 / 100, Top = height * 18 / 100, Right = width * 82 / 100, Bottom = height * 31 / 100 });
            rectangles.Add(new() { Left = width * 80 / 100, Top = height * 5 / 100, Right = width * 94 / 100, Bottom = height * 20 / 100 });
        }
        if (rectangles.Count == 0) return;
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write(32); writer.Write(1); writer.Write(rectangles.Count); writer.Write(rectangles.Count * 16);
        writer.Write(0); writer.Write(0); writer.Write(width); writer.Write(height);
        foreach (var rect in rectangles) { writer.Write(rect.Left); writer.Write(rect.Top); writer.Write(rect.Right); writer.Write(rect.Bottom); }
        var data = stream.ToArray(); var region = ExtCreateRegion(0, (uint)data.Length, data);
        if (region != 0 && SetWindowRgn(window, region, true) == 0) DeleteObject(region);
    }
}
