# 奶蛙桌面宠物

奶蛙是一款面向 Windows 10 和 Windows 11 的原生桌面宠物，目前专注幼年形态与轻量陪伴体验

当前版本为 `0.1.3`

## 下载与运行

普通用户请从 GitHub Releases 下载 `MilkFrog-Windows-0.1.3.zip`

完整解压后运行 `MilkFrog.exe`，程序需要 .NET 10 Desktop Runtime

动画与道具图片已经内嵌到单文件程序中，发布包不会附带散装 PNG 素材

## 桌面操作

- 单击奶蛙进行抚摸，睡眠时单击可唤醒
- 按住奶蛙拖动位置，释放后播放落地动作
- 悬停奶蛙打开喂奶、洗澡、玩球、休息、背包等快捷操作
- 右键奶蛙打开状态、学习打工、商店、背包、成长记录和设置
- 双击托盘图标找回奶蛙
- 通过托盘菜单控制显示、置顶、位置锁定和退出

角色透明区域不会阻挡桌面点击，日常只有奶蛙常驻，其他功能按需打开

## 当前功能

- 136 张连续帧与 24 条原生动作时间线
- 喂奶、洗澡、吃点心、玩球、睡觉、醒来、拖拽和疲惫反馈
- 饱食、清洁、心情、健康和精力五项状态
- 奶豆商店、背包、永久玩具、每日奖励和低余额救助
- 学习课程、技能要求、打工收入和中途结算
- 等级与成长记录，内容解锁延伸至 Lv.30
- 自动保存、备份恢复、位置记忆、单实例运行和自定义数据目录

所有等级保持幼年外观，当前版本不包含成年形态

## 用户数据

默认记录保存在程序旁边的 `UserData\state.json`

设置页可以把用户数据迁移到其他可写目录，更新或移动程序前请保留 `UserData`

## 仓库结构

```text
src/MilkFrog.Core    养成规则、状态与存档
src/MilkFrog.App     WPF 桌面窗口、动画、托盘与面板
tests/MilkFrog.Tests 无窗口核心检查
tools                图标、动画处理与 Windows 发布脚本
design               产品、动作与界面设计记录
assets               参考图、生成素材、精灵表与连续帧
```

## 开发与构建

开发环境需要 .NET 10 SDK 和 PowerShell 7

仓库已经包含构建所需的角色图片、精灵表与连续帧素材

```powershell
.\tools\Build-Windows.ps1
```

也可以通过 MSBuild 属性 `MilkFrogAssetRoot` 指向其他私有素材目录

```powershell
dotnet build .\src\MilkFrog.App\MilkFrog.App.csproj -p:MilkFrogAssetRoot=D:\Private\MilkFrogAssets
```

发布脚本会生成图标、编译项目、运行自动检查、发布单文件程序并生成 ZIP

## 发布方式

仓库提交源码、设计文档与美术素材，不提交 `dist`、构建缓存和用户数据

正式成品请上传到 GitHub Releases，不要把 ZIP 直接提交进 Git 历史

## 授权

项目由 Ti 以 MIT License 开源，代码与仓库内素材均可依照许可证使用

🔗 友情链接
LINUX DO —— 真诚分享、友好讨论的技术社区，本项目的交流与反馈也发布于此
