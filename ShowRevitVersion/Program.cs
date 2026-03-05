using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OpenMcdf;
using System.Reflection;

[assembly: AssemblyVersion("1.0.*")]
namespace ShowRevitVersion
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string msg = string.Empty;
            string caption = string.Empty;

            if (args.Length < 2)
            {
                string filePath = string.Empty;
                if (args.Length == 0)
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    string filter = "Revit files (*.rvt;*.rfa)|*.rvt;*.rfa|All files (*.*)|*.*";
                    ofd.Filter = filter;
                    ofd.Multiselect = false;
                    ofd.Title = "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    if (ofd.ShowDialog() != DialogResult.OK)
                        return;

                    filePath = ofd.FileName;
                }
                else if (args.Length == 1)
                {
                    filePath = args[0];
                }
                string version = OpenFileAndGetVersion(filePath);
                caption = Path.GetFileName(filePath);
                msg = $"Revit {version}";

            }
            else
            {
                List<string> lines = new List<string>();
                foreach (string s in args)
                {
                    string version = OpenFileAndGetVersion(s);
                    string filename = Path.GetFileName(s);
                    lines.Add($"{filename}: \tRevit {version}");
                }
                msg = string.Join(Environment.NewLine, lines);
            }
            MessageBox.Show(msg, caption);
        }



        private static string OpenFileAndGetVersion(string path)
        {
            string value = "UNKNOWN";
            if (!File.Exists(path))
                throw new FileNotFoundException("File not found: " + path);

            int bytesToRead = 2 * 1024 * 1024;
            byte[] buffer;


            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                int length = (int)Math.Min(bytesToRead, fs.Length);
                buffer = new byte[bytesToRead];
                fs.Read(buffer, 0, length);
            }
            string text = Encoding.UTF8.GetString(buffer);

            Match match = Regex.Match(text, @"product-version>(\d+)<");
            if (match.Success)
            {
                value = match.Groups[1].Value;
            }
            else
            {
                value = GetVersionAsOle(path);
            }
            return value;
        }

        private static string GetVersionAsOle(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File not found: " + path);

            string value = string.Empty;
            using (CompoundFile cf = new CompoundFile(path))
            {
                CFStream stream = cf.RootStorage.GetStream("BasicFileInfo");
                byte[] buffer0 = stream.GetData();
                byte[] buffer = buffer0.Where(b => b != 0).ToArray();
                Encoding encoding = Encoding.UTF8;
                string text = encoding.GetString(buffer);
                Match m = Regex.Match(text, @"Format:\s*(20\d{2})");
                if (m.Success)
                {
                    value = m.Groups[1].Value;
                    value += GetWorksharingText(path, text);
                    return value;
                }

                Match oldVersionsMath = Regex.Match(text, @"Revit\s*(20\d{2})");
                if (oldVersionsMath.Success)
                {
                    value = oldVersionsMath.Groups[1].Value;
                    value += GetWorksharingText(path, text);
                    return value;
                }
            }
            string errMsg = "Failed to determine the Revit version: " + path;
            MessageBox.Show(errMsg);
            throw new Exception(errMsg);
        }

        private static string GetWorksharingText(string path, string text)
        {
            if (path.ToLower().EndsWith(".rvt"))
            {
                Match worksharingMatch = Regex.Match(text, @"orksharing:\s*(.*?)\r");
                if (worksharingMatch.Success)
                {
                    string worksharingText = worksharingMatch.Groups[1].Value;
                    if (!worksharingText.Contains("Not"))
                    {
                        string result = ". Workshared! " + worksharingText + " file";
                        return result;
                    }
                }
            }
            return string.Empty;
        }
    }
}
