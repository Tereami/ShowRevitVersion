using System;
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

                MessageBox.Show(
                    "ShowRevitVersion installed successfully!\n\n" +
                    "Right-click any .rvt or .rfa file in Explorer to use it.\n\n" +
                    "Installed to:\n" + InstallDir,
                    "Installation Complete",
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

                if (Directory.Exists(InstallDir))
                    Directory.Delete(InstallDir, true);

                MessageBox.Show("ShowRevitVersion has been uninstalled.",
                    "Uninstall Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Uninstall failed:\n\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void RegisterContextMenu(string fileType)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{fileType}\shell\ShowRevitVersion"))
                key.SetValue("", "Show Revit Version");

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
    }
}
