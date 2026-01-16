# 蓝牙管理工具技术架构与实现路径文档

> **项目目标**: 开发一款 Windows 10/11 蓝牙音频设备管理工具  
> **技术栈**: C# (.NET 8) + WPF  
> **UI/UX 目标**: 复刻 EarTrumpet 风格的现代化托盘交互体验

---

## 目录

1. [项目概述](#1-项目概述)
2. [参考项目深度分析](#2-参考项目深度分析)
   - 2.1 BluetoothDevicePairing (C#)
   - 2.2 ToothTray (C++)
   - 2.3 BlueGauge (Rust)
   - 2.4 Alternative-A2DP-Driver
3. [技术架构设计](#3-技术架构设计)
4. [核心模块实现路径](#4-核心模块实现路径)
5. [UI/UX 设计规范](#5-uiux-设计规范)
6. [开发路线图](#6-开发路线图)
7. [附录：关键代码示例](#7-附录关键代码示例)

---

## 1. 项目概述

### 1.1 核心功能需求

| 功能 | 优先级 | 描述 |
|------|--------|------|
| 系统托盘驻留 | P0 | 最小化到托盘，开机自启动 |
| 设备发现与枚举 | P0 | 发现已配对的经典蓝牙和 BLE 设备 |
| 一键连接/断开 | P0 | 快速切换蓝牙音频设备连接状态 |
| 电量显示 | P0 | 精准显示设备电量百分比 |
| 现代化 UI | P1 | EarTrumpet 风格的悬浮面板 |
| 设备别名 | P2 | 自定义设备显示名称 |
| 低电量通知 | P2 | 可配置阈值的电量预警 |

### 1.2 技术栈选型

```
┌─────────────────────────────────────────────────────────┐
│                    应用层 (WPF)                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │
│  │  托盘图标   │  │  悬浮面板   │  │   设置窗口      │  │
│  └─────────────┘  └─────────────┘  └─────────────────┘  │
├─────────────────────────────────────────────────────────┤
│                   服务层 (C#)                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │
│  │ 蓝牙服务    │  │  音频服务   │  │   配置服务      │  │
│  └─────────────┘  └─────────────┘  └─────────────────┘  │
├─────────────────────────────────────────────────────────┤
│                   平台层 (Windows API)                   │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │
│  │ WinRT API   │  │ CoreAudio   │  │   PnP/CfgMgr    │  │
│  │ (蓝牙枚举)  │  │ (IKsControl)│  │   (电量读取)    │  │
│  └─────────────┘  └─────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### 1.3 参考项目概览

| 项目 | 语言 | 核心价值 | 可复用程度 |
|------|------|----------|------------|
| BluetoothDevicePairing | C# | 设备发现、配对、音频断开逻辑 | ⭐⭐⭐⭐⭐ 直接复用 |
| ToothTray | C++ | IKsControl 连接/断开实现 | ⭐⭐⭐⭐ 需移植到 C# |
| BlueGauge | Rust | BLE/BTC 电量获取逻辑 | ⭐⭐⭐⭐ 需移植到 C# |
| Alternative-A2DP-Driver | - | 高清编解码器支持 | ⭐⭐ 可选高级功能 |

---

## 2. 参考项目深度分析

### 2.1 BluetoothDevicePairing (C#) - 核心参考

> **路径**: `code/BluetoothDevicePairing`  
> **框架**: .NET Framework 4.7.2 (需升级到 .NET 8)  
> **价值**: ⭐⭐⭐⭐⭐ 可直接复用大部分代码

#### 2.1.1 项目架构

```
src/
├── Bluetooth/
│   ├── Adapters/          # 蓝牙适配器发现
│   ├── Devices/           # 设备抽象与操作
│   │   ├── BluetoothDevice.cs      # 经典蓝牙设备
│   │   ├── BluetoothLeDevice.cs    # BLE 设备
│   │   ├── DeviceDiscoverer.cs     # 设备发现器
│   │   └── DevicePairer.cs         # 配对逻辑
│   └── AudioDevices/      # 音频设备控制
│       ├── AudioDevice.cs          # IKsControl 封装
│       └── AudioDeviceEnumerator.cs
├── Commands/              # CLI 命令实现
│   ├── PairDeviceByMac.cs
│   ├── DisconnectBluetoothAudioDeviceByMac.cs
│   └── Utils/
│       ├── DeviceFinder.cs         # 设备查找工具
│       └── AudioDeviceDisconnector.cs
└── Utils/
```

#### 2.1.2 核心实现分析

**设备发现 (DeviceFinder.cs)**
```csharp
// 按 MAC 地址查找 - 直接创建设备对象，无需完整扫描
public static Device FindDevicesByMac(DeviceMacAddress mac, DeviceType deviceType)
{
    return deviceType == DeviceType.Bluetooth 
        ? BluetoothDevice.FromMac(mac)      // 使用 FromBluetoothAddressAsync
        : BluetoothLeDevice.FromMac(mac);   // 使用 FromBluetoothAddressAsync
}

// 按名称查找 - 需要先扫描设备列表
public static Device FindDevicesByName(List<Device> devices, string name)
{
    return devices.FirstOrDefault(d => d.Name.Contains(name));
}
```

**音频设备断开 (AudioDeviceDisconnector.cs)**
```csharp
public static void DisconnectBluetoothAudioDevice(Device device)
{
    // 1. 检查设备连接状态
    if (device.ConnectionStatus != BluetoothConnectionStatus.Connected)
        return;
    
    // 2. 遍历关联的音频端点
    foreach (var audioDevice in device.AssociatedAudioDevices)
    {
        audioDevice.Disconnect();  // 调用 IKsControl
    }
}
```

**IKsControl 断开实现 (AudioDevice.cs)**
```csharp
// GUID: 7FA06C40-B8F6-4C7E-8556-E8C33A12E54D
private static readonly Guid KSPROPSETID_BtAudio = new(...);

private void GetKsProperty(KSPROPERTY_BTAUDIO btAudioProperty)
{
    // KSPROPERTY_ONESHOT_DISCONNECT = 1
    // KSPROPERTY_ONESHOT_RECONNECT = 0
    var ksProperty = new KsProperty(
        KSPROPSETID_BtAudio, 
        btAudioProperty, 
        KsPropertyKind.KSPROPERTY_TYPE_GET);
    
    ksControl.KsProperty(ksProperty, ...);
}
```

#### 2.1.3 关键依赖

| NuGet 包 | 用途 |
|----------|------|
| `Vanara.PInvoke.CoreAudio` | IMMDevice, IDeviceTopology 等 COM 接口 |
| `Microsoft.Windows.SDK.Contracts` | WinRT API 投影 (Windows.Devices.Bluetooth) |
| `CommandLineParser` | CLI 参数解析 (可移除) |

#### 2.1.4 可复用模块

| 模块 | 复用方式 | 修改点 |
|------|----------|--------|
| `DeviceFinder.cs` | 直接复用 | 改为异步方法 |
| `AudioDevice.cs` | 直接复用 | 无需修改 |
| `DeviceDiscoverer.cs` | 直接复用 | 添加事件通知 |
| `DevicePairer.cs` | 部分复用 | 本项目不需配对功能 |

---

### 2.2 ToothTray (C++) - 连接控制参考

> **路径**: `code/ToothTray`  
> **框架**: Win32 + WinRT (C++/WinRT)  
> **价值**: ⭐⭐⭐⭐ IKsControl 连接/断开的完整实现

#### 2.2.1 核心架构

```
ToothTray/
├── ToothTray.cpp              # 入口，消息循环
├── TrayIcon.cpp/h             # 系统托盘图标
├── ToothTrayMenu.cpp/h        # 动态右键菜单
├── BluetoothAudioDevices.cpp/h    # 核心：音频设备枚举与控制
├── BluetoothConnector.cpp/h   # IKsControl 封装
├── BluetoothDeviceWatcher.cpp/h   # 设备状态监控
├── DeviceContainerEnumerator.cpp/h # 设备容器名称解析
└── BluetoothRadio.cpp/h       # 蓝牙适配器状态
```

#### 2.2.2 关键实现：蓝牙音频设备枚举

ToothTray 使用 **CoreAudio + DeviceTopology** 枚举蓝牙音频设备：

```cpp
// BluetoothAudioDevices.cpp - 枚举逻辑
void EnumerateBluetoothAudioDevices()
{
    // 1. 获取所有音频渲染端点
    IMMDeviceEnumerator* pEnumerator;
    CoCreateInstance(CLSID_MMDeviceEnumerator, ...);
    pEnumerator->EnumAudioEndpoints(eRender, DEVICE_STATE_ACTIVE, &pCollection);
    
    // 2. 遍历每个端点
    for (each device in pCollection)
    {
        // 3. 获取 ContainerId (关联物理设备)
        IPropertyStore* pProps;
        device->OpenPropertyStore(STGM_READ, &pProps);
        pProps->GetValue(PKEY_Device_ContainerId, &containerId);
        
        // 4. 检查是否为蓝牙设备
        IDeviceTopology* pTopology;
        device->Activate(IID_IDeviceTopology, ...);
        // 检查设备 ID 是否以 "{2}.\\?\bth" 开头
        if (IsBluetoothDevice(connectorId))
        {
            // 5. 获取 IKsControl 接口用于连接控制
            IKsControl* pKsControl;
            pConnector->Activate(IID_IKsControl, ...);
        }
    }
}
```

#### 2.2.3 关键实现：一键连接/断开

```cpp
// BluetoothConnector.cpp
// GUID: 602DCEAC-D13D-4DDA-807D-37456ABC210E
static const GUID KSPROPSETID_BtAudio = {...};

enum KSPROPERTY_BTAUDIO {
    KSPROPERTY_ONESHOT_RECONNECT = 0,   // 连接
    KSPROPERTY_ONESHOT_DISCONNECT = 1   // 断开
};

HRESULT BluetoothConnector::Connect()
{
    return GetKsBtAudioProperty(KSPROPERTY_ONESHOT_RECONNECT);
}

HRESULT BluetoothConnector::Disconnect()
{
    return GetKsBtAudioProperty(KSPROPERTY_ONESHOT_DISCONNECT);
}

HRESULT BluetoothConnector::GetKsBtAudioProperty(KSPROPERTY_BTAUDIO property)
{
    KSPROPERTY ksProperty;
    ksProperty.Set = KSPROPSETID_BtAudio;
    ksProperty.Id = property;
    ksProperty.Flags = KSPROPERTY_TYPE_GET;
    
    DWORD bytesReturned;
    return ksControl->KsProperty(&ksProperty, sizeof(ksProperty), 
                                  NULL, 0, &bytesReturned);
}
```

#### 2.2.4 托盘菜单动态构建

```cpp
// ToothTrayMenu.cpp - 每次点击重新构建菜单
void ToothTrayMenu::BuildMenu()
{
    // 清空旧菜单
    while (DeleteMenu(hMenu, 0, MF_BYPOSITION));
    
    // 重新枚举设备
    auto devices = EnumerateBluetoothAudioDevices();
    
    for (auto& device : devices)
    {
        MENUITEMINFOW mii = {};
        mii.fMask = MIIM_STRING | MIIM_ID | MIIM_STATE;
        mii.wID = GetMenuItemId(device);
        mii.dwTypeData = device.friendlyName;
        
        // 已连接设备显示勾选
        if (device.isConnected)
            mii.fState = MFS_CHECKED;
        
        InsertMenuItemW(hMenu, -1, TRUE, &mii);
    }
}

void ToothTrayMenu::ShowPopupMenu(HWND hwnd)
{
    SetForegroundWindow(hwnd);  // 必须！否则菜单不会自动关闭
    TrackPopupMenuEx(hMenu, TPM_RIGHTBUTTON, x, y, hwnd, NULL);
}
```

#### 2.2.5 C# 移植要点

| C++ 组件 | C# 替代方案 |
|----------|-------------|
| `IMMDeviceEnumerator` | `NAudio.CoreAudioApi.MMDeviceEnumerator` |
| `IDeviceTopology` | `Vanara.PInvoke.CoreAudio` |
| `IKsControl` | 需自定义 COM Interop (见附录) |
| `DeviceWatcher` | `Windows.Devices.Enumeration.DeviceWatcher` |
| `Shell_NotifyIcon` | `Hardcodet.NotifyIcon.Wpf` |

---

### 2.3 BlueGauge (Rust) - 电量获取参考

> **路径**: `code/BlueGauge`  
> **框架**: Rust + Windows-rs + tray-icon  
> **价值**: ⭐⭐⭐⭐ BLE/BTC 双路径电量获取的完整实现

#### 2.3.1 项目架构

```
src/
├── bluetooth/
│   ├── mod.rs         # 模块导出
│   ├── ble.rs         # BLE 电量获取 (GATT)
│   ├── btc.rs         # 经典蓝牙电量获取 (PnP)
│   ├── info.rs        # BluetoothInfo 结构定义
│   └── watch.rs       # 设备监控协调器
├── tray/
│   ├── mod.rs         # 托盘逻辑
│   └── icon.rs        # 动态图标渲染
├── config.rs          # TOML 配置
├── notify.rs          # Windows 通知
└── main.rs
```

#### 2.3.2 BLE 电量获取 (GATT 标准服务)

```rust
// ble.rs - 使用标准 Battery Service
const BATTERY_SERVICE_UUID: Uuid = Uuid::from_u16(0x180F);      // Battery Service
const BATTERY_LEVEL_UUID: Uuid = Uuid::from_u16(0x2A19);        // Battery Level

pub async fn get_ble_battery_level(device: &BluetoothLEDevice) -> Option<u8> {
    // 1. 获取 GATT 服务
    let services = device
        .GetGattServicesForUuidAsync(GattServiceUuids::Battery())?
        .await?;
    
    let service = services.Services()?.GetAt(0)?;
    
    // 2. 获取电量特征
    let chars = service
        .GetCharacteristicsForUuidAsync(GattCharacteristicUuids::BatteryLevel())?
        .await?;
    
    let battery_char = chars.Characteristics()?.GetAt(0)?;
    
    // 3. 读取电量值 (单字节 0-100)
    let result = battery_char.ReadValueAsync()?.await?;
    let buffer = result.Value()?;
    let reader = DataReader::FromBuffer(&buffer)?;
    
    Some(reader.ReadByte()?)
}

// 订阅电量变化通知
pub async fn watch_ble_battery(device: &BluetoothLEDevice, tx: Sender<BatteryUpdate>) {
    let battery_char = /* 获取特征 */;
    
    // 启用通知
    battery_char.WriteClientCharacteristicConfigurationDescriptorAsync(
        GattClientCharacteristicConfigurationDescriptorValue::Notify
    )?.await?;
    
    // 监听值变化
    battery_char.ValueChanged(&TypedEventHandler::new(move |_, args| {
        let value = /* 解析电量 */;
        tx.send(BatteryUpdate { device_id, battery: value });
        Ok(())
    }))?;
}
```

#### 2.3.3 经典蓝牙电量获取 (PnP 设备属性)

```rust
// btc.rs - 使用 Windows PnP API
// 关键 DEVPKEY: {104EA319-6EE2-4701-BD47-8DDBF425BBE5}, pid=2
const DEVPKEY_BLUETOOTH_BATTERY: DEVPROPKEY = DEVPROPKEY {
    fmtid: GUID::from_u128(0x104EA319_6EE2_4701_BD47_8DDBF425BBE5),
    pid: 2,
};

pub fn read_btc_battery(instance_id: &str) -> Option<u8> {
    unsafe {
        // 1. 定位设备节点
        let mut dev_inst: u32 = 0;
        CM_Locate_DevNodeW(&mut dev_inst, instance_id, CM_LOCATE_DEVNODE_NORMAL)?;
        
        // 2. 读取电量属性
        let mut prop_type: u32 = 0;
        let mut buffer: [u8; 1] = [0];
        let mut size: u32 = 1;
        
        CM_Get_DevNode_PropertyW(
            dev_inst,
            &DEVPKEY_BLUETOOTH_BATTERY,
            &mut prop_type,
            buffer.as_mut_ptr(),
            &mut size,
            0
        )?;
        
        Some(buffer[0])  // 返回 0-100
    }
}

// 轮询监控 (经典蓝牙不支持通知)
pub async fn watch_btc_battery(devices: Vec<String>, tx: Sender<BatteryUpdate>) {
    loop {
        for instance_id in &devices {
            if let Some(battery) = read_btc_battery(instance_id) {
                tx.send(BatteryUpdate { instance_id, battery });
            }
        }
        tokio::time::sleep(Duration::from_secs(5)).await;  // 5秒轮询
    }
}
```

#### 2.3.4 设备监控器 (Watcher)

```rust
// watch.rs - 协调 4 个并行监控任务
pub struct Watcher {
    ble_battery_handle: JoinHandle<()>,    // BLE 电量 (事件驱动)
    btc_battery_handle: JoinHandle<()>,    // BTC 电量 (5秒轮询)
    btc_status_handle: JoinHandle<()>,     // BTC 连接状态
    presence_handle: JoinHandle<()>,        // 设备添加/移除
}

impl Watcher {
    pub async fn start(tx: Sender<BluetoothEvent>) {
        // BLE: 事件驱动，通过 GATT 通知
        let ble_handle = tokio::spawn(watch_ble_devices(tx.clone()));
        
        // BTC: 轮询，每 5 秒检查一次
        let btc_battery_handle = tokio::spawn(watch_btc_battery(tx.clone()));
        
        // 设备存在性: 使用 DeviceWatcher
        let presence_handle = tokio::spawn(watch_device_presence(tx.clone()));
    }
}
```

#### 2.3.5 托盘图标动态渲染

```rust
// icon.rs - 4 种图标样式
pub enum TrayIconStyle {
    App,            // 静态应用图标
    BatteryIcon,    // 系统电池图标 (Segoe Fluent Icons)
    BatteryNumber,  // 数字百分比
    BatteryRing,    // 圆环进度条
}

fn render_battery_icon(level: u8, style: TrayIconStyle) -> Icon {
    match style {
        TrayIconStyle::BatteryIcon => {
            // 使用 Windows 系统字体绘制
            let glyph = match level {
                0..=10  => '\u{EBA0}',   // 电池空
                11..=30 => '\u{EBA1}',   // 电池低
                31..=60 => '\u{EBA2}',   // 电池中
                61..=90 => '\u{EBA3}',   // 电池高
                _ => '\u{EBA4}',          // 电池满
            };
            render_glyph("Segoe Fluent Icons", glyph)
        }
        TrayIconStyle::BatteryRing => {
            // 使用 piet 绘制圆环
            let mut ctx = piet_common::Device::new()?;
            draw_ring(&mut ctx, level as f64 / 100.0);
            ctx.to_icon()
        }
        // ...
    }
}
```

#### 2.3.6 C# 移植要点

| Rust 组件 | C# 替代方案 |
|-----------|-------------|
| `windows-rs` WinRT | `Microsoft.Windows.SDK.Contracts` |
| `CM_Get_DevNode_PropertyW` | `Vanara.PInvoke.CfgMgr32` 或 P/Invoke |
| `GattServiceUuids::Battery()` | `Windows.Devices.Bluetooth.GenericAttributeProfile` |
| `tokio` async | `async/await` + `Task.Run` |
| `tray-icon` | `Hardcodet.NotifyIcon.Wpf` |

---

### 2.4 Alternative-A2DP-Driver - 音质增强参考

> **路径**: `code/Alternative-A2DP-Driver`  
> **类型**: 第三方驱动安装指南  
> **价值**: ⭐⭐ 可选高级功能

#### 2.4.1 解决的问题

Windows 原生蓝牙驱动仅支持基础编解码器 (SBC)，导致：
- 音质损失明显
- 不支持 LDAC、aptX HD 等高清编解码器
- 延迟较高

#### 2.4.2 技术方案

| 编解码器 | 比特率 | 原生支持 | A2DP 驱动 |
|----------|--------|----------|-----------|
| SBC | ~328kbps | ✅ | ✅ |
| AAC | ~256kbps | ❌ | ✅ |
| aptX | ~352kbps | ❌ | ✅ |
| aptX HD | ~576kbps | ❌ | ✅ |
| LDAC | 最高990kbps | ❌ | ✅ |

#### 2.4.3 集成建议

由于需要禁用驱动签名验证 (DSE)，**不建议**作为核心功能集成。可作为：
- 高级设置中的"音质增强向导"
- 外部链接引导用户手动安装
- 检测已安装的 A2DP 驱动并显示当前编解码器

---

## 3. 技术架构设计

### 3.1 整体架构

```
┌────────────────────────────────────────────────────────────────────┐
│                         表现层 (WPF)                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐  │
│  │  TrayIcon    │  │ FlyoutWindow │  │    SettingsWindow        │  │
│  │  (托盘图标)  │  │ (悬浮面板)   │  │    (设置窗口)            │  │
│  └──────────────┘  └──────────────┘  └──────────────────────────┘  │
│           │                │                      │                 │
│           └────────────────┼──────────────────────┘                 │
│                            ▼                                        │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    ViewModel 层 (MVVM)                       │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │   │
│  │  │TrayViewModel│  │DeviceListVM │  │  SettingsViewModel  │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────┘   │
├────────────────────────────────────────────────────────────────────┤
│                         服务层 (Services)                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐   │
│  │ BluetoothService│  │  AudioService   │  │  ConfigService   │   │
│  │ ├─ DeviceFinder │  │ ├─ Enumerator   │  │ ├─ Load/Save     │   │
│  │ ├─ BatteryReader│  │ ├─ Connector    │  │ ├─ DeviceAlias   │   │
│  │ └─ DeviceWatcher│  │ └─ Disconnector │  │ └─ Preferences   │   │
│  └─────────────────┘  └─────────────────┘  └──────────────────┘   │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                   NotificationService                        │   │
│  │  ├─ LowBatteryAlert    ├─ ConnectionStateChange              │   │
│  └─────────────────────────────────────────────────────────────┘   │
├────────────────────────────────────────────────────────────────────┤
│                         平台层 (Platform)                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌──────────┐  │
│  │ WinRT APIs  │  │ CoreAudio   │  │  CfgMgr32   │  │  Win32   │  │
│  │ (蓝牙枚举)  │  │ (IKsControl)│  │ (PnP电量)   │  │  (托盘)  │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  └──────────┘  │
└────────────────────────────────────────────────────────────────────┘
```

### 3.2 项目结构

```
CyanTooth/
├── CyanTooth.sln
├── src/
│   ├── CyanTooth/                 # 主项目 (WPF)
│   │   ├── App.xaml(.cs)
│   │   ├── CyanTooth.csproj
│   │   │
│   │   ├── Views/                        # XAML 视图
│   │   │   ├── TrayIconView.xaml         # 托盘图标资源
│   │   │   ├── FlyoutWindow.xaml         # 悬浮面板
│   │   │   ├── DeviceCard.xaml           # 设备卡片控件
│   │   │   └── SettingsWindow.xaml       # 设置窗口
│   │   │
│   │   ├── ViewModels/                   # MVVM ViewModel
│   │   │   ├── MainViewModel.cs
│   │   │   ├── DeviceViewModel.cs
│   │   │   └── SettingsViewModel.cs
│   │   │
│   │   ├── Controls/                     # 自定义控件
│   │   │   ├── BatteryIndicator.xaml     # 电量指示器
│   │   │   └── DeviceIcon.xaml           # 设备类型图标
│   │   │
│   │   ├── Themes/                       # 主题资源
│   │   │   ├── Generic.xaml
│   │   │   ├── LightTheme.xaml
│   │   │   └── DarkTheme.xaml
│   │   │
│   │   └── Resources/                    # 静态资源
│   │       ├── Icons/
│   │       └── Strings/
│   │
│   ├── CyanTooth.Core/            # 核心业务逻辑
│   │   ├── CyanTooth.Core.csproj
│   │   │
│   │   ├── Models/                       # 数据模型
│   │   │   ├── BluetoothDeviceInfo.cs
│   │   │   ├── DeviceType.cs
│   │   │   └── ConnectionState.cs
│   │   │
│   │   ├── Services/                     # 服务接口与实现
│   │   │   ├── IBluetoothService.cs
│   │   │   ├── BluetoothService.cs
│   │   │   ├── IAudioService.cs
│   │   │   ├── AudioService.cs
│   │   │   ├── IConfigService.cs
│   │   │   ├── ConfigService.cs
│   │   │   └── NotificationService.cs
│   │   │
│   │   └── Events/                       # 事件定义
│   │       ├── DeviceStateChangedEvent.cs
│   │       └── BatteryLevelChangedEvent.cs
│   │
│   └── CyanTooth.Platform/        # 平台相关代码
│       ├── CyanTooth.Platform.csproj
│       │
│       ├── Bluetooth/                    # 蓝牙 API 封装
│       │   ├── DeviceDiscoverer.cs       # 设备发现
│       │   ├── DeviceWatcher.cs          # 状态监控
│       │   ├── BleBatteryReader.cs       # BLE 电量
│       │   └── BtcBatteryReader.cs       # 经典蓝牙电量
│       │
│       ├── Audio/                        # 音频 API 封装
│       │   ├── AudioEndpointEnumerator.cs
│       │   ├── BluetoothConnector.cs     # IKsControl
│       │   └── KsPropertyInterop.cs      # COM Interop
│       │
│       └── Native/                       # P/Invoke 定义
│           ├── CfgMgr32.cs
│           └── BluetoothApis.cs
│
├── tests/
│   └── CyanTooth.Tests/
│
└── docs/
    └── ARCHITECTURE.md                   # 本文档
```

### 3.3 核心依赖

```xml
<!-- CyanTooth.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>  <!-- NotifyIcon 需要 -->
  </PropertyGroup>
  
  <ItemGroup>
    <!-- 系统托盘 -->
    <PackageReference Include="Hardcodet.NotifyIcon.Wpf" Version="1.1.0" />
    
    <!-- 现代 UI 组件 -->
    <PackageReference Include="WPF-UI" Version="3.0.0" />
    
    <!-- MVVM 框架 -->
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
    
    <!-- 依赖注入 -->
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
  </ItemGroup>
</Project>

<!-- CyanTooth.Platform.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- CoreAudio COM 接口 -->
    <PackageReference Include="Vanara.PInvoke.CoreAudio" Version="4.0.0" />
    
    <!-- CfgMgr32 (电量读取) -->
    <PackageReference Include="Vanara.PInvoke.CfgMgr32" Version="4.0.0" />
    
    <!-- WinRT 投影 -->
    <PackageReference Include="Microsoft.Windows.SDK.Contracts" 
                      Version="10.0.22621.755" />
  </ItemGroup>
</Project>
```

---

## 4. 核心模块实现路径

### 4.1 蓝牙设备发现与枚举

#### 4.1.1 实现方案

结合 BluetoothDevicePairing 的 `DeviceFinder.cs` 和 ToothTray 的 `BluetoothDeviceWatcher`：

```csharp
// Services/BluetoothService.cs
public class BluetoothService : IBluetoothService, IDisposable
{
    private DeviceWatcher _classicWatcher;
    private DeviceWatcher _bleWatcher;
    private readonly ConcurrentDictionary<string, BluetoothDeviceInfo> _devices = new();
    
    public event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;
    
    public async Task StartDiscoveryAsync()
    {
        // 经典蓝牙设备选择器
        string classicSelector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        _classicWatcher = DeviceInformation.CreateWatcher(classicSelector);
        
        // BLE 设备选择器
        string bleSelector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
        _bleWatcher = DeviceInformation.CreateWatcher(bleSelector);
        
        // 注册事件
        _classicWatcher.Added += OnDeviceAdded;
        _classicWatcher.Updated += OnDeviceUpdated;
        _classicWatcher.Removed += OnDeviceRemoved;
        
        _bleWatcher.Added += OnDeviceAdded;
        _bleWatcher.Updated += OnDeviceUpdated;
        _bleWatcher.Removed += OnDeviceRemoved;
        
        _classicWatcher.Start();
        _bleWatcher.Start();
    }
    
    private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation info)
    {
        var device = await CreateDeviceInfoAsync(info);
        _devices.TryAdd(info.Id, device);
        DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs(device, ChangeType.Added));
    }
    
    private async Task<BluetoothDeviceInfo> CreateDeviceInfoAsync(DeviceInformation info)
    {
        // 尝试获取经典蓝牙设备
        var btDevice = await BluetoothDevice.FromIdAsync(info.Id);
        if (btDevice != null)
        {
            return new BluetoothDeviceInfo
            {
                Id = info.Id,
                Name = btDevice.Name,
                Address = btDevice.BluetoothAddress,
                DeviceType = DeviceType.Classic,
                IsConnected = btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected
            };
        }
        
        // 尝试获取 BLE 设备
        var bleDevice = await BluetoothLEDevice.FromIdAsync(info.Id);
        if (bleDevice != null)
        {
            return new BluetoothDeviceInfo
            {
                Id = info.Id,
                Name = bleDevice.Name,
                Address = bleDevice.BluetoothAddress,
                DeviceType = DeviceType.LowEnergy,
                IsConnected = bleDevice.ConnectionStatus == BluetoothConnectionStatus.Connected
            };
        }
        
        return null;
    }
}
```

#### 4.1.2 设备信息模型

```csharp
// Models/BluetoothDeviceInfo.cs
public class BluetoothDeviceInfo : ObservableObject
{
    public string Id { get; init; }
    public string Name { get; init; }
    public ulong Address { get; init; }
    public DeviceType DeviceType { get; init; }
    
    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }
    
    private byte? _batteryLevel;
    public byte? BatteryLevel
    {
        get => _batteryLevel;
        set => SetProperty(ref _batteryLevel, value);
    }
    
    // MAC 地址格式化显示
    public string MacAddress => $"{Address:X12}".Insert(10, ":").Insert(8, ":")
        .Insert(6, ":").Insert(4, ":").Insert(2, ":");
    
    // PnP 实例 ID (用于经典蓝牙电量读取)
    public string InstanceId { get; set; }
}

public enum DeviceType { Classic, LowEnergy }
```

