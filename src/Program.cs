using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZipSearcher.Helpers;

namespace ZipSearcher
{
    class Program
    {
        static Config _config;
        static List<string> resultList = new List<string>();

        static void Main(string[] args)
        {
            _config = ConfigHelper.LoadConfigAsync<Config>().Result;
            if (_config == null)
                _config = new Config();

            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            PrintHelpInfo();

            Console.WriteLine();
            var input = Console.ReadLine();
            args = GetArgs(input, 5);
            while (args[0].ToLower() != "quit")
            {
                switch (args[0].ToLower())
                {
                    case "useconfig":
                        _config.LastConfig = args[1];
                        SaveConfig();
                        break;
                    case "unzip":
                        UnZipDirectory();
                        break;
                    case "sc":
                    case "showconfig":
                        ShowConfig();
                        break;
                    case "d":
                    case "deleteConfig":
                        DeleteConfig(args[1]);
                        break;
                    case "c":
                    case "config":
                        SetConfig(args);
                        break;
                    case "s":
                    case "search":
                        SearchTxt(args[1]);
                        break;
                    case "sz":
                    case "searchzip":
                        SearchZip(args[1]);
                        break;
                    case "clear":
                        Console.Clear();
                        PrintHelpInfo();
                        break;
                    case "ol":
                    case "openlist":
                        OpenList(args[1]);
                        break;
                    default:

                        break;
                }
                input = Console.ReadLine();
                args = GetArgs(input, 5);
            }
        }

        private static void OpenList(string exe)
        {
            foreach (var item in resultList)
            {
                Process.Start(exe, item);
            }
        }

        private static void ShowConfig()
        {
            if (_config.Configs.Count == 0)
                return;

            OutPut($"config->LastConfig:{_config.LastConfig}");
            foreach (var item in _config.Configs)
            {
                OutPut($"config->Name:{item.Name}");
                OutPut($"config->LastDir:{item.LastDir}");
                OutPut($"config->Spattern:{item.Spattern}");
                Console.WriteLine();
            }
        }
        private static void DeleteConfig(string Name)
        {
            var exsit = _config.Configs.FirstOrDefault(m => m.Name == Name);
            if (exsit != null)
            {
                _config.Configs.Remove(exsit);
            }

            if (_config.Configs.Count > 0)
                _config.LastConfig = _config.Configs.FirstOrDefault().Name;
            else
                _config.LastConfig = string.Empty;
            SaveConfig();
            ShowConfig();
        }

        private static void SaveConfig()
        {
            ConfigHelper.SaveConfigAsync(_config).GetAwaiter().GetResult();
        }

        private static void SetConfig(string[] args)
        {
            var config = new ConfigItem();
            config.Name = args[1];
            config.LastDir = args[2];
            config.Spattern = args[3];

            var exsit = _config.Configs.FirstOrDefault(m => m.Name == config.Name);
            if (exsit != null)
            {
                exsit.LastDir = config.LastDir;
                exsit.Spattern = config.Spattern;
            }
            else
            {
                _config.Configs.Add(config);
            }

            _config.LastConfig = config.Name;
            SaveConfig();
            ShowConfig();
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
            Environment.Exit(1);
        }

        private static void PrintHelpInfo()
        {
            OutPut("使用帮助:");
            ShowConfig();
            OutPut("unzip");
            OutPut("d deleteConfig <Name>");
            OutPut("useconfig <Name>");
            OutPut("showconfig");
            OutPut("c config <Name> <Dir> <Pattern>");
            OutPut("p spattern <搜索过滤>");
            OutPut("s search <搜索内容> ");
            OutPut("sz searchzip <搜索内容> ");
            OutPut("ol openlist <notepad++|notepad> ");

        }

        private static string[] GetArgs(string input, int length = 2)
        {// search 'c://test 1123123' 123123 '444'
            var tempPara = new List<Tuple<string, Guid>>();
            var matchResult = Regex.Matches(input, @"'.*?'");
            foreach (Match item in matchResult)
            {
                Guid guid = Guid.NewGuid();
                input = input.Replace(item.Value, guid.ToString());
                tempPara.Add(new Tuple<string, Guid>(item.Value.Trim('\''), guid));
            }
            var temp = input.Split(' ');
            string[] result = new string[length];
            for (int i = 0; i < temp.Length; i++)
            {
                var tempData = tempPara.FirstOrDefault
                     (m => m.Item2.ToString() == temp[i]);
                if (tempData != null)
                    result[i] = tempData.Item1;
                else
                    result[i] = temp[i];
            }
            return result;
        }

        private static void SearchZip(string txt)
        {
            resultList = new List<string>();
            var config = GetConfig(_config.LastConfig);
            using (var progress = new ProgressBar())
            {
                int index = 0;
                var zipFiles = Directory.EnumerateFiles(config.LastDir, "*.zip", SearchOption.AllDirectories);
                var count = zipFiles.Count();
                foreach (var zipFileItem in zipFiles)
                {
                    progress.Report((double)index++ / count);
                    var destination = Decompress(zipFileItem, config.Spattern);
                    if (string.IsNullOrEmpty(destination))
                        continue;

                    var tempList = DoSearchTxt(txt, destination, $"{config.Spattern}*");
                    resultList.AddRange(tempList);

                    if (tempList.Count > 0)
                        continue;
                    try
                    {
                        Directory.Delete(destination, true);
                    }
                    catch (Exception ex)
                    {
                        OutPut($"Delete ex:{ex}");
                        continue;
                    }
                }
            }
            resultList.ForEach(m => OutPut(m));
            OutPut($"找到{resultList.Count}条记录");
            Console.WriteLine();

        }

        private static List<string> MatchResult(string txt, List<string> list)
        {
            List<string> resultList = new List<string>();
            foreach (var item in list)
            {
                using (StreamReader reader = new StreamReader(item))
                {
                    var content = reader.ReadToEnd();
                    if (content.Contains(txt))
                    {
                        resultList.Add(item);
                    }
                }
            }
            return resultList;
        }

        private static void SearchTxt(string txt)
        {
            var config = GetConfig(_config.LastConfig);
            List<string> resultList = DoSearchTxt(txt, config.LastDir, config.Spattern);
            resultList.ForEach(m => OutPut(m));
            OutPut($"找到{resultList.Count}条记录");
            Console.WriteLine();
        }
        private static List<string> DoSearchTxt(string txt, string dir, string pattern)
        {
            try
            {
                var result = Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
                List<string> resultList = MatchResult(txt, result.ToList());
                ////int index = 0;
                //List<string> resultList = new List<string>();
                ////using (var progress = new ProgressBar())
                ////{
                //foreach (var item in result)
                //{
                //    using (StreamReader reader = new StreamReader(item))
                //    {
                //        var content = reader.ReadToEnd();
                //        //progress.Report((double)index++ / result.Length);
                //        if (content.Contains(txt))
                //        {
                //            resultList.Add(item);
                //        }
                //    }
                //}
                //}

                return resultList;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private static ConfigItem GetConfig(string configName)
        {
            var exist = _config.Configs.FirstOrDefault(m => m.Name == configName);
            return exist;
        }

        private static void OutPut(string txt)
        {
            Console.WriteLine($"----{txt}");
        }

        private static void UnZipDirectory()
        {
            var config = GetConfig(_config.LastConfig);
            var zipFiles = Directory.GetFiles(config.LastDir, "*.zip", SearchOption.AllDirectories);

            int index = 0;
            using (var progress = new ProgressBar())
            {
                foreach (var file in zipFiles)
                {
                    progress.Report((double)index++ / zipFiles.Length);

                    Decompress(file);
                }
            }
        }

        public static string Decompress(string fullName)
        {
            string destination = null;
            try
            {
                destination = fullName.Remove(fullName.Length - Path.GetExtension(fullName).Length);
                if (Directory.Exists(destination))
                    Directory.Delete(destination, true);
                ZipFile.ExtractToDirectory(fullName, destination);
            }
            catch (Exception ex)
            {
            }
            return destination;
        }

        public static string Decompress(string fullName, string pattern)
        {
            string destination = null;
            try
            {
                destination = fullName.Remove(fullName.Length - Path.GetExtension(fullName).Length);
                if (Directory.Exists(destination))
                    Directory.Delete(destination, true);
                bool matched = false;
                using (ZipArchive archive = ZipFile.OpenRead(fullName))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.FullName.Contains(pattern)))
                    {
                        matched = true;
                        var target = Path.Combine(destination, entry.FullName);
                        var dir = Path.GetDirectoryName(target);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        entry.ExtractToFile(Path.Combine(destination, entry.FullName), true);
                    }
                }
                if (!matched)
                    return null;
            }
            catch (Exception ex)
            {
            }
            return destination;
        }
    }
}
