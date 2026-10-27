using System;
using System.Drawing;
using System.Windows.Forms;

namespace M1ToolPlus.Tests
{
    /// <summary>
    /// 状态栏自适应布局测试。
    ///
    /// 背景：StatusStrip 默认的 HorizontalStackWithOverflow 一旦算不下，会把放不下的项
    /// 塞进右端一个几乎看不见的溢出菜单 —— 用户装完 v1.8.1 后「检查更新」就这样消失了
    /// （接上读卡器、设备串口文字变长，把它顶了出去）。
    ///
    /// 现在改成 Table 布局 + 中间项 Spring：两端保持自然宽度永不参与分配，
    /// 中间按可见项数量均分。这里守住这套约定，防止以后有人把布局改回去。
    /// </summary>
    internal static class TestsStatusBarLayout
    {
        public static void Run(Action<string, Action> add)
        {
            add("StatusBar: 切到 Table 布局且所有项禁止溢出", ConfiguresTableLayout);
            add("StatusBar: 两端自然宽度、中间项按数量 Spring", SpringOnlyMiddleItems);
            add("StatusBar: 中间项隐藏后重新分配，不残留 Spring", ReassignWhenMiddleHidden);
            add("StatusBar: 只有两项时两端都不 Spring", TwoItemsOnly);
            add("DeviceStatus: 无设备时只留「设备串口：」", DeviceStatusNoDevice);
            add("DeviceStatus: 有设备时前缀常显 + 串口", DeviceStatusWithDevice);
            add("DeviceStatus: 从连接串解析串口名", DeviceStatusExtractPort);
            add("DeviceStatus: 非串口设备不做存在性判断", DeviceStatusNonSerialAlwaysPresent);
        }

        /// <summary>
        /// 拔出判断依赖从连接串里取出串口名：libnfc 的连接串形如
        /// pn532_uart:COM6:115200，要拿到 COM6 才能和系统串口列表比对。
        /// </summary>
        private static void DeviceStatusExtractPort()
        {
            TestAssert.Equal("COM6", DeviceStatus.ExtractPort("pn532_uart:COM6:115200"), "COM6");
            TestAssert.Equal("COM12", DeviceStatus.ExtractPort("pn532_uart:COM12:115200"), "COM12（两位）");
            TestAssert.Equal("COM6", DeviceStatus.ExtractPort("PN532_UART:com6:115200"), "统一大写、忽略大小写");
            TestAssert.Equal(null, DeviceStatus.ExtractPort("acr122_pcsc:ACR122U"), "PCSC 设备没有串口名");
            TestAssert.Equal(null, DeviceStatus.ExtractPort(null), "null");
            TestAssert.Equal(null, DeviceStatus.ExtractPort(""), "空串");
        }

        /// <summary>
        /// 非串口设备（acr122_pcsc / openpcsc 等）无法用 SerialPort 枚举判断，
        /// 必须一律返回 true，交给完整扫描处理 —— 否则拔出时会误清空。
        /// </summary>
        private static void DeviceStatusNonSerialAlwaysPresent()
        {
            TestAssert.True(DeviceStatus.PortStillPresent("acr122_pcsc:ACR122U"), "PCSC 设备不做串口判断");
            TestAssert.True(DeviceStatus.PortStillPresent(null), "空连接串视为「还在」，由扫描决定");
            // 一个几乎不可能存在的串口号，用来验证「查得到 vs 查不到」的分支
            TestAssert.False(DeviceStatus.PortStillPresent("pn532_uart:COM250:115200"),
                "COM250 不应存在于本机（若真存在请改测试用例）");
        }

        /// <summary>设备没接 / 已拔出：前缀常显，串口部分为空。</summary>
        private static void DeviceStatusNoDevice()
        {
            TestAssert.Equal(Properties.Resources.设备串口, DeviceStatus.Format(null), "null 连接串");
            TestAssert.Equal(Properties.Resources.设备串口, DeviceStatus.Format(""), "空连接串");
        }

        /// <summary>有设备：前缀照旧显示，后面跟 COM 口（去掉驱动前缀与波特率）。</summary>
        private static void DeviceStatusWithDevice()
        {
            TestAssert.Equal(Properties.Resources.设备串口 + "COM6",
                DeviceStatus.Format("pn532_uart:COM6:115200"), "COM6");
            TestAssert.Equal(Properties.Resources.设备串口 + "COM12",
                DeviceStatus.Format("pn532_uart:COM12:115200"), "COM12（两位以上）");
        }

        /// <summary>造一个宽度与真实窗口一致（787）的状态栏，按 Items 顺序添加若干个标签。</summary>
        private static StatusStrip Build(int count, out ToolStripStatusLabel[] labels)
        {
            StatusStrip ss = new StatusStrip();
            ss.Width = 787;
            labels = new ToolStripStatusLabel[count];
            for (int i = 0; i < count; i++)
            {
                labels[i] = new ToolStripStatusLabel("项" + i);
                ss.Items.Add(labels[i]);
            }
            UiTheme.LayoutStatusBar(ss);
            return ss;
        }

        private static void ConfiguresTableLayout()
        {
            ToolStripStatusLabel[] labels;
            StatusStrip ss = Build(5, out labels);

            TestAssert.Equal(ToolStripLayoutStyle.Table, ss.LayoutStyle, "布局必须切成 Table（Spring 只在 Table 下生效）");
            foreach (ToolStripItem item in ss.Items)
            {
                TestAssert.Equal(ToolStripItemOverflow.Never, item.Overflow, item.Name + " 不允许进溢出菜单");
            }
        }

        private static void SpringOnlyMiddleItems()
        {
            ToolStripStatusLabel[] labels;
            StatusStrip ss = Build(5, out labels);

            TestAssert.False(labels[0].Spring, "最左项不 Spring（保持自然宽度，永不参与分配）");
            TestAssert.True(labels[0].AutoSize, "最左项 AutoSize");
            TestAssert.False(labels[4].Spring, "最右项不 Spring");
            TestAssert.True(labels[4].AutoSize, "最右项 AutoSize");

            for (int i = 1; i <= 3; i++)
            {
                TestAssert.True(labels[i].Spring, "第 " + (i + 1) + " 项（中间）应 Spring");
                TestAssert.Equal(ContentAlignment.MiddleLeft, labels[i].TextAlign,
                    "中间项文本左对齐（图标与文字分开定位，两者都居中会重叠）");
            }
            ss.Dispose();
        }

        private static void ReassignWhenMiddleHidden()
        {
            ToolStripStatusLabel[] labels;
            StatusStrip ss = Build(5, out labels);

            // 模拟「设备串口」这一项被隐藏：剩下的中间项数量变成 2。
            labels[2].Visible = false;
            UiTheme.LayoutStatusBar(ss);

            TestAssert.True(labels[1].Spring, "隐藏一项后，原第 2 项仍 Spring");
            TestAssert.True(labels[3].Spring, "隐藏一项后，原第 4 项接替成为中间项");
            TestAssert.False(labels[4].Spring, "最右项始终不 Spring");
            TestAssert.False(labels[2].Spring, "被隐藏的项不应带着 Spring 参与布局");
            ss.Dispose();
        }

        private static void TwoItemsOnly()
        {
            ToolStripStatusLabel[] labels;
            StatusStrip ss = Build(2, out labels);

            TestAssert.False(labels[0].Spring, "仅两项时最左不 Spring");
            TestAssert.False(labels[1].Spring, "仅两项时最右不 Spring");
            ss.Dispose();
        }
    }
}
