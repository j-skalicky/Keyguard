using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Keyguard;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    public static extern bool LockWorkStation();

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private static readonly LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static readonly List<int> _keystrokeTimes = [];
    private static int _lastKeystroke = -1;
    private static DateTime _lastKeystrokeTime = DateTime.Now;
    private static bool _attackDetected = false;

    private const int SLIDING_WINDOW_THRESHOLD = 100; // in milliseconds
    private const int SLIDING_WINDOW_SIZE = 7;
    private const bool LOCK_SCREEN = false;
    private const int KEYSTROKES_QUARANTINE_DURATION = 5; // in seconds
    private const IntPtr DUMMY_KEYSTROKE = 1;

    static void Main()
    {
        try
        {
            Console.WriteLine("Starting the program...");
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }
        catch
        {
            Console.WriteLine("Sorry, an unexpected error has occurred. Try again, please.");
        }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (_attackDetected && Math.Floor((DateTime.Now - _lastKeystrokeTime).TotalSeconds) < KEYSTROKES_QUARANTINE_DURATION)
        {
            // eat the keystroke and return
            return DUMMY_KEYSTROKE;
        }

        _attackDetected = false;

        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            if (vkCode != _lastKeystroke)
            {
                _lastKeystroke = vkCode;
                _keystrokeTimes.Add((int)(DateTime.Now - _lastKeystrokeTime).TotalMilliseconds);
                _lastKeystrokeTime = DateTime.Now;
                if (_keystrokeTimes.Count > SLIDING_WINDOW_SIZE)
                {
                    _keystrokeTimes.RemoveAt(0); // remove the first item, as we've just inserted a new one at the end
                    _attackDetected = IsThresholdExceeded();
                }
            }
        }

        if (_attackDetected)
        {
            DoCorrectiveAction();
            return DUMMY_KEYSTROKE;
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using Process curProcess = Process.GetCurrentProcess();
        if (curProcess.MainModule == null || curProcess.MainModule.ModuleName == null)
        {
            throw new Exception();
        }
        using ProcessModule curModule = curProcess.MainModule;

        return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
    }

    private static bool IsThresholdExceeded()
    {
        int average = (int)_keystrokeTimes.Average();
        Console.WriteLine("Current average: {0}", average);
        return average < SLIDING_WINDOW_THRESHOLD;
    }

    private static void DoCorrectiveAction()
    {
        // print a message
        Console.WriteLine("Hey, you're typing faster than any human can probably do. Your keystrokes will be consumed for another {0} seconds.", KEYSTROKES_QUARANTINE_DURATION);

        // if enabled, lock the screen
        if (LOCK_SCREEN)
        {
            LockWorkStation();
        }
    }
}
