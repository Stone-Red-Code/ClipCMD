using System;
using System.Diagnostics;

namespace ClipCMD;

internal static class SafeRepeat
{
    public static bool Start(Action action, int repeatCount)
    {
        for (int i = 0; i < repeatCount; i++)
        {
            try
            {
                action.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        return false;
    }

    public static bool Start<T>(Func<T> action, int repeatCount, out T? result)
    {
        for (int i = 0; i < repeatCount; i++)
        {
            try
            {
                result = action.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        result = default;
        return false;
    }
}