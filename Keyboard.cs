using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

public static class Keyboard
{
    private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const int KEYEVENTF_KEYUP = 0x0002;
    private const int WM_KEYDOWN = 0x0100; // Kept as it was in the user's original file
    private const int WM_KEYUP = 0x0101;   // Kept as it was in the user's original file

    [DllImport("user32.dll")]
    private static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(int hWnd, uint Msg, int wParam, int lParam); // Kept as it was in the user's original file

    [DllImport("user32.dll")]
    static extern int MapVirtualKey(uint uCode, uint uMapType); // Re-added for SendString character typing

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch); // Re-added for SendString character typing

    public static void KeyDown(Keys key)
    {
        keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | 0, 0);
    }

    public static void KeyUp(Keys key)
    {
        keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }

    public static void KeyPress(Keys key)
    {
        KeyDown(key);
        Thread.Sleep(10);
        KeyUp(key);
    }

    public static void SendString(string text)
    {
        // Reverted to character-by-character typing due to STA thread requirement for Clipboard operations.
        // The error "Current thread must be set to single thread apart mean sta mode before ole calls can be made"
        // indicates that Clipboard.SetText() cannot be reliably used on the plugin's thread.
        foreach (char c in text)
        {
            // Get the virtual key code for the character
            short vk = VkKeyScan(c);
            byte bVk = (byte)(vk & 0xFF);
            byte bScan = (byte)MapVirtualKey(bVk, 0);

            // Simulate key press
            keybd_event(bVk, bScan, 0, 0); // Key Down
            Thread.Sleep(10); // Small delay
            keybd_event(bVk, bScan, KEYEVENTF_KEYUP, 0); // Key Up
            Thread.Sleep(10); // Small delay between characters
        }
    }
}