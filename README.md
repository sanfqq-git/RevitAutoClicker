# Revit Always Load Auto Clicker (Enhanced with Button State)

![sanfqq_mark](https://img.shields.io/badge/author-sanfqq-blue)

## 项目简介

**Revit Always Load Auto Clicker** 是一个针对 **Autodesk Revit** 的自动点击对话框按钮工具，旨在自动处理：

- Revit 安全性警告对话框（“Security Warning”/“Unsigned Add-In”）
- 常规确认对话框（“OK”、“确定”等）
- 特定错误对话框（如尺寸标注错误），支持备用按钮自动点击

本工具增强了按钮状态检测（灰色禁用按钮跳过），并提供备用按钮逻辑，确保 Revit 插件自动加载或对话框自动处理。

> 当前版本：v1.2 (2026-05-26)  
> 开发者：sanfqq  

---

## 功能特性

1. **自动点击安全对话框**  
   - 自动点击 `Always Load / 总是加载 / &Always Load` 按钮。
2. **自动点击确认对话框**  
   - 自动点击 `OK / 确定 / 关闭 / Continue / 继续 / Yes / 是` 按钮。
3. **按钮状态检测**  
   - 检测按钮是否可用（未灰显），避免无效点击。
4. **备用按钮处理**  
   - 对特定错误对话框（如“invalid dimension references”）：
     - 若 `OK` 按钮不可用，则自动点击 `Delete Dimension(s)` 按钮。
5. **动态进程监测**  
   - 支持新打开的 Revit 实例自动监控，无需重启工具。
6. **调试模式**  
   - 可输出调试信息，包括检测到的对话框及按钮文本。
7. **快捷退出**  
   - 控制台按 `Q` 键即可退出。

---

## 使用方法

1. 编译生成 `RevitAutoClicker.exe`。
2. 打开控制台运行：
   ```bash
   RevitAutoClicker.exe
