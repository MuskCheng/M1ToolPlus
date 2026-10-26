using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
        public string AssetDigest; // GitHub 提供的内容摘要，形如 "sha256:xxxx"（可为空）

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
        /// 计算文件 SHA256，返回小写十六进制。用于校验下载的更新包。
        /// </summary>
        public static string ComputeSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] hash = sha.ComputeHash(fs);
                StringBuilder sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) { sb.Append(hash[i].ToString("x2")); }
                return sb.ToString();
            }
        }

        /// <summary>
        /// 用 GitHub 提供的 digest（"sha256:xxxx"）校验文件。
        /// 返回 true = 校验通过（或无法校验：digest 为空 / 非 sha256 算法——
        /// GitHub 未提供摘要时不能把「无数据」判为失败，否则老版本 Release 会全部卡死）。
        /// actual 输出实际计算值（未计算时为 null），供失败提示展示。
        /// </summary>
        public static bool VerifyPackage(string path, string digest, out string actual)
        {
            actual = null;
            if (string.IsNullOrEmpty(digest)) { return true; }
            string algo = null;
            string expected = digest;
            int idx = digest.IndexOf(':');
            if (idx > 0)
            {
                algo = digest.Substring(0, idx).Trim().ToLowerInvariant();
                expected = digest.Substring(idx + 1).Trim();
            }
            if (algo != null && algo != "sha256") { return true; }  // 未知算法：不校验，不阻断更新
            if (expected.Length == 0) { return true; }
            try
            {
                actual = ComputeSha256(path);
            }
            catch (Exception)
            {
                return false;   // 读不到文件 = 包不可用，按失败处理
            }
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
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
                                // GitHub 对上传资产计算的内容摘要（2024 起的 API 字段），
                                // 客户端据此校验下载包完整性，防篡改/防截断。
                                info.AssetDigest = (string)a["digest"];
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
