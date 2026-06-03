// ============================================================
// sanfqq - Revit Always Load Auto Clicker (Enhanced with button state)
// Created: 2026-05-26
// Purpose: Auto-click buttons, with fallback if preferred button disabled
// ============================================================

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace Sanfqq.RevitAutoClicker
{
    public class Program
    {
        // ----- Windows API imports -----
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWnd, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        // 新增：检查按钮是否启用（灰色禁用状态）
        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private const int BM_CLICK = 0x00F5;

        // ----- 关键词定义 -----
        private static readonly string[] DialogTitleKeywords = {
            "Security Warning", "Security - Unsigned Add-In", "Unsigned Add-In",
            "Security", "Add-In", "安全性警告"
        };

        private static readonly string[] AlwaysLoadButtonTexts = {
            "Always Load", "总是加载", "&Always Load"
        };

        private static readonly string[] GenericDialogKeywords = {
            "Warning", "Error", "Information", "提示", "警告", "错误", "信息",
            "Revit", "Autodesk Revit", "确认", "Confirm"
        };

        private static readonly string[] ConfirmButtonTexts = {
            "OK", "确定", "关闭", "Close", "Continue", "继续", "是", "Yes"
        };

        // 新增：针对特定错误对话框的备用按钮文本
        private static readonly string[] FallbackButtonTexts = {
            "Delete Dimension(s)", "删除尺寸标注"
        };

        // 触发备用按钮逻辑的对话框标题关键词
        private static readonly string[] FallbackDialogKeywords = {
            "Error - cannot be ignored", "invalid dimension references", "dimension references"
        };

        private static int clickCount = 0;
        private static bool debugMode = true;
        private static bool running = true;
        private const string SANFQQ_MARK = "sanfqq_2026_revit_auto_clicker_v1.2";

        // 动态进程监测相关
        private static HashSet<int> monitoredRevitPids = new HashSet<int>();
        private static DateTime lastProcessScanTime = DateTime.MinValue;
        private static readonly TimeSpan processScanInterval = TimeSpan.FromSeconds(2);

        public static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  sanfqq - Revit Always Load Auto Clicker");
            Console.WriteLine("  " + SANFQQ_MARK);
            Console.WriteLine("  Enhanced: Button state detection + fallback");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("Monitoring for Revit dialogs...");
            Console.WriteLine("Press 'Q' to quit, or close the console window.");
            Console.WriteLine();

            // 按键退出线程
            Thread inputThread = new Thread(() =>
            {
                while (running)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                            running = false;
                    }
                    Thread.Sleep(200);
                }
            });
            inputThread.IsBackground = true;
            inputThread.Start();

            // 主循环（无超时）
            while (running)
            {
                try
                {
                    RefreshRevitProcesses();
                    List<IntPtr> allWindows = GetAllTopLevelWindows();
                    bool clicked = false;

                    foreach (IntPtr hWnd in allWindows)
                    {
                        GetWindowThreadProcessId(hWnd, out uint windowPid);
                        int pid = (int)windowPid;

                        if (monitoredRevitPids.Contains(pid) && IsWindowVisible(hWnd))
                        {
                            if (FindAndClickDialogButton(hWnd))
                                clicked = true;
                        }
                    }

                    if (clicked && debugMode)
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Action performed (Total: {clickCount})");
                }
                catch (Exception ex)
                {
                    if (debugMode) Console.WriteLine("Debug error: " + ex.Message);
                }
                Thread.Sleep(300);
            }

            Console.WriteLine($"\nTotal clicks: {clickCount}\nPress any key to exit...");
            Console.ReadKey();
        }

        // ----- 动态刷新 Revit 进程（支持新打开的实例）-----
        private static void RefreshRevitProcesses()
        {
            if ((DateTime.Now - lastProcessScanTime) < processScanInterval)
                return;
            lastProcessScanTime = DateTime.Now;

            var currentPids = new HashSet<int>();
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.ProcessName.StartsWith("Revit", StringComparison.OrdinalIgnoreCase) &&
                        !proc.ProcessName.Contains("AlwaysLoadClicker"))
                        currentPids.Add(proc.Id);
                }
                catch { }
            }

            var newPids = new List<int>();
            foreach (int pid in currentPids)
                if (!monitoredRevitPids.Contains(pid))
                    newPids.Add(pid);

            var removedPids = new List<int>();
            foreach (int pid in monitoredRevitPids)
                if (!currentPids.Contains(pid))
                    removedPids.Add(pid);

            foreach (int pid in newPids)
            {
                Console.WriteLine($"sanfqq: New Revit process detected (PID: {pid})");
                monitoredRevitPids.Add(pid);
            }
            foreach (int pid in removedPids)
            {
                Console.WriteLine($"sanfqq: Revit process exited (PID: {pid})");
                monitoredRevitPids.Remove(pid);
            }

            if (monitoredRevitPids.Count == 0 && currentPids.Count > 0)
            {
                foreach (int pid in currentPids)
                {
                    Console.WriteLine($"sanfqq: Monitoring existing Revit process (PID: {pid})");
                    monitoredRevitPids.Add(pid);
                }
            }
        }

        private static List<IntPtr> GetAllTopLevelWindows()
        {
            var list = new List<IntPtr>();
            EnumWindows((hWnd, lParam) => { list.Add(hWnd); return true; }, IntPtr.Zero);
            return list;
        }

        // ----- 核心：查找并点击对话框按钮（支持按钮状态检测）-----
        private static bool FindAndClickDialogButton(IntPtr parentWindow)
        {
            string parentTitle = GetWindowTitle(parentWindow);

            // 判断对话框类型
            bool isSecurityDialog = false;
            foreach (string kw in DialogTitleKeywords)
                if (parentTitle.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                { isSecurityDialog = true; Console.WriteLine($"sanfqq: Security dialog: {parentTitle}"); break; }

            bool isGenericDialog = false;
            if (!isSecurityDialog)
                foreach (string kw in GenericDialogKeywords)
                    if (parentTitle.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    { isGenericDialog = true; if (debugMode) Console.WriteLine($"Debug: Generic dialog: {parentTitle}"); break; }

            bool needFallback = false;
            foreach (string kw in FallbackDialogKeywords)
                if (parentTitle.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                { needFallback = true; if (debugMode) Console.WriteLine($"Debug: Fallback dialog detected: {parentTitle}"); break; }

            if (!isSecurityDialog && !isGenericDialog && !needFallback)
                return false;

            IntPtr buttonHandle = IntPtr.Zero;

            // 1. 如果是需要备用按钮的对话框（如 dimension error）
            if (needFallback)
            {
                // 先尝试 OK 按钮（确认是否可用）
                IntPtr okButton = FindButtonByText(parentWindow, ConfirmButtonTexts);
                if (okButton != IntPtr.Zero)
                {
                    bool okEnabled = IsWindowEnabled(okButton);
                    if (debugMode) Console.WriteLine($"Debug: OK button enabled = {okEnabled}");
                    if (okEnabled)
                        buttonHandle = okButton;
                    else
                    {
                        // OK 不可用 -> 找 Delete Dimension(s) 按钮
                        buttonHandle = FindButtonByText(parentWindow, FallbackButtonTexts);
                        if (buttonHandle != IntPtr.Zero)
                            Console.WriteLine("sanfqq: OK button disabled, using Delete Dimension(s) instead");
                    }
                }
                else
                {
                    // 没有 OK 按钮，直接找备用按钮
                    buttonHandle = FindButtonByText(parentWindow, FallbackButtonTexts);
                }
            }
            else
            {
                // 普通对话框：安全对话框优先找 Always Load，否则找通用确认按钮
                if (isSecurityDialog)
                    buttonHandle = FindButtonByText(parentWindow, AlwaysLoadButtonTexts);
                if (buttonHandle == IntPtr.Zero)
                    buttonHandle = FindButtonByText(parentWindow, ConfirmButtonTexts);
            }

            if (buttonHandle != IntPtr.Zero)
            {
                string btnText = GetWindowTitle(buttonHandle);
                Console.WriteLine($"sanfqq: Clicking '{btnText}' in '{parentTitle}'");
                SendMessage(buttonHandle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                clickCount++;
                return true;
            }

            if (debugMode) FindAllButtonsInWindow(parentWindow);
            return false;
        }

        // 辅助：根据文本列表查找第一个匹配的按钮（不关心启用状态）
        private static IntPtr FindButtonByText(IntPtr hWnd, string[] targetTexts)
        {
            IntPtr found = IntPtr.Zero;
            EnumChildWindows(hWnd, (hwnd, lParam) =>
            {
                string className = GetWindowClassName(hwnd);
                if (className.IndexOf("Button", StringComparison.OrdinalIgnoreCase) < 0)
                    return true;
                string btnText = GetWindowTitle(hwnd);
                foreach (string target in targetTexts)
                {
                    if (btnText.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        found = hwnd;
                        return false; // 停止枚举
                    }
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        // 调试：列出所有按钮文本
        private static void FindAllButtonsInWindow(IntPtr hWnd)
        {
            var texts = new List<string>();
            EnumChildWindows(hWnd, (hwnd, lParam) =>
            {
                string className = GetWindowClassName(hwnd);
                if (className.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string t = GetWindowTitle(hwnd);
                    if (!string.IsNullOrEmpty(t))
                    {
                        texts.Add(t);
                        Console.WriteLine($"Debug: Button '{t}' (Class: {className})");
                    }
                }
                return true;
            }, IntPtr.Zero);
            if (texts.Count == 0) Console.WriteLine("Debug: No buttons found");
        }

        private static string GetWindowTitle(IntPtr hWnd)
        {
            int len = GetWindowTextLength(hWnd);
            if (len == 0) return "";
            var sb = new StringBuilder(len + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetWindowClassName(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
    }
}