using System.IO;
using System.Runtime.InteropServices;

namespace WasteAccountingClient.Services;

/// <summary>
/// Создаёт ярлык на рабочем столе при первом входе (если файла ещё нет).
/// </summary>
public static class DesktopShortcutService
{
    private const string ShortcutFileName = "Учет отходов.lnk";
    private const string LegacyShortcutFileName = "WasteAccounting.lnk";

    public static void TryCreateDesktopShortcutIfMissing()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
                return;

            exePath = Path.GetFullPath(exePath);
            if (!File.Exists(exePath))
                return;

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop))
                return;

            var linkPath = Path.Combine(desktop, ShortcutFileName);
            if (File.Exists(linkPath))
                return;

            var legacyPath = Path.Combine(desktop, LegacyShortcutFileName);
            if (File.Exists(legacyPath))
            {
                try { File.Delete(legacyPath); }
                catch { /* старый ярлык занят — создадим новый рядом */ }
            }

            var shellType = Type.GetTypeFromProgID("WScript.Shell", throwOnError: false);
            if (shellType == null)
                return;

            var shell = Activator.CreateInstance(shellType);
            if (shell == null)
                return;

            object? shortcut = null;
            try
            {
                dynamic wsh = shell;
                shortcut = wsh.CreateShortcut(linkPath);
                dynamic sc = shortcut;
                sc.TargetPath = exePath;
                sc.WorkingDirectory = Path.GetDirectoryName(exePath) ?? "";
                sc.WindowStyle = 1;
                sc.Description = "Учет отходов";
                sc.IconLocation = $"{exePath},0";
                sc.Save();
            }
            finally
            {
                if (shortcut != null && Marshal.IsComObject(shortcut))
                    Marshal.FinalReleaseComObject(shortcut);
                if (Marshal.IsComObject(shell))
                    Marshal.FinalReleaseComObject(shell);
            }
        }
        catch
        {
            // Нет прав, политика организации, нестандартный рабочий стол — тихо пропускаем.
        }
    }
}
