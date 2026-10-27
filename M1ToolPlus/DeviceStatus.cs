using System;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace M1ToolPlus
{
    /// <summary>
    /// 状态栏「设备串口」文案（v1.8.2）：
    ///
    /// 前缀「设备串口：」四个字**常显**，后面的具体串口按实际情况显示。
    /// 设备没接或已拔出时只留前缀——之前这里什么都不做，读卡器拔掉后底部
    /// 还会一直挂着上一次的 COM 口，看着像设备还连着。
    ///
    /// 另外这里也负责判断「配置里记的那个串口还在不在」：拔出后光靠
    /// nfc-scan-device 完整扫描太慢（libnfc 会先去开那个失效串口，可阻塞
    /// 数十秒，还会打印 Invalid serial port），所以插拔时先用
    /// SerialPort.GetPortNames() 做毫秒级的即时判断。
    /// </summary>
    internal static class DeviceStatus
    {
        /// <param name="connstring">libnfc 连接串，形如 pn532_uart:COM6:115200；空 = 无设备。</param>
        internal static string Format(string connstring)
        {
            if (string.IsNullOrEmpty(connstring)) { return Properties.Resources.设备串口; }
            return Properties.Resources.设备串口 +
                   connstring.Replace("pn532_uart:", "").Replace(":115200", "");
        }

        /// <summary>从连接串里取出串口名：pn532_uart:COM6:115200 → COM6；取不到返回 null。</summary>
        internal static string ExtractPort(string connstring)
        {
            if (string.IsNullOrEmpty(connstring)) { return null; }
            Match m = Regex.Match(connstring, "(?i)COM\\d+");
            if (!m.Success) { return null; }
            return m.Value.ToUpperInvariant();
        }

        /// <summary>
        /// 连接串指向的串口当前是否还在系统里。
        /// 非串口设备（acr122_pcsc / openpcsc 等）无法枚举，一律返回 true 交给完整扫描处理。
        /// </summary>
        internal static bool PortStillPresent(string connstring)
        {
            string port = ExtractPort(connstring);
            if (string.IsNullOrEmpty(port)) { return true; }
            try
            {
                foreach (string p in SerialPort.GetPortNames())
                {
                    if (string.Equals(p, port, StringComparison.OrdinalIgnoreCase)) { return true; }
                }
            }
            catch (Exception) { return true; }
            return false;
        }
    }
}
