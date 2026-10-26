using System;
using System.Diagnostics;

namespace M1ToolPlus.Tests
{
    /// <summary>
    /// CardToolFlow（v1.8.0 从 Form1 抽出的读写卡执行骨架）测试：
    /// 覆盖纯函数部分——nfc-mfclassic 参数拼装与进程启动参数。
    /// Run 骨架涉及真实子进程与 BackgroundWorker，归入 TESTPLAN 手工用例。
    /// </summary>
    internal static class TestsCardToolFlow
    {
        public static void Run(Action<string, Action> add)
        {
            add("ClassicArgs: 读卡带密钥文件", ReadWithKeyFile);
            add("ClassicArgs: 无密钥文件不附加密钥段", NoKeyFile);
            add("ClassicArgs: 用户选「不使用 Key」时不附加密钥段", NoKeyOption);
            add("ClassicArgs: 写卡 + 含空格路径保持引号包裹", WriteWithSpacedPath);
            add("BuildPsi: 无窗口且双向重定向", BuildPsiShape);
        }

        private static void ReadWithKeyFile()
        {
            TestAssert.Equal(
                "r A u \"dump.mfd\" \"key.mfd\" f",
                CardToolFlow.BuildClassicArgs("r", "A", "dump.mfd", "key.mfd", false),
                "读卡参数");
        }

        private static void NoKeyFile()
        {
            TestAssert.Equal(
                "w B u \"dump.mfd\"",
                CardToolFlow.BuildClassicArgs("w", "B", "dump.mfd", "", false),
                "密钥文件为空");
            TestAssert.Equal(
                "w B u \"dump.mfd\"",
                CardToolFlow.BuildClassicArgs("w", "B", "dump.mfd", null, false),
                "密钥文件为 null");
        }

        private static void NoKeyOption()
        {
            // 对话框选「不使用 Key（全新白卡）」时 keymfd 可能有值，但必须忽略它
            TestAssert.Equal(
                "r C u \"dump.mfd\"",
                CardToolFlow.BuildClassicArgs("r", "C", "dump.mfd", "key.mfd", true),
                "noKey=true 且存在密钥文件");
        }

        private static void WriteWithSpacedPath()
        {
            TestAssert.Equal(
                "w C u \"C:\\dumps\\my card.mfd\" \"C:\\keys\\k.mfd\" f",
                CardToolFlow.BuildClassicArgs("w", "C", "C:\\dumps\\my card.mfd", "C:\\keys\\k.mfd", false),
                "带空格的 dump 与密钥路径");
        }

        private static void BuildPsiShape()
        {
            ProcessStartInfo psi = CardToolFlow.BuildPsi("nfc-bin/nfc-mfclassic.exe", "r A u \"x.mfd\"");
            TestAssert.Equal("nfc-bin/nfc-mfclassic.exe", psi.FileName, "工具路径");
            TestAssert.Equal("r A u \"x.mfd\"", psi.Arguments, "参数原样透传");
            TestAssert.False(psi.UseShellExecute, "必须 UseShellExecute=false 才能重定向输出");
            TestAssert.True(psi.CreateNoWindow, "不弹出控制台窗口");
            TestAssert.True(psi.RedirectStandardOutput, "重定向 stdout");
            TestAssert.True(psi.RedirectStandardError, "重定向 stderr");
        }
    }
}
