using System;
using System.IO;
using System.Text;

namespace M1ToolPlus.Tests
{
    /// <summary>
    /// 更新链路测试：tag 版本解析 + 下载包 SHA256 校验（v1.8.0 新增的安全门）。
    /// </summary>
    internal static class TestsGitHubUpdate
    {
        public static void Run(Action<string, Action> add)
        {
            add("ReleaseInfo: tag 解析为 Version", ParseVersionCases);
            add("SHA256: 与已知常量一致", Sha256KnownValue);
            add("Verify: digest 匹配的包通过", VerifyMatchingDigest);
            add("Verify: 被篡改的包（同尺寸）拒绝", VerifyRejectsTampered);
            add("Verify: 无 digest（老 Release）不阻断更新", VerifySkipsEmptyDigest);
            add("Verify: 非 sha256 算法不阻断更新", VerifySkipsOtherAlgorithm);
        }

        private static void ParseVersionCases()
        {
            TestAssert.Null(ReleaseInfo.ParseVersion(null), "null tag");
            TestAssert.Null(ReleaseInfo.ParseVersion(""), "空 tag");
            TestAssert.Equal(new Version(1, 8, 0), ReleaseInfo.ParseVersion("v1.8.0"), "v1.8.0");
            TestAssert.Equal(new Version(1, 8, 0), ReleaseInfo.ParseVersion("1.8.0"), "1.8.0（无 v 前缀）");
            TestAssert.Equal(new Version(1, 8, 0), ReleaseInfo.ParseVersion("  V1.8.0 "), "V 前缀 + 两侧空白");
            TestAssert.Equal(new Version(1, 8), ReleaseInfo.ParseVersion("v1.8"), "v1.8（两段）");
            TestAssert.Equal(new Version(2, 0, 1, 5), ReleaseInfo.ParseVersion("v2.0.1.5"), "v2.0.1.5（四段）");
            TestAssert.Equal(new Version(1, 8, 0, 0), ReleaseInfo.ParseVersion("v1.8.0.0.9"), "五段只取前四段");
            TestAssert.Null(ReleaseInfo.ParseVersion("abc"), "非数字 tag");
            TestAssert.Null(ReleaseInfo.ParseVersion("v1.x.0"), "含非数字段");
        }

        private static void Sha256KnownValue()
        {
            string path = TempFile(new byte[0]);
            try
            {
                // NIST 已知值：SHA256("") =
                TestAssert.Equal(
                    "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    GitHubUpdate.ComputeSha256(path),
                    "空文件 SHA256");
            }
            finally { TryDelete(path); }
        }

        private static void VerifyMatchingDigest()
        {
            byte[] payload = Encoding.UTF8.GetBytes("M1ToolPlus update package payload");
            string path = TempFile(payload);
            try
            {
                string digest = "sha256:" + GitHubUpdate.ComputeSha256(path);
                string actual;
                TestAssert.True(
                    GitHubUpdate.VerifyPackage(path, digest, out actual),
                    "digest 匹配应通过");
                TestAssert.Equal(digest.Substring(7), actual, "回填的实际哈希应为小写十六进制");
            }
            finally { TryDelete(path); }
        }

        private static void VerifyRejectsTampered()
        {
            byte[] payload = new byte[4096];
            for (int i = 0; i < payload.Length; i++) { payload[i] = (byte)(i & 0xFF); }
            string path = TempFile(payload);
            try
            {
                string good = "sha256:" + GitHubUpdate.ComputeSha256(path);

                // 只翻转一个字节、尺寸不变：大小校验拦不住，必须靠哈希。
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.Seek(1024, SeekOrigin.Begin);
                    int b = fs.ReadByte();
                    fs.Seek(1024, SeekOrigin.Begin);
                    fs.WriteByte((byte)(b ^ 0xFF));
                }
                TestAssert.Equal(4096L, new FileInfo(path).Length, "篡改后文件尺寸应不变（否则用例无意义）");

                string actual;
                TestAssert.False(
                    GitHubUpdate.VerifyPackage(path, good, out actual),
                    "同尺寸篡改包必须被拒绝");
                TestAssert.NotNull(actual, "拒绝时应给出实际哈希");
                TestAssert.True(actual != good.Substring(7), "实际哈希应与期望不同");
            }
            finally { TryDelete(path); }
        }

        private static void VerifySkipsEmptyDigest()
        {
            string path = TempFile(Encoding.UTF8.GetBytes("x"));
            try
            {
                string actual;
                TestAssert.True(GitHubUpdate.VerifyPackage(path, null, out actual), "digest 为 null 时不校验");
                TestAssert.Null(actual, "未计算哈希");
                TestAssert.True(GitHubUpdate.VerifyPackage(path, "", out actual), "digest 为空串时不校验");
            }
            finally { TryDelete(path); }
        }

        private static void VerifySkipsOtherAlgorithm()
        {
            string path = TempFile(Encoding.UTF8.GetBytes("x"));
            try
            {
                string actual;
                TestAssert.True(
                    GitHubUpdate.VerifyPackage(path, "md5:00000000000000000000000000000000", out actual),
                    "未知算法不阻断更新");
            }
            finally { TryDelete(path); }
        }

        private static string TempFile(byte[] content)
        {
            string path = Path.Combine(Path.GetTempPath(), "m1tp-hash-" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(path, content);
            return path;
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) { File.Delete(path); } }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
