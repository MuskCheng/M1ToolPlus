using System;
using System.IO;
using System.Net;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace M1ToolPlus
{
    /// <summary>
    /// 最新 Release 元数据（tag + 主 .zip 资源）。
    /// tag 解析支持 v1.8.0 / 1.8.0 / v1.8 等常见形态。
    /// </summary>
    internal class ReleaseInfo
    {
        public string Tag;       // 形如 v1.8.0
        public string HtmlUrl;   // Release 页面地址
        public string AssetUrl;  // 更新包直链（browser_download_url）
        public string AssetName; // 更新包文件名
        public long AssetSize;   // 更新包字节数（缓存校验用）

        public Version Remote
        {
            get { return ParseVersion(Tag); }
        }

        public static Version ParseVersion(string tag)
        {
            if (string.IsNullOrEmpty(tag)) { return null; }
            string s = tag.Trim();
            if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase)) { s = s.Substring(1); }
            string[] parts = s.Split('.');
            int take = Math.Min(parts.Length, 4);
            string joined = "";
            for (int i = 0; i < take; i++)
            {
                int n;
                if (!int.TryParse(parts[i], out n)) { return null; }
                joined += ((i == 0) ? "" : ".") + n.ToString();
            }
            try { return new Version(joined); }
            catch (Exception) { return null; }
        }
    }

    /// <summary>
    /// 检查更新（v1.8.0 重写）：查询本仓库（Settings.GitHubR = MuskCheng/M1ToolPlus）
    /// 的 GitHub Releases API，返回最新 Release 的 tag 与其中的 .zip 更新资源。
    /// 旧的壳只指向原作者 xcicode/MifareOneTool 且无调用点，本版起彻底指向本仓库。
    /// 下载与进度 UI 见 FormUpdate（WebClient + 异步事件）。
    /// </summary>
    internal static class GitHubUpdate
    {
        /// <summary>
        /// GitHub 全线要求 TLS 1.2+；.NET 4.0 目标下枚举里没有 Tls12，
        /// 用数值 3072（即 Tls12）强转设置；旧系统无对应支持时静默保持原状。
        /// </summary>
        public static void EnableTls12()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 拉取默认仓库的最新 Release。返回 null 表示查询失败（网络/接口错误），
        /// 失败原因写入 error 输出参数。
        /// </summary>
        public static ReleaseInfo FetchLatest(out string error)
        {
            error = null;
            EnableTls12();
            string repo = Properties.Settings.Default.GitHubR;
            string url = "https://api.github.com/repos/" + repo + "/releases/latest";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.UserAgent = "M1ToolPlus/" + Assembly.GetExecutingAssembly().GetName().Version;
                req.Timeout = 20000;
                req.ReadWriteTimeout = 20000;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                {
                    string body = sr.ReadToEnd();
                    JObject jo = JObject.Parse(body);
                    JToken msg;
                    if (jo.TryGetValue("message", out msg))
                    {
                        error = (msg == null) ? "GitHub API" : msg.ToString();
                        return null;
                    }
                    ReleaseInfo info = new ReleaseInfo();
                    info.Tag = (string)jo["tag_name"];
                    info.HtmlUrl = (string)jo["html_url"];
                    JArray assets = jo["assets"] as JArray;
                    if (assets != null)
                    {
                        foreach (JToken it in assets)
                        {
                            JObject a = it as JObject;
                            if (a == null) { continue; }
                            string name = (string)a["name"];
                            if (!string.IsNullOrEmpty(name) &&
                                name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                info.AssetUrl = (string)a["browser_download_url"];
                                info.AssetName = name;
                                info.AssetSize = (long)a["size"];
                                break;
                            }
                        }
                    }
                    return info;
                }
            }
            catch (Exception ex)
            {
                WebException wex = ex as WebException;
                HttpWebResponse hr = (wex == null) ? null : (wex.Response as HttpWebResponse);
                if (hr != null && hr.StatusCode == HttpStatusCode.NotFound)
                {
                    error = "404";   // 仓库还没有任何 Release（查 /releases/latest 就是 404）
                }
                else
                {
                    error = ex.Message;
                }
                return null;
            }
        }
    }
}
