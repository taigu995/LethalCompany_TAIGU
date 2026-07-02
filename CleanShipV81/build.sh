#!/bin/bash
# CleanShip V81 构建脚本
# 使用前请确保已安装 dotnet SDK 并修改 csproj 中的 DLL 引用路径

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

echo "========================================="
echo "  CleanShip V81 适配版 - 构建脚本"
echo "========================================="

# 检查 dotnet
if ! command -v dotnet &> /dev/null; then
    echo "[错误] 未找到 dotnet SDK，请先安装 .NET SDK 6.0+"
    exit 1
fi

echo "[信息] dotnet 版本: $(dotnet --version)"

# 构建
echo "[信息] 开始构建..."
dotnet build -c Release

if [ $? -eq 0 ]; then
    echo ""
    echo "========================================="
    echo "  构建成功！"
    echo "  输出: bin/Release/net48/CustomCompany.CleabShip.dll"
    echo "========================================="
    echo ""
    echo "请将生成的 DLL 文件复制到:"
    echo "  Lethal Company/BepInEx/plugins/"
else
    echo ""
    echo "========================================="
    echo "  构建失败！请检查错误信息。"
    echo "========================================="
    exit 1
fi
