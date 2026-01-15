# 🎧 CyanTooth (青牙 - 现代化蓝牙工具)

![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?logo=windows&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/UI-WPF-0078D6?logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-Apache--2.0-blue)

CyanTooth 是一款专为 Windows 10/11 设计的现代化蓝牙音频设备管理工具。它旨在提供类似 **EarTrumpet** 风格的流畅交互体验，让您能够快速连接、断开和管理您的蓝牙音频设备，并实时监控设备电量。

## 📸 预览

> [!TIP]
> 此处建议添加一张应用的主界面截图或托盘菜单截图，让用户直观感受 UI 设计。

| 托盘菜单 (参考) | 主界面 (参考) |
| :---: | :---: |
| ![Placeholder](https://via.placeholder.com/300x450?text=Tray+Menu+Screenshot) | ![Placeholder](https://via.placeholder.com/600x400?text=Main+UI+Screenshot) |

## 📦 下载

您可以从 [Releases](https://github.com/yourusername/CyanTooth/releases) 页面下载最新的安装包。
- `CyanTooth.exe`: 绿色版，直接运行。
- `CyanTooth_Setup.exe`: 安装版，包含所有运行时。

## ✨ 核心特性

- **🚀 系统托盘驻留** - 轻量级运行，最小化到托盘，随时待命。
- **🔍 智能设备发现** - 自动发现已配对的蓝牙设备（支持经典蓝牙和 BLE）。
- **⚡ 一键连接/断开** - 告别繁琐的系统设置，一键快速切换音频设备连接状态。
- **🔋 精准电量显示** - 实时显示设备电量百分比，支持多种协议。
- **🏷️ 智能设备分类** - 自动识别设备类型（耳机、音箱、键盘、鼠标等）。
- **🎨 现代化 UI** - 基于 WPF-UI 构建，完美融入 Windows 11 设计语言。

## 🛠️ 技术架构

本项目采用清晰的三层架构设计：

| 层级 | 项目名称 | 描述 |
|------|----------|------|
| **UI 层** | `BluetoothManager` | 基于 WPF 的用户界面，负责交互与展示。 |
| **核心层** | `BluetoothManager.Core` | 包含业务逻辑、数据模型、服务接口。 |
| **平台层** | `BluetoothManager.Platform` | 封装底层 Windows API、蓝牙协议和音频控制。 |

### 主要技术栈

- **框架**: .NET 8, WPF
- **UI 组件**: [WPF-UI](https://github.com/lepoco/wpfui)
- **MVVM**: CommunityToolkit.Mvvm
- **依赖注入**: Microsoft.Extensions.Hosting
- **底层 API**: Vanara.PInvoke (CoreAudio, CfgMgr32, SetupAPI)

## 💻 系统要求

- **操作系统**: Windows 10 (Build 19041+) 或 Windows 11
- **运行环境**: .NET 8.0 Runtime (Desktop)
- **硬件**: 支持蓝牙 4.0+ 的蓝牙适配器

## 🚀 快速开始

### 从源码构建

1. **克隆仓库**
   ```bash
   git clone https://github.com/yourusername/CyanTooth.git
   cd CyanTooth
   ```

2. **还原依赖**
   ```bash
   dotnet restore
   ```

3. **构建项目**
   ```bash
   dotnet build
   ```

4. **运行**
   ```bash
   cd src/BluetoothManager
   dotnet run
   ```

## 📂 项目结构

```
CyanTooth/
├── src/                          # 源代码目录
│   ├── BluetoothManager/         # 主程序 (WPF UI)
│   ├── BluetoothManager.Core/    # 核心业务逻辑
│   └── BluetoothManager.Platform/# 平台 API 封装
├── docs/                         # 项目文档
│   └── ARCHITECTURE.md           # 架构设计文档
└── tests/                        # 测试项目
```

## 🤝 致谢与参考

CyanTooth 的诞生借鉴了许多优秀开源项目的思路：

- [BluetoothDevicePairing](https://github.com/SuRHeal/BluetoothDevicePairing) - 设备发现与配对逻辑
- [ToothTray](https://github.com/p18222/ToothTray) - IKsControl 音频连接实现
- [BlueGauge](https://github.com/fsmv/BlueGauge) - BLE/BTC 电量读取逻辑
- [Alternative-A2DP-Driver](https://github.com/j some/Alternative-A2DP-Driver) - 高级音频编解码器支持参考

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建您的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启一个 Pull Request

## 📄 许可证

本项目基于 [Apache License 2.0](LICENSE) 开源。


