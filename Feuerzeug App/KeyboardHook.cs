using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Feuerzeug;

public sealed class KeyboardHook : IDisposable
{
    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    static extern bool UnregisterHotKey(nint hWnd, int id);

    readonly Window window = new();

    int currentId;

    public event EventHandler<KeyPressedEventArgs> KeyPressed;

    public KeyboardHook()
    {
        window.KeyPressed += (sender, e) => KeyPressed?.Invoke(this, e);
    }

    public void RegisterHotKey(ModifierKeys modifier, Keys key)
    {
        currentId++;

        if (!RegisterHotKey(window.Handle, currentId, (uint) modifier, (uint) key))
        {
            throw new InvalidOperationException("Couldn't register the hot key.");
        }
    }

    public void Dispose()
    {
        for (int i = currentId; i > 0; i--)
        {
            UnregisterHotKey(window.Handle, i);
        }

        window.Dispose();
    }

    [Flags]
    public enum ModifierKeys : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }

    public class KeyPressedEventArgs(ModifierKeys modifier, Keys key) : EventArgs
    {
        public readonly ModifierKeys modifier = modifier;
        public readonly Keys key = key;
    }

    class Window : NativeWindow, IDisposable
    {
        const int WM_HOTKEY = 0x0312;

        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        public Window()
        {
            CreateHandle(new());
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY)
            {
                var key = (Keys) ((int) m.LParam >> 16 & 0xFFFF);
                var modifier = (ModifierKeys) ((int) m.LParam & 0xFFFF);

                KeyPressed?.Invoke(this, new(modifier, key));
            }
        }

        public void Dispose()
        {
            DestroyHandle();
        }
    }

}
