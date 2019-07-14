using LibRobotAuto.Common;
using System.Windows;
using System;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using System.Linq;
using System.Collections.Generic;


namespace LibRobotAuto.Module
{
    /*BEGIN---------------------------------------------------------------------- 辅助传输类 -------------------------------------------------------------------------------BEGIN*/
    public class Helper
    {
        public static AutoResetEvent endFileEvent = new AutoResetEvent(false); /* 用于线程间同步 */

        private static readonly object dtLock = new object();
        public static string remoteIpAndPort = "http://47.100.185.147:8085";
        public static bool endres = false;
        public static bool taskcancel = false;
        public const string endFileName = "end";
        public static string dtFileName = "lastdate.f";
        public static string resFileName = "";

        public static string configPath = Path.Combine(UserConfig.rootPath, "Config_I&Q");
        public static string resFailedPath = Path.Combine(configPath, "failed_res");
        public static string failedRootPath = Path.Combine(configPath, "failed_files");
        public static string fileRootPath = Path.Combine("\\data", UserConfig.SchoolCode + "\\" + UserConfig.RobotNumber);

        public static void reserveRecentNFolders(string path)
        {
            //只保留最近2次数据
            int maxFolderCap = UserConfig.maxFolderCapacity > 2 ? 2 : UserConfig.maxFolderCapacity;
            if (maxFolderCap <= 0)
            {
                maxFolderCap = 2;
            }

            DirectoryInfo di = new DirectoryInfo(path);
            if (!di.Exists)
            {
                Log.error("[Helper.reserveRecentNFolders method] Path:  " + path + "  does not exists!");
                return;
            }

            var directories = di.EnumerateDirectories().OrderByDescending(dir => dir.CreationTime);
            int i = 1;
            foreach (DirectoryInfo dir in directories)
            {
                if (!new FileInfo(dir + "\\" + endFileName).Exists) { continue; }
                if (i <= maxFolderCap) { i++; }
                else { dir.Delete(true); }
            }
        }

        public static void createFailedFiles(List<string> ffnames, string root_path)
        {
            try
            {
                if (!Directory.Exists(root_path)) { Directory.CreateDirectory(root_path); }

                Log.info("[Helper.createFailedFiles method]  Failed Directory: " + root_path + "   Files:  " + String.Join(",", ffnames.ToArray()));
                foreach (string ffname in ffnames)
                {
                    string file = root_path + "\\" + ffname;
                    if (!File.Exists(file))
                    {
                        FileStream fs = File.Create(file);
                        fs.Close();
                    }
                }
            }
            catch (Exception e) { Log.error("[Helper.createFailedFiles method]  Exception:  " + e.ToString()); }

        }

        public static void deleteFile(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    // If file found, delete it    
                    File.Delete(file);
                }
            }
            catch (Exception e)
            {
                Log.error("[Helper.deleteFile method]  Exception:  " + e.ToString());
            }
        }

        public static void emptyFolder(string folderPath)
        {

            System.IO.DirectoryInfo di = new DirectoryInfo(folderPath);

            if (di == null)
            {
                Log.error("[Helper.emptyFolder method]  Error:  " + folderPath + "does not exists");
                return;
            }

            try
            {
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
            }
            catch (Exception e)
            {
                Log.error("[Helper.emptyFolder method]  Exception:  " + e.ToString());
            }

        }

        public static DateTime opDateFile(string mode, DateTime _dt)
        {
            mode = mode.ToLower();
            DateTime dt = _dt;

            lock (dtLock)
            {
                if (mode.Equals("r"))
                    dt = DateTime.Parse(File.ReadAllText(configPath + "\\" + dtFileName).ToString());
                else if (mode.Equals("w"))
                    File.WriteAllText(configPath + "\\" + dtFileName, _dt.ToString());
            }
            return dt;
        }
    }


    //public class UserConfig
    //{
    //    public static AutoResetEvent autoResetEvent = new AutoResetEvent(false);
    //    public static string rootPath = "X:\\";
    //    public static string SchoolCode = "WHU";
    //    public static string RobotNumber = "4";
    //    public static bool NEW = false;
    //    public static int maxFolderCapacity = 3;
    //}



    /* 网络传输包 */
    public class Package : Object
    {
        public string user; /* 用户名 */
        public string pwd; /* 密码 */

        public string filePath; /* 文件相对路径 */
        public List<string> fileNames;/* 文件名列表 */
        public List<List<string>> contents;/* 文件类容列表 */
    }

    /* 日志纪录类 */
    public class Log
    {
        public static string date;
        private static StreamWriter err_w;
        private static StreamWriter info_w;

        private static object err_lock = new object();
        private static object info_lock = new object();

        private static string log_path = "\\log_I&Q\\" + "fileTransferLog\\";
        private static string info_path = "info\\";
        private static string err_path = "error\\";

        private static void log(string logMessage, TextWriter w)
        {
            w.Write("{0} {1} ", "Time: ", DateTime.Now.ToLongTimeString() + "  " +
                DateTime.Now.ToLongDateString());
            w.WriteLine("{0} {1}", "    Message: ", logMessage);
            w.Close();
        }

        public static void info(string message)
        {
            string path = UserConfig.rootPath + log_path + info_path;
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }

            lock (info_lock)
            {
                info_w = File.AppendText(path + date + ".log");
                log("[" + UserConfig.SchoolCode + " " + UserConfig.RobotNumber + "]  " + message, info_w);
            }
        }

        public static void error(string message)
        {
            string path = UserConfig.rootPath + log_path + err_path;
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }

            lock (err_lock)
            {
                err_w = File.AppendText(path + date + ".err");
                log("[" + UserConfig.SchoolCode + " " + UserConfig.RobotNumber + "]  " + message, err_w);
            }
        }
    }
    /*END---------------------------------------------------------------------- 辅助传输类 -------------------------------------------------------------------------------END*/



    /* 文件传输类 */
    public class FileTransfer
    {
        private DateTime dt;
        private Uri uri;
        private readonly HttpClient httpC;


        public FileTransfer()
        {
            httpC = new HttpClient();
            uri = new Uri(Helper.remoteIpAndPort + "/data");
            httpC.DefaultRequestHeaders.ConnectionClose = true;
            //httpC.DefaultRequestHeaders.Connection.Add("keep-alive");
            httpC.Timeout = new TimeSpan(0, 15, 0);
        }

        public FileTransfer(object dtFName)
            : this()
        {
            string dtFileName = Helper.configPath + "\\" + dtFName as string;
            if (!File.Exists(dtFileName))
            {
                dt = DateTime.Now;
                using (StreamWriter sw = File.CreateText(dtFileName))
                {
                    sw.WriteLine(dt.ToString());
                }
            }
            else { dt = Helper.opDateFile("r", DateTime.Now); }
        }

        /* FileTransfer类 文件传输运行主函数 */
        public void run()
        {
            var dataRootPath = UserConfig.rootPath + "\\data\\";
            while (true)
            {
                UserConfig.autoResetEvent.WaitOne();
                try
                {
                    /* 寻找路径下需要传输的文件 */
                    var dirs = Directory.GetDirectories(dataRootPath).ToList();
                    foreach (string dir in dirs)
                    {
                        var subDir = dir.Remove(0, dataRootPath.Length);
                        Package pkg = getFileContent(dir, subDir);

                        if (pkg != null)
                        {
                            transferAsync(pkg, subDir);
                        }
                        // <<<
                        //  2019-06-12 去掉循环，因为有可能在某些条件下发生死循环，肯能会造成内存溢出。
                        // >>>
                        //while (pkg != null)
                        //{
                        //    transferAsync(pkg, subDir);
                        //    pkg = getFileContent(dir, subDir);
                        //}
                    }
                }
                catch (Exception e)
                {
                    Log.error("[FileTransfer.run method] Exception:  " + e.ToString());
                }
            }
        }

        public void reTransfer(string __subDir, string r_path)
        {
            string failed_path = Path.Combine(Helper.failedRootPath, __subDir);
            if (!Directory.Exists(failed_path))
            {
                Log.error("[FileTransfer.reTransfer method] Path not exists: " + failed_path);
                return;
            }

            /* 创建传输失败的文件的传输包 */
            Package pkg = new Package();
            pkg.filePath = Path.Combine(Helper.fileRootPath, __subDir) + "\\";
            pkg.fileNames = new List<string>();
            pkg.contents = new List<List<string>>();


            try
            {

                List<string> failedfiles = Directory.GetFiles(failed_path).Select(Path.GetFileName).OrderBy(f => new FileInfo(failed_path + "\\" + f).LastWriteTime).Take(4).ToList();
                if (failedfiles == null || failedfiles.Count == 0)
                {
                    // <<<
                    //  2019-06-12 去掉该条日志，因为有可能会使日志成倍增长。
                    // >>>
                    //Log.error("[FileTransfer.reTransfer method] Error:  no failedFiles in failedPath " + failed_path);
                    return;
                }

                for (int i = 0; i < failedfiles.Count; i++)
                {

                    string v_fileName = r_path + failedfiles[i];
                    if (!File.Exists(v_fileName)) //TODO
                    {
                        if (File.Exists(failed_path + "\\" + failedfiles[i]))
                            File.Delete(failed_path + "\\" + failedfiles[i]);
                        Log.error("[FileTransfer.reTransfer method] File not exists: " + v_fileName);
                    }
                    else
                    {
                        pkg.fileNames.Add(failedfiles[i]);
                        var lines = File.ReadLines(v_fileName).ToList();
                        pkg.contents.Add(lines);
                        lines = null;
                    }
                }

                failedfiles = null;
            }
            catch (UnauthorizedAccessException uae)
            {
                Log.error("[FileTransfer.reTransfer method] UnauthorizedAccessException:  " + uae.ToString());
                return;
            }



            //pkg.fileNames = Directory.GetFiles(failed_path).Select(Path.GetFileName).OrderBy(f => new FileInfo(failed_path + "\\" + f).LastWriteTime).Take(8).ToList();
            //pkg.contents = new List<List<string>>(pkg.fileNames.Count);

            //for (int i = 0; i < pkg.fileNames.Count; i++)
            //{
            //    string v_fileName = r_path + pkg.fileNames[i];
            //    if (!File.Exists(v_fileName)) //TODO
            //    {

            //        if (File.Exists(failed_path + "\\" + pkg.fileNames[i]))
            //            File.Delete(failed_path + "\\" + pkg.fileNames[i]);
            //        pkg.fileNames.RemoveAt(i);
            //        i--;
            //        Log.error("[FileTransfer.reTransfer method] File does not exists:  " + v_fileName);
            //        continue;
            //    }
            //    pkg.contents.Insert(i, new List<string>(File.ReadAllLines(v_fileName)));
            //}     



            int retry = 2;

            while ((Directory.Exists(failed_path) && Directory.GetFiles(failed_path).Length > 0) && retry > 0)
            {
                HttpContent cont = new StringContent(JsonConvert.SerializeObject(pkg));
                try
                {
                    /* 向服务器发起请求传送本次传输的文件 */
                    var response = httpC.PostAsync(uri, cont).Result;
                    var res = response.Content.ReadAsStringAsync().Result;
                    if (res != null && res == "OK")
                    {
                        if (pkg.fileNames.Contains(Helper.endFileName))
                        {
                            Helper.resFileName = __subDir + ".res";
                            Helper.endFileEvent.Set();
                            Directory.Delete(failed_path, true);
                        }
                        else
                        {
                            foreach (string fn in pkg.fileNames)
                            {
                                File.Delete(failed_path + "\\" + fn);
                            }
                        }


                        Log.info("[FileTransfer.reTransfer method] Success:  path: " + pkg.filePath + "  files: " + String.Join(",", pkg.fileNames));
                        return;
                    }
                }
                catch (Exception e)
                {
                    retry--;
                }

                cont = null;
            }

            pkg = null;

        }

        /* 搜索dataPath路径下最新生成的文件的文件名 */
        private Package getFileContent(string dataPath, string _subDir)
        {


            /* 按文件最后修改时间降序 */
            var look = (from f in Directory.EnumerateFiles(dataPath)
                        let ff = new FileInfo(f)
                        where ff.CreationTime > dt.AddSeconds(1)
                        orderby ff.CreationTime ascending
                        select ff.Name
                        ).Take(2);
            /* 得到最新文件名 */
            List<string> names = look.ToList();
            if (names.Count < 1) { return null; }
            Log.info("[FileTransfer.getFileContent method] path: " + dataPath + " newfileNames: " + String.Join(",", names.ToArray()));

            /* 创建网络传输包 然后向其中填充文件内容*/
            Package pkg = new Package();
            pkg.filePath = Path.Combine(Helper.fileRootPath, _subDir) + "\\";
            pkg.fileNames = new List<string>();
            pkg.contents = new List<List<string>>();

            //Package pkg = new Package
            //{
            //    filePath = Path.Combine(Helper.fileRootPath, _subDir) + "\\",
            //    fileNames = names,
            //    contents = new List<List<string>>()
            //};

            try
            {
                for (int i = 0; i < names.Count; i++)
                {
                    string v_fileName = dataPath + "\\" + names[i];
                    if (!File.Exists(v_fileName)) //TODO
                    {
                        Log.error("[FileTransfer.getFileContent method] File does not exists:  " + v_fileName);
                        continue;
                    }
                    pkg.fileNames.Add(names[i]);
                    var lines = File.ReadLines(v_fileName).ToList();
                    pkg.contents.Add(lines);
                    lines = null;
                }

                //for (int i = 0; i < pkg.fileNames.Count; i++)
                //{
                //    string v_fileName = dataPath + "\\" + pkg.fileNames[i];
                //    if (!File.Exists(v_fileName)) //TODO
                //    {
                //        pkg.fileNames.RemoveAt(i);
                //        i--;
                //        Log.error("[FileTransfer.getFileContent method] File does not exists:  " + v_fileName);
                //        continue;
                //    }
                //    pkg.contents.Insert(i, new List<string>(File.ReadAllLines(v_fileName)));
                //}

                dt = new FileInfo(dataPath + "\\" + pkg.fileNames[pkg.fileNames.Count - 1]).LastWriteTime;
                Helper.opDateFile("w", dt);

                //<2019-06-13> 删除临时日志
                //Log.info("[getFileContent ] dataTime: " + dt.ToString());
            }
            catch (Exception e)
            {
                Log.error("[FileTransfer.getFileContent method] Exception: " + e.ToString());
                /* 记录读取失败的文件 */
                Helper.createFailedFiles(pkg.fileNames, Path.Combine(Helper.failedRootPath, _subDir));
                return null;
            }

            return pkg;
        }

        private async void transferAsync(Package pk, string _subPath)
        {
            if (pk == null) { return; }

            try
            {
                HttpContent cont = new StringContent(JsonConvert.SerializeObject(pk));
                /* end文件不在本次传输，在重传中传输 */
                //string fPath = Path.Combine(Helper.failedRootPath, _subPath);
                if (pk.fileNames.Contains(Helper.endFileName) /*&& Directory.Exists(fPath) && Directory.GetDirectories(fPath).Count() > 0*/)
                {
                    //Helper.createFailedFiles(new List<string>() {Helper.endFileName }, Path.Combine(Helper.failedRootPath, _subPath));
                    int index = pk.fileNames.IndexOf(Helper.endFileName);
                    Package tmp_pkg = new Package
                    {
                        fileNames = new List<string>(pk.fileNames.ToArray()),
                        filePath = pk.filePath,
                        contents = new List<List<string>>(pk.contents.ToArray())
                    };
                    tmp_pkg.fileNames.RemoveAt(index);
                    tmp_pkg.contents.RemoveAt(index);
                    cont = new StringContent(JsonConvert.SerializeObject(tmp_pkg));
                }


                /* 向服务器发起请求传送本次传输的文件 */
                using (var response = await httpC.PostAsync(uri, cont))
                using (var content = response.Content)
                {
                    string res = await content.ReadAsStringAsync();
                    if (res != null && res == "OK")
                    {
                        /* end文件不在本次传输，在重传中传输 */
                        if (pk.fileNames.Contains(Helper.endFileName) /*&& Directory.Exists(fPath) && Directory.GetDirectories(fPath).Count() > 0*/)
                        {
                            Helper.createFailedFiles(new List<string>() { Helper.endFileName }, Path.Combine(Helper.failedRootPath, _subPath));
                        }
                        List<string> fns = new List<string>(pk.fileNames);
                        fns.Remove(Helper.endFileName);
                        if (fns.Count > 0) Log.info("[FileTransfer.transferAsync method] File send  successfully:   " + String.Join(",", fns.ToArray()));
                    }
                    else
                    {
                        Helper.createFailedFiles(pk.fileNames, Path.Combine(Helper.failedRootPath, _subPath));
                        Log.error("[FileTransfer.transferAsync method] File send  failed！");
                    }
                }
            }
            catch (HttpRequestException e)
            {
                /* 记录传输失败的文件 */
                Helper.createFailedFiles(pk.fileNames, Path.Combine(Helper.failedRootPath, _subPath));
                Log.error("[FileTransfer.transferAsync method] Failed:  " + _subPath + ":  " + String.Join(",", pk.fileNames));
                Log.error("[FileTransfer.transferAsync method] HttpRequestException:  " + e.ToString());
            }
            catch (Exception e)
            {
                /* 记录传输失败的文件 */
                Helper.createFailedFiles(pk.fileNames, Path.Combine(Helper.failedRootPath, _subPath));
                Log.error("[FileTransfer.transferAsync method] Failed:  " + _subPath + ":  " + String.Join(",", pk.fileNames));
                Log.error("[FileTransfer.transferAsync method] Exception:  " + e.ToString());
            }
        }
    }


    public class FilesReTransAll
    {
        public static void reTransAll()
        {
            FileTransfer ft = new FileTransfer();
            while (true)
            {
                try
                {
                    if (!Directory.Exists(Helper.failedRootPath))
                    {
                        //
                        //<2019-06-13> 删除冗余日志
                        //
                        //Log.info("[FilesReTransAll.reTransAll method] Log: failedRootPath not exists " + Helper.failedRootPath);                      
                        continue;
                    }

                    /* 若没有失败文件则过1min后检查一下目录 */
                    string[] dirs = Directory.GetDirectories(Helper.failedRootPath);
                    if (dirs.Count() < 1) { Thread.Sleep(60000); continue; }


                    List<string> dirList = dirs.Select(s => s.Remove(0, Helper.failedRootPath.Length + 1)).ToList();
                    if (dirList == null || dirList.Count == 0)
                    {
                        continue;
                    }

                    foreach (string p in dirList)
                    {
                        string abs_p = UserConfig.rootPath + "\\data\\" + p + "\\";
                        ft.reTransfer(p, abs_p);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.error("[FilesReTransAll.reTransAll method] UnauthorizedAccessException:  " + ex.ToString());
                    continue;
                }
                catch (Exception e)
                {
                    Log.error("[FilesReTransAll.reTransAll method]  Exception:  " + e.ToString());
                    continue;
                }

            }
        }
    }


    /* 从云上获取.res文件*/
    public class FilesRequest
    {
        private readonly HttpClient httpclient;
        private Uri uri;

        public FilesRequest()
        {
            httpclient = new HttpClient();
            uri = new Uri(Helper.remoteIpAndPort + "/result");
            //httpclient.DefaultRequestHeaders.Connection.Add("keep-alive");
            httpclient.Timeout = new TimeSpan(0, 35, 0);
        }
        public void getRemoteFileAsync(Package pg)
        {
            if (pg == null)
            {
                Log.error("[FilesRequest.getRemoteFileAsync method] Memory err!");
                return;
            }

            try
            {
                /* 这次盘点的.res文件 */
                if (Helper.resFileName.Equals(""))
                    Log.error("[FilesRequest.getRemoteFileAsync method] new .res file'name is empty ");
                else if (!pg.fileNames.Contains(Helper.resFileName))
                    pg.fileNames.Add(Helper.resFileName);

                /* 增加上次获取失败的.res文件 */
                //if (Directory.Exists(Helper.resFailedPath))
                //{
                //    var _fileNames = new List<string>(Directory.EnumerateFiles(Helper.resFailedPath).Select(s => new FileInfo(s).Name).ToArray());
                //    var tmp = pg.fileNames;
                //    pg.fileNames = _fileNames.Union(tmp).ToList();
                //}


                StringContent req = new StringContent(JsonConvert.SerializeObject(pg));
                var response = httpclient.PostAsync(uri, req).Result;
                string res = response.Content.ReadAsStringAsync().Result;
                if (res != null)
                {
                    /* 记录获取失败的.res文件 */
                    Package jsonPkg = JsonConvert.DeserializeObject<Package>(res);
                    var failedRes = pg.fileNames.Except(jsonPkg.fileNames).ToList();
                    int count = jsonPkg.fileNames == null ? 0 : jsonPkg.fileNames.Count;

                    if (count > 0 && failedRes.Count > 0) Helper.createFailedFiles(failedRes, Helper.resFailedPath);

                    string p = UserConfig.rootPath + "\\result\\";
                    if (!Directory.Exists(p)) { Directory.CreateDirectory(p); }

                    /* 将从远程服务器请求到的文件写入本地路径 p 下 */

                    for (int i = 0; i < count; i++)
                    {
                        List<string> contents = jsonPkg.contents.ElementAt(i);
                        string fileName = jsonPkg.fileNames.ElementAt(i);
                        string file = p + fileName;
                        File.WriteAllLines(file, contents);
                        Log.info("[FileRequest.getRemoteFileAsync method]  writing file: " + file + " successfully!");
                    }
                    /* 将名为end的文件最后写入 */
                    if (count > 0)
                    {
                        string[] empty = { };
                        File.WriteAllLines(p + Helper.endFileName, empty);
                        Log.info("[FileRequest.getRemoteFileAsync method] writing file: " + Helper.endFileName + " successfully!");
                        foreach (string fn in jsonPkg.fileNames)
                        {
                            var f = Helper.resFailedPath + "\\" + fn;
                            if (File.Exists(f)) File.Delete(f);
                        }

                        Helper.endres = true;
                    }
                }
                else
                {
                    if (!Helper.resFileName.Equals(""))
                    {
                        Helper.createFailedFiles(new List<string>() { Helper.resFileName }, Helper.resFailedPath);
                        Log.error("[FileRequest.getRemoteFileAsync method] Request file failed!   ");
                    }
                    else
                        Log.error("[FileRequest.getRemoteFileAsync method] Request file failed!  &&  Helper.resFileName is empty ");
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException tce)
            {
                if (!Helper.resFileName.Equals("")) Helper.createFailedFiles(new List<string>() { Helper.resFileName }, Helper.resFailedPath);
                Log.error("[FileRequest.getRemoteFileAsync method] CancelException: " + tce.ToString());
                Helper.taskcancel = true;
            }
            catch (AggregateException ae)
            {
                if (!Helper.resFileName.Equals("")) Helper.createFailedFiles(new List<string>() { Helper.resFileName }, Helper.resFailedPath);
                Log.error("[FileRequest.getRemoteFileAsync method] AggException: " + ae.ToString());
                Helper.taskcancel = true;
            }
            catch (Exception e)
            {
                if (!Helper.resFileName.Equals("")) Helper.createFailedFiles(new List<string>() { Helper.resFileName }, Helper.resFailedPath);
                Log.error("[FileRequest.getRemoteFileAsync method] Exception: " + e.ToString());
                Log.info("[FileRequest.getRemoteFileAsync method] Exception:  Another Exception");
                Helper.taskcancel = true;
            }
        }
    }

    public class RequestResult
    {
        public static void requestRes(object fpath)
        {
            string path = fpath as string; /* 相对路径 */

            while (true)
            {
                Helper.endFileEvent.WaitOne(); /* 等待另一个线程传输完相应路径下所有文件后再启动 */
                Package pkg = new Package { filePath = path, fileNames = new List<string>() };

                int chance = 1;
                DateTime start = DateTime.Now;
                while (true)
                {

                    if (DateTime.Now.Subtract(start).Hours > 4)
                    {
                        Log.error("[FilesRequest.run method] Timeout:  now break!");
                        string subject = "获取.res超时";
                        String body = "由于网络信号较差，传输.res文件失败！";
                        //trySendEmail(subject, body, false);
                        return;
                    }

                    if (chance < 4)
                    {
                        Log.info("[FilesRequest.run method] request restart! chance=" + chance);
                        Log.info("[FilesRequest.run method] Helper.endres=" + Helper.endres + " Helper.taskcancel=" + Helper.taskcancel);
                        chance++;
                    }

                    FilesRequest fr = new FilesRequest();
                    Thread.Sleep(1 * 60 * 1000);
                    while (!Helper.endres && !Helper.taskcancel)
                    {
                        fr.getRemoteFileAsync(pkg); /* 获取远程文件 */
                        Thread.Sleep(5 * 60 * 1000); /* 若没有获取到文件 则每隔5min请求一次 */
                    }

                    if (Helper.taskcancel)
                    {
                        Helper.taskcancel = false;
                    }

                    if (Helper.endres)/* 获取到远程文件后结束 */
                    {
                        Helper.endres = false;
                        //MessageBox.Show("已获取到远程文件!");
                        Log.info("[FilesRequest.run method] Get '.res' file now method break!");

                        //<2019-06-13>原来是return现在需要改为break,可以连续盘点获取.res文件
                        //return;
                        break;
                    }
                }
            }
        }
    }


    class FileTransportModule
    {
        private static bool one_time = true;

        public static void Run()
        {
            string resultPath = "\\result\\" + UserConfig.SchoolCode + "\\" + UserConfig.RobotNumber + "\\";
            Log.date = DateTime.Now.ToLocalTime().ToString().Replace(':', '-').Replace('/', '-').Replace(' ', '_');

            /* 保留最新N个文件夹 */
            string r_path = UserConfig.rootPath + "\\data\\";
            Helper.reserveRecentNFolders(r_path);

            /* 清空 上一次失败的.dat文件 */
            Helper.emptyFolder(Helper.failedRootPath);
            /* 清空 上一次失败的.res文件 */
            Helper.emptyFolder(Helper.resFailedPath);
            /* 删除lastdata.f文件 */
            Helper.deleteFile(Helper.configPath + "\\" + Helper.dtFileName);

            /* 各个线程只运行一次 */
            if (!one_time) { return; }
            Log.info("[FileTransportModule.Run] Starting a new inventory now");
            try
            {
                one_time = false;

                Thread ft = new Thread(() => new FileTransfer(Helper.dtFileName).run());
                ft.Start();

                Thread fr = new Thread(() => RequestResult.requestRes(resultPath));
                fr.Start();

                Thread frt = new Thread(() => FilesReTransAll.reTransAll());
                frt.Start();

            }
            catch (Exception e)
            {
                Log.error("[FileTransportModule.Run] Exception  " + e.ToString());
            }
        }

        //static void Main(string[] args)
        //{
        //    string date = DateTime.Now.ToShortDateString().ToString().Replace('/', '-');
        //    string time = DateTime.Now.ToLongTimeString().ToString().Replace(':', '-');


        //    Thread tt = new Thread(() => FileTransportModule.Run());
        //    tt.Start();


        //}
    }

}
