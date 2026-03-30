using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ShowRevitVersionInstaller
{
    internal static class Program
    {
        static readonly string InstallDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShowRevitVersion");

        static readonly string ExePath = Path.Combine(InstallDir, "ShowRevitVersion.exe");

        // Localized label shown in the context menu and "Open with" list
        static readonly string VerbLabel = GetLocalizedLabel();

        [STAThread]
        static void Main(string[] args)
        {
            bool uninstall = args.Length > 0 &&
                args[0].Equals("/uninstall", StringComparison.OrdinalIgnoreCase);

            if (uninstall)
                Uninstall();
            else
                Install();
        }

        static void Install()
        {
            try
            {
                Directory.CreateDirectory(InstallDir);

                string resourceName = "ShowRevitVersionInstaller.payload.ShowRevitVersion.exe";
                using (Stream src = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (src == null)
                        throw new Exception("Embedded payload not found: " + resourceName);

                    using (FileStream dst = new FileStream(ExePath, FileMode.Create, FileAccess.Write))
                        src.CopyTo(dst);
                }

                RegisterContextMenu("Revit.Project");
                RegisterContextMenu("Revit.Family");
                RegisterOpenWith(".rvt");
                RegisterOpenWith(".rfa");

                MessageBox.Show(
                    VerbLabel + "\n\n" +
                    GetLocalizedSuccess() + "\n\n" +
                    InstallDir,
                    GetLocalizedSuccessTitle(),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Installation failed:\n\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void Uninstall()
        {
            try
            {
                RemoveContextMenu("Revit.Project");
                RemoveContextMenu("Revit.Family");
                RemoveOpenWith(".rvt");
                RemoveOpenWith(".rfa");
                Registry.CurrentUser.DeleteSubKeyTree(
                    @"Software\Classes\Applications\ShowRevitVersion.exe",
                    throwOnMissingSubKey: false);

                if (Directory.Exists(InstallDir))
                    Directory.Delete(InstallDir, true);

                MessageBox.Show(GetLocalizedUninstalled(),
                    "ShowRevitVersion", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Uninstall failed:\n\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Legacy context menu ("Show more options")
        static void RegisterContextMenu(string fileType)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{fileType}\shell\ShowRevitVersion"))
                key.SetValue("", VerbLabel);

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{fileType}\shell\ShowRevitVersion\command"))
                key.SetValue("", $"\"{ExePath}\" \"%1\"");
        }

        static void RemoveContextMenu(string fileType)
        {
            Registry.CurrentUser.DeleteSubKeyTree(
                $@"Software\Classes\{fileType}\shell\ShowRevitVersion",
                throwOnMissingSubKey: false);
        }

        // "Open with" registration — appears in the Windows 11 modern context menu
        static void RegisterOpenWith(string extension)
        {
            // Register the application
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                @"Software\Classes\Applications\ShowRevitVersion.exe"))
                key.SetValue("FriendlyAppName", VerbLabel);

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                @"Software\Classes\Applications\ShowRevitVersion.exe\shell\open\command"))
                key.SetValue("", $"\"{ExePath}\" \"%1\"");

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                @"Software\Classes\Applications\ShowRevitVersion.exe\SupportedTypes"))
            {
                key.SetValue(".rvt", "");
                key.SetValue(".rfa", "");
            }

            // Add to OpenWithList for this extension
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\OpenWithList"))
            {
                key.SetValue("ShowRevitVersion.exe", "");
            }
        }

        static void RemoveOpenWith(string extension)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\OpenWithList", writable: true))
                key?.DeleteValue("ShowRevitVersion.exe", throwOnMissingValue: false);
        }

        // --- Localization ---

        static string GetLocalizedLabel()
        {
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "fr": return "Afficher la version Revit";
                case "de": return "Revit-Version anzeigen";
                case "es": return "Mostrar versión de Revit";
                case "it": return "Mostra versione Revit";
                case "pt": return "Mostrar versão do Revit";
                case "nl": return "Revit-versie weergeven";
                case "pl": return "Pokaż wersję Revit";
                case "ru": return "Показать версию Revit";
                case "ja": return "Revitバージョンを表示";
                case "zh": return "显示Revit版本";
                default:   return "Show Revit Version";
            }
        }

        static string GetLocalizedSuccess()
        {
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "fr": return "Clic droit sur un fichier .rvt ou .rfa pour l'utiliser.";
                case "de": return "Rechtsklick auf eine .rvt- oder .rfa-Datei zum Verwenden.";
                case "es": return "Haga clic derecho en un archivo .rvt o .rfa para usarlo.";
                default:   return "Right-click any .rvt or .rfa file to use it.";
            }
        }

        static string GetLocalizedSuccessTitle()
        {
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "fr": return "Installation réussie";
                case "de": return "Installation abgeschlossen";
                case "es": return "Instalación completa";
                default:   return "Installation Complete";
            }
        }

        static string GetLocalizedUninstalled()
        {
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "fr": return "ShowRevitVersion a été désinstallé.";
                case "de": return "ShowRevitVersion wurde deinstalliert.";
                case "es": return "ShowRevitVersion ha sido desinstalado.";
                default:   return "ShowRevitVersion has been uninstalled.";
            }
        }
    }
}
