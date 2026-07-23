using MessagePack;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PlumbBuddy.Platforms.Windows;

[MessagePackObject]
public class SavedWindowPlacement
(
    string displayConfigurationHashHex,
    int maxPositionX,
    int maxPositionY,
    int minPositionX,
    int minPositionY,
    int normalPositionLeft,
    int normalPositionTop,
    int normalPositionRight,
    int normalPositionBottom,
    int showCommand,
    uint flags
)
{
    internal static SavedWindowPlacement FromWin32WindowPlacement(string displayConfigurationHashHex, WINDOWPLACEMENT windowPlacement) =>
        new
        (
            displayConfigurationHashHex,
            windowPlacement.ptMaxPosition.X,
            windowPlacement.ptMaxPosition.Y,
            windowPlacement.ptMinPosition.X,
            windowPlacement.ptMinPosition.Y,
            windowPlacement.rcNormalPosition.left,
            windowPlacement.rcNormalPosition.top,
            windowPlacement.rcNormalPosition.right,
            windowPlacement.rcNormalPosition.bottom,
            (int)windowPlacement.showCmd,
            (uint)windowPlacement.flags
        );

    [Key(0)]
    public string DisplayConfigurationHashHex { get; } = displayConfigurationHashHex;

    [Key(10)]
    public uint Flags { get; } = flags;

    [Key(1)]
    public int MaxPositionX { get; } = maxPositionX;

    [Key(2)]
    public int MaxPositionY { get; } = maxPositionY;

    [Key(3)]
    public int MinPositionX { get; } = minPositionX;

    [Key(4)]
    public int MinPositionY { get; } = minPositionY;

    [Key(8)]
    public int NormalPositionBottom { get; } = normalPositionBottom;

    [Key(5)]
    public int NormalPositionLeft { get; } = normalPositionLeft;

    [Key(7)]
    public int NormalPositionRight { get; } = normalPositionRight;

    [Key(6)]
    public int NormalPositionTop { get; } = normalPositionTop;

    [Key(9)]
    public int ShowCommand { get; } = showCommand;

    internal WINDOWPLACEMENT ToWin32WindowPlacement() =>
        new()
        {
            flags = (WINDOWPLACEMENT_FLAGS)Flags,
            length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>(),
            ptMaxPosition = new(MaxPositionX, MaxPositionY),
            ptMinPosition = new(MinPositionX, MinPositionY),
            rcNormalPosition = new(NormalPositionLeft, NormalPositionTop, NormalPositionRight, NormalPositionBottom),
            showCmd = (SHOW_WINDOW_CMD)ShowCommand,
        };
}