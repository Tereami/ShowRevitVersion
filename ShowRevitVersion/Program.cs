using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        [STAThread]
        static void Main(string[] args)
        {
            // Load dependency DLLs from embedded resources so the exe works as a single file
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                string dllName = new AssemblyName(e.Name).Name + ".dll";
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(dllName);
                if (s == null) return null;
                byte[] data = new byte[s.Length];
                s.Read(data, 0, data.Length);
                return Assembly.Load(data);
            };

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
                    ofd.Filter = L.FileFilter;
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
            if (!File.Exists(path))
                throw new FileNotFoundException(string.Format(L.FileNotFound, path));

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
                return match.Groups[1].Value;

            return GetVersionAsOle(path);
        }

        private static string GetVersionAsOle(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(string.Format(L.FileNotFound, path));

            using (CompoundFile cf = new CompoundFile(path))
            {
                CFStream stream = cf.RootStorage.GetStream("BasicFileInfo");
                byte[] buffer0 = stream.GetData();
                byte[] buffer = buffer0.Where(b => b != 0).ToArray();
                string text = Encoding.UTF8.GetString(buffer);

                Match m = Regex.Match(text, @"Format:\s*(20\d{2})");
                if (m.Success)
                    return m.Groups[1].Value + GetWorksharingText(path, text);

                Match oldVersionsMatch = Regex.Match(text, @"Revit\s*(20\d{2})");
                if (oldVersionsMatch.Success)
                    return oldVersionsMatch.Groups[1].Value + GetWorksharingText(path, text);
            }

            string errMsg = string.Format(L.FailedVersion, path);
            MessageBox.Show(errMsg, "ShowRevitVersion", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw new Exception(errMsg);
        }

        private static string GetWorksharingText(string path, string text)
        {
            if (path.ToLower().EndsWith(".rvt"))
            {
                Match m = Regex.Match(text, @"orksharing:\s*(.*?)\r");
                if (m.Success && !m.Groups[1].Value.Contains("Not"))
                    return string.Format(L.Workshared, L.WorksharingType(m.Groups[1].Value));
            }
            return string.Empty;
        }
    }

    internal static class L
    {
        static readonly string Lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        static string Pick(string en, string fr, string de = null, string es = null,
            string it = null, string pt = null, string nl = null,
            string pl = null, string ru = null, string ja = null, string zh = null)
        {
            switch (Lang)
            {
                case "fr": return fr;
                case "de": return de ?? en;
                case "es": return es ?? en;
                case "it": return it ?? en;
                case "pt": return pt ?? en;
                case "nl": return nl ?? en;
                case "pl": return pl ?? en;
                case "ru": return ru ?? en;
                case "ja": return ja ?? en;
                case "zh": return zh ?? en;
                default:   return en;
            }
        }

        public static string FileFilter => Pick(
            en: "Fichiers Revit (*.rvt;*.rfa)|*.rvt;*.rfa|Tous les fichiers (*.*)|*.*",
            fr: "Fichiers Revit (*.rvt;*.rfa)|*.rvt;*.rfa|Tous les fichiers (*.*)|*.*",
            de: "Revit-Dateien (*.rvt;*.rfa)|*.rvt;*.rfa|Alle Dateien (*.*)|*.*",
            es: "Archivos Revit (*.rvt;*.rfa)|*.rvt;*.rfa|Todos los archivos (*.*)|*.*",
            it: "File Revit (*.rvt;*.rfa)|*.rvt;*.rfa|Tutti i file (*.*)|*.*",
            pt: "Ficheiros Revit (*.rvt;*.rfa)|*.rvt;*.rfa|Todos os ficheiros (*.*)|*.*",
            nl: "Revit-bestanden (*.rvt;*.rfa)|*.rvt;*.rfa|Alle bestanden (*.*)|*.*",
            pl: "Pliki Revit (*.rvt;*.rfa)|*.rvt;*.rfa|Wszystkie pliki (*.*)|*.*",
            ru: "Файлы Revit (*.rvt;*.rfa)|*.rvt;*.rfa|Все файлы (*.*)|*.*",
            ja: "Revitファイル (*.rvt;*.rfa)|*.rvt;*.rfa|すべてのファイル (*.*)|*.*",
            zh: "Revit文件 (*.rvt;*.rfa)|*.rvt;*.rfa|所有文件 (*.*)|*.*"
        );

        // "{0}" = translated worksharing type (e.g. "central", "local")
        public static string Workshared => Pick(
            en: ". Workshared! {0} file",
            fr: ". Partagé\u00a0! {0}",
            de: ". Gemeinsam genutzt! {0}",
            es: ". ¡Compartido! {0}",
            it: ". Condiviso! {0}",
            pt: ". Partilhado! {0}",
            nl: ". Gedeeld! {0}",
            pl: ". Udostępniony! {0}",
            ru: ". Общий доступ! {0}",
            ja: ". ワークシェアリング済み！{0}",
            zh: ". 已共享！{0}"
        );

        public static string WorksharingType(string rawValue)
        {
            bool isCentral = rawValue.IndexOf("Central", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isLocal   = rawValue.IndexOf("Local",   StringComparison.OrdinalIgnoreCase) >= 0;

            switch (Lang)
            {
                case "fr": return isCentral ? "fichier central" : isLocal ? "fichier local" : rawValue;
                case "de": return isCentral ? "Zentraldatei"   : isLocal ? "lokale Datei"  : rawValue;
                case "es": return isCentral ? "archivo central": isLocal ? "archivo local"  : rawValue;
                case "it": return isCentral ? "file centrale"  : isLocal ? "file locale"    : rawValue;
                case "pt": return isCentral ? "ficheiro central": isLocal ? "ficheiro local" : rawValue;
                case "nl": return isCentral ? "centraal bestand": isLocal ? "lokaal bestand": rawValue;
                case "pl": return isCentral ? "plik centralny" : isLocal ? "plik lokalny"   : rawValue;
                case "ru": return isCentral ? "центральный файл": isLocal ? "локальный файл": rawValue;
                case "ja": return isCentral ? "セントラルファイル": isLocal ? "ローカルファイル": rawValue;
                case "zh": return isCentral ? "中心文件"        : isLocal ? "本地文件"       : rawValue;
                default:   return isCentral ? "Central file"   : isLocal ? "Local file"     : rawValue;
            }
        }

        public static string FileNotFound => Pick(
            en: "File not found: {0}",
            fr: "Fichier introuvable\u00a0: {0}",
            de: "Datei nicht gefunden: {0}",
            es: "Archivo no encontrado: {0}",
            it: "File non trovato: {0}",
            pt: "Ficheiro não encontrado: {0}",
            nl: "Bestand niet gevonden: {0}",
            pl: "Nie znaleziono pliku: {0}",
            ru: "Файл не найден: {0}",
            ja: "ファイルが見つかりません: {0}",
            zh: "找不到文件：{0}"
        );

        public static string FailedVersion => Pick(
            en: "Failed to determine the Revit version: {0}",
            fr: "Impossible de déterminer la version Revit\u00a0: {0}",
            de: "Revit-Version konnte nicht ermittelt werden: {0}",
            es: "No se pudo determinar la versión de Revit: {0}",
            it: "Impossibile determinare la versione Revit: {0}",
            pt: "Não foi possível determinar a versão do Revit: {0}",
            nl: "Kan de Revit-versie niet bepalen: {0}",
            pl: "Nie można określić wersji Revit: {0}",
            ru: "Не удалось определить версию Revit: {0}",
            ja: "Revitのバージョンを特定できませんでした: {0}",
            zh: "无法确定Revit版本：{0}"
        );
    }
}
