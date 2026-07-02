# CleanShip 模组 V81 适配版

## 项目说明

本项目是 Lethal Company（致命公司）的 CleanShip 模组的 V81 版本适配。

### 原始模组功能

- **飞船整理 (CleanShip)**: 自动整理飞船内的物品，按类型分类放置
- **废料收集 (CollectScrap)**: 自动将地牢中的废料收集到飞船
- **玩家传送 (Teleport)**: 传送到指定玩家位置
- **夜视增强 (NightVision)**: 增强夜视能力
- **复活玩家 (Revive)**: 复活已死亡的队友
- **伤害检测 (DamageCheck)**: 显示谁在攻击谁
- **击杀敌人 (KillEnemy)**: 瞄准敌人按 Z 键击杀
- **穿墙模式 (Noclip)**: 飞行穿墙
- **强制断开 (ForceDisconnect)**: 强制断开游戏连接

### V81 适配变更

#### 1. Transpiler 补丁修复
- **问题**: 原版使用硬编码的 IL 指令索引（73 和 58）来修改 `Debug_ReviveAllPlayersServerRpc` 和 `Debug_ReviveAllPlayersClientRpc` 方法。V80/V81 的方法体发生了变化，导致索引失效。
- **修复**: 改用模式匹配策略，先尝试原始索引，如果失败则搜索匹配的 IL 指令模式（`Brfalse`/`Brfalse_S`），最后使用兜底策略。

#### 2. 方法签名适配
- `SetObjectAsNoLongerHeld`: 适配 V80/V81 可能的参数变化
- `SwitchToItemSlot`: 适配新的实用物品栏位系统
- `DamagePlayerFromOtherClientServerRpc`: 适配签名变化

#### 3. 夜视范围补偿
- V80 降低了夜视范围，本模组将夜视范围设为 5000 以补偿

#### 4. 依赖更新
- BepInEx: 5.4.21 → 5.4.2305
- 移除对 OPJosMod (ReviveCompany) 的硬依赖，改为可选依赖

## 编译说明

### 前置条件

1. 安装 .NET Framework 4.8 SDK 或 .NET 6+ SDK
2. 获取以下 DLL 文件（从你的 Lethal Company 安装目录）：

```
Lethal Company/
├── Lethal Company_Data/
│   └── Managed/
│       ├── Assembly-CSharp.dll          # 游戏核心代码
│       ├── Unity.Netcode.Runtime.dll     # 网络模块
│       ├── UnityEngine.dll               # Unity 核心
│       ├── UnityEngine.CoreModule.dll    # Unity 核心模块
│       ├── UnityEngine.InputModule.dll   # 输入模块
│       ├── UnityEngine.InputLegacyModule.dll
│       ├── UnityEngine.PhysicsModule.dll
│       ├── UnityEngine.AnimationModule.dll
│       ├── UnityEngine.IMGUIModule.dll
│       ├── UnityEngine.UI.dll            # UI 模块
│       ├── UnityEngine.UIModule.dll
│       ├── Facepunch.Steamworks.Win64.dll
│       └── ...
├── BepInEx/
│   ├── core/
│   │   ├── BepInEx.dll                   # BepInEx 核心
│   │   ├── 0Harmony.dll                  # Harmony 补丁库
│   │   └── ...
│   └── plugins/
│       └── OPJosMod/                     # 可选：复活模组
│           └── OPJosMod.dll
```

### 编译步骤

#### 方式一：使用 .NET CLI（推荐）

1. 编辑 `CustomCompany.CleabShip.csproj`，将 DLL 引用路径修改为你本地的实际路径：

```xml
<Reference Include="Assembly-CSharp">
  <HintPath>C:\你的路径\Lethal Company_Data\Managed\Assembly-CSharp.dll</HintPath>
</Reference>
```

2. 编译：
```bash
dotnet build -c Release
```

3. 输出文件在 `bin/Release/net48/CustomCompany.CleabShip.dll`

#### 方式二：使用 Visual Studio

1. 用 Visual Studio 打开 `CustomCompany.CleabShip.csproj`
2. 修改 DLL 引用路径
3. 选择 Release 配置，生成解决方案

### 安装

1. 确保已安装 BepInEx 5.4.2305+
2. 将编译好的 `CustomCompany.CleabShip.dll` 放入 `BepInEx/plugins/` 目录
3. 启动游戏

## 文件结构

```
CleanShipV81/
├── CustomCompany.CleabShip.csproj  # 项目文件
├── Plugin.cs                        # BepInEx 插件入口
├── CustomCompanyManager.cs          # 管理器 + 工具类
├── CustomCompanyConfig.cs           # 配置管理
├── Patches.cs                       # Harmony 补丁（核心适配）
├── Behaviours.cs                    # 行为逻辑（整理、收集、传送等）
├── README.md                        # 说明文档
└── build.sh                         # 构建脚本
```

## 已知问题与注意事项

1. **Transpiler 补丁**: 如果 V81 的 `Debug_ReviveAllPlayersServerRpc` 方法体发生了较大变化，自动模式匹配可能失败。此时需要查看日志中的警告信息，并手动调整补丁策略。

2. **OPJosMod 依赖**: 原版依赖 OPJosMod 的 `ReviveBehaviour.RevivePlayer()` 方法。如果该模组未安装，复活功能将不可用。建议安装兼容 V81 的复活模组。

3. **实用物品栏位**: V80 新增了实用物品栏位，可能影响物品整理逻辑。如果遇到物品分类不正确的问题，可以在配置中自定义物品位置。

4. **夜视范围**: 由于 V80 降低了夜视范围，本模组的夜视增强可能比之前版本更明显。

## 许可证

本项目基于原始 CustomCompany.CleabShip 模组修改，仅供学习交流使用。
