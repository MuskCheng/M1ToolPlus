using System;
using System.IO;

namespace M1ToolPlus.Tests
{
    /// <summary>
    /// S50 卡映像模型（ClassMifareS50.cs）的纯逻辑测试：
    /// UID/BCC 校验、访问控制位校验、mfd/mct 往返、hex 工具。
    /// 这些是写卡前「文件检查」与读卡后「结果展示」的算法底座，
    /// 一旦改错就会把坏数据写进卡里，所以优先覆盖。
    /// </summary>
    internal static class TestsS50
    {
        private static readonly byte[] Uid = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        public static void Run(Action<string, Action> add)
        {
            add("S50: 指定 UID 构造后 16 扇区全部合法", FreshS50IsClean);
            add("S50: UID/BCC 被篡改后 0 扇区报 BCC 错(0x01)", BccCorruptionDetected);
            add("S50: UID 长度非 4 字节抛异常", WrongUidLengthThrows);
            add("S50: mfd 导出/加载往返一致", MfdRoundTrip);
            add("S50: mfd 文件大小不符抛 IOException", MfdRejectsBadSize);
            add("S50: MCT 文本导出/加载往返一致", MctTxtRoundTrip);
            add("S50: 默认访问控制位自校验一致(ReadAC==ReadRAC)", DefaultAccessBitsConsistent);
            add("Utils: hex 字符串与字节块往返", HexRoundTrip);
        }

        private static void FreshS50IsClean()
        {
            S50 card = new S50(Uid);
            TestAssert.Equal(16, card.Sectors.Count, "扇区数");
            int[] ret = card.Verify();
            TestAssert.Equal(17, ret.Length, "Verify 返回长度（16 扇区 + 汇总）");
            TestAssert.Equal(0, ret[16], "汇总校验码（0 = 全部合法）");
            // BCC = UID 四字节异或，构造时已写入 0 块
            byte bcc = (byte)(Uid[0] ^ Uid[1] ^ Uid[2] ^ Uid[3]);
            TestAssert.Equal("deadbeef", Utils.Hex2Str(new byte[] { Uid[0], Uid[1], Uid[2], Uid[3] }), "UID 回显");
            TestAssert.Equal(bcc, card.Sectors[0].Block[0][4], "0 块 BCC 字节");
        }

        private static void BccCorruptionDetected()
        {
            S50 card = new S50(Uid);
            byte[] raw = card.SectorsRaw;
            raw[0] = (byte)(raw[0] ^ 0xFF);   // 破坏 UID 首字节 → BCC 校验和不再为 0
            card.SectorsRaw = raw;

            int[] ret = card.Verify();
            TestAssert.Equal(0x01, ret[0], "0 扇区校验码（应仅报 BCC 错）");
            TestAssert.Equal(0x01, ret[16], "汇总校验码（其余扇区仍合法）");
        }

        private static void WrongUidLengthThrows()
        {
            TestAssert.Throws<Exception>(() => new S50(new byte[3]), "S50(3 字节 UID)");
            TestAssert.Throws<Exception>(() => new S50(new byte[5]), "S50(5 字节 UID)");
            TestAssert.Throws<Exception>(() => new Sector(new byte[2]), "Sector(2 字节 UID)");
        }

        private static byte[] Pattern()
        {
            byte[] raw = new byte[1024];
            for (int i = 0; i < raw.Length; i++) { raw[i] = (byte)(i * 7 + 3); }
            return raw;
        }

        private static void MfdRoundTrip()
        {
            string path = Path.Combine(Path.GetTempPath(), "m1tp-test-" + Guid.NewGuid().ToString("N") + ".mfd");
            try
            {
                S50 src = new S50(Uid);
                src.SectorsRaw = Pattern();
                src.ExportToMfd(path);

                S50 dst = new S50();
                dst.LoadFromMfd(path);
                TestAssert.BytesEqual(src.SectorsRaw, dst.SectorsRaw, "mfd 往返字节");
            }
            finally
            {
                try { File.Delete(path); }
                catch (IOException) { }
            }
        }

        private static void MfdRejectsBadSize()
        {
            string path = Path.Combine(Path.GetTempPath(), "m1tp-test-" + Guid.NewGuid().ToString("N") + ".mfd");
            try
            {
                File.WriteAllBytes(path, new byte[1023]);
                TestAssert.Throws<IOException>(() => new S50().LoadFromMfd(path), "加载 1023 字节的 mfd");
            }
            finally
            {
                try { File.Delete(path); }
                catch (IOException) { }
            }
        }

        private static void MctTxtRoundTrip()
        {
            string path = Path.Combine(Path.GetTempPath(), "m1tp-test-" + Guid.NewGuid().ToString("N") + ".mct");
            try
            {
                S50 src = new S50(Uid);
                src.SectorsRaw = Pattern();
                src.ExportToMctTxt(path);

                S50 dst = new S50();
                dst.LoadFromMctTxt(path);
                TestAssert.BytesEqual(src.SectorsRaw, dst.SectorsRaw, "MCT 文本往返字节");
            }
            finally
            {
                try { File.Delete(path); }
                catch (IOException) { }
            }
        }

        private static void DefaultAccessBitsConsistent()
        {
            // 出厂（transport）访问控制位 FF 07 80 69：正向与反向解码必须一致，
            // 否则每个新扇区都会被判成「访问控制损坏」。ReadRAC 会就地取反入参，
            // 这里刻意各传一份副本，模拟 Sector.Verify 里的真实调用方式。
            byte[] ac = new byte[] { 0xFF, 0x07, 0x80, 0x69 };
            byte[] acP = Utils.ReadAC((byte[])ac.Clone());
            byte[] acN = Utils.ReadRAC((byte[])ac.Clone());
            TestAssert.Equal(Utils.Hex2Str(acP), Utils.Hex2Str(acN), "默认访问位的正/反向解码");

            S50 card = new S50(Uid);
            TestAssert.Equal(0, card.Verify(1), "出厂扇区 1 的校验码");
        }

        private static void HexRoundTrip()
        {
            byte[] data = new byte[] { 0x00, 0x0F, 0xA5, 0xFF, 0x10 };
            string hex = Utils.Hex2Str(data);
            TestAssert.Equal("000fa5ff10", hex, "Hex2Str 小写十六进制");
            TestAssert.BytesEqual(data, Utils.Hex2Block(hex, data.Length), "Hex2Block(Hex2Str(x))");
            TestAssert.BytesEqual(
                new byte[] { 0x01, 0xAB },
                Utils.Hex2Block("01 AB", 2),
                "Hex2Block 忽略空格并按大写解析");
        }
    }
}
