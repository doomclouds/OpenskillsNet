# OpenSkills C# CLI 部署指南

本指南说明如何在 Windows 系统上全局部署 C# 版本的 OpenSkills CLI，使其可以在任意位置的 cmd 或 PowerShell 中执行。

## 前置要求

- **.NET 10.0 SDK 或更高版本**
  - 下载地址: https://dotnet.microsoft.com/download
  - 验证安装: 运行 `dotnet --version`

## 快速部署

### 方法 1: 使用 PowerShell 脚本（推荐）

1. 以管理员权限打开 PowerShell
2. 导航到项目根目录
3. 运行部署脚本：

```powershell
.\deploy.ps1
```

如果需要强制覆盖现有安装：

```powershell
.\deploy.ps1 -Force
```

自定义安装路径：

```powershell
.\deploy.ps1 -InstallPath "C:\Tools\openskills\bin"
```

创建自包含部署（包含 .NET 运行时，无需单独安装 .NET）：

```powershell
.\deploy.ps1 -SelfContained
```

自包含部署会生成更大的文件，但可以在没有安装 .NET 运行时的系统上运行。

### 方法 2: 使用 CMD 脚本

1. 以管理员权限打开命令提示符
2. 导航到项目根目录
3. 运行部署脚本：

```cmd
deploy.cmd
```

强制覆盖：

```cmd
deploy.cmd -Force
```

## 部署过程说明

部署脚本会执行以下操作：

1. **检查 .NET SDK** - 验证是否安装了 .NET 10.0 SDK
2. **构建项目** - 编译 Release 版本的 C# CLI
3. **发布应用** - 创建可发布的文件包
4. **复制文件** - 将可执行文件复制到安装目录（默认: `%LOCALAPPDATA%\openskills\bin`）
5. **添加到 PATH** - 将安装目录添加到用户 PATH 环境变量

## 安装后使用

### 方法 1: 使用 openskills 命令（推荐）

部署脚本会自动安装包装脚本，重启终端后可以直接使用：

**PowerShell:**
```powershell
# 直接使用（推荐，需要重启终端使 PATH 生效）
openskills list
openskills install anthropics/skills
openskills sync

# 或者使用相对路径（如果脚本在当前目录）
.\openskills list
```

**CMD:**
```cmd
openskills list
openskills install anthropics/skills
openskills sync
```

**注意：** 在 PowerShell 中，如果遇到执行策略限制，可以使用：
- `.\openskills.cmd list` （使用 CMD 包装脚本，推荐）
- `.\openskills.ps1 list` （需要执行策略允许）

包装脚本会自动调用 `OpenSkills.Cli.exe`，无需额外配置。

### 方法 2: 直接使用可执行文件

如果包装脚本不可用，可以直接使用：

```cmd
OpenSkills.Cli.exe list
OpenSkills.Cli.exe install anthropics/skills
OpenSkills.Cli.exe sync
```

## 验证安装

运行以下命令验证安装是否成功：

**PowerShell:**
```powershell
openskills --version
```

**CMD:**
```cmd
openskills --version
```

或者直接使用可执行文件：
```cmd
OpenSkills.Cli.exe --version
```

## 卸载

要卸载 OpenSkills CLI：

1. 从 PATH 环境变量中移除安装目录
2. 删除安装目录（默认: `%LOCALAPPDATA%\openskills\bin`）

## 故障排除

### PATH 未生效

如果重启终端后仍然无法找到命令：

1. **PowerShell**: 运行以下命令刷新环境变量：
   ```powershell
   $env:Path = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')
   ```

2. **CMD**: 关闭并重新打开命令提示符

### 构建失败

如果构建过程中出现错误：

1. 确保已安装 .NET 10.0 SDK
2. 清理构建缓存：
   ```cmd
   dotnet clean OpenSkills.sln
   ```
3. 重新运行部署脚本

### 权限问题

如果遇到权限错误：

1. 确保以管理员权限运行脚本
2. 检查安装目录的写入权限
3. 如果 PATH 更新失败，可以手动添加到系统环境变量

## 默认安装路径

- **用户级安装**: `%LOCALAPPDATA%\openskills\bin` (推荐)
- **系统级安装**: `%ProgramFiles%\openskills\bin` (需要管理员权限)

## 相关文件

- `deploy.ps1` - PowerShell 部署脚本
- `deploy.cmd` - CMD 部署脚本
- `openskills.ps1` - PowerShell 包装脚本（自动安装）
- `openskills.cmd` - CMD 包装脚本（自动安装）

## 注意事项

- 部署脚本会修改用户 PATH 环境变量
- 如果安装目录已存在，默认会提示是否覆盖（使用 `-Force` 可跳过提示）
- 首次使用前需要重启终端以使 PATH 更改生效
