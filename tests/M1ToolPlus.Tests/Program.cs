using System;
using System.Collections.Generic;

namespace M1ToolPlus.Tests
{
    /// <summary>
    /// 测试入口：注册用例 → 逐个执行 → 打印结果 → 返回退出码
    /// （0 = 全部通过，1 = 有失败）。build.ps1 -Test 与 CI 据此判成败。
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            List<KeyValuePair<string, Action>> cases = new List<KeyValuePair<string, Action>>();
            Action<string, Action> add = delegate(string name, Action body)
            {
                cases.Add(new KeyValuePair<string, Action>(name, body));
            };

            TestsS50.Run(add);
            TestsGitHubUpdate.Run(add);
            TestsCardToolFlow.Run(add);
            TestsStatusBarLayout.Run(add);

            int failed = 0;
            Console.WriteLine("M1ToolPlus 单元测试：{0} 个用例", cases.Count);
            Console.WriteLine(new string('-', 72));
            foreach (KeyValuePair<string, Action> c in cases)
            {
                try
                {
                    c.Value();
                    Console.WriteLine("[PASS] " + c.Key);
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine("[FAIL] " + c.Key);
                    Console.WriteLine("       " + ex.Message);
                }
            }
            Console.WriteLine(new string('-', 72));
            Console.WriteLine("通过 {0} / {1}，失败 {2}", cases.Count - failed, cases.Count, failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
