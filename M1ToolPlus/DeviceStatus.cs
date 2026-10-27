namespace M1ToolPlus
{
    /// <summary>
    /// 状态栏「设备串口」文案（v1.8.2）：
    ///
    /// 前缀「设备串口：」四个字**常显**，后面的具体串口按实际情况显示。
    /// 设备没接或已拔出时只留前缀——之前这里什么都不做，读卡器拔掉后底部
    /// 还会一直挂着上一次的 COM 口，看着像设备还连着。
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
    }
}
