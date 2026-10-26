using System;
using System.Collections.Generic;

namespace M1ToolPlus.Tests
{
    /// <summary>
    /// 极小断言框架（替代测试框架）：每个用例是一个 Action，
    /// 失败抛 <see cref="AssertFailedException"/>，由 Program 统计并决定退出码。
    /// </summary>
    internal sealed class AssertFailedException : Exception
    {
        public AssertFailedException(string message) : base(message) { }
    }

    internal static class TestAssert
    {
        public static void True(bool condition, string what)
        {
            if (!condition) { throw new AssertFailedException(what + " —— 期望 true，实际 false"); }
        }

        public static void False(bool condition, string what)
        {
            if (condition) { throw new AssertFailedException(what + " —— 期望 false，实际 true"); }
        }

        public static void Equal(object expected, object actual, string what)
        {
            if (!Equals(expected, actual))
            {
                throw new AssertFailedException(
                    what + " —— 期望 [" + Show(expected) + "]，实际 [" + Show(actual) + "]");
            }
        }

        public static void NotNull(object value, string what)
        {
            if (value == null) { throw new AssertFailedException(what + " —— 期望非 null"); }
        }

        public static void Null(object value, string what)
        {
            if (value != null) { throw new AssertFailedException(what + " —— 期望 null，实际 [" + Show(value) + "]"); }
        }

        public static void BytesEqual(byte[] expected, byte[] actual, string what)
        {
            if (expected == null || actual == null)
            {
                if (expected == actual) { return; }
                throw new AssertFailedException(what + " —— 其中一个为 null");
            }
            if (expected.Length != actual.Length)
            {
                throw new AssertFailedException(what + " —— 长度期望 " + expected.Length + "，实际 " + actual.Length);
            }
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] != actual[i])
                {
                    throw new AssertFailedException(
                        what + " —— 第 " + i + " 字节期望 0x" + expected[i].ToString("X2") +
                        "，实际 0x" + actual[i].ToString("X2"));
                }
            }
        }

        /// <summary>断言动作抛出指定异常类型（含其子类）。</summary>
        public static void Throws<T>(Action action, string what) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;   // 符合预期
            }
            catch (Exception ex)
            {
                throw new AssertFailedException(
                    what + " —— 期望 " + typeof(T).Name + "，实际 " + ex.GetType().Name + ": " + ex.Message);
            }
            throw new AssertFailedException(what + " —— 期望抛出 " + typeof(T).Name + "，但没有异常");
        }

        private static string Show(object value)
        {
            if (value == null) { return "null"; }
            if (value is string) { return "\"" + value + "\""; }
            if (value is System.Collections.IEnumerable && !(value is string))
            {
                List<string> parts = new List<string>();
                foreach (object o in (System.Collections.IEnumerable)value) { parts.Add(Show(o)); }
                return "{" + string.Join(", ", parts.ToArray()) + "}";
            }
            return value.ToString();
        }
    }
}
