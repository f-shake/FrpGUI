#!/bin/bash

# 构建frp并复制到Publish/frp目录的脚本
echo "=== 开始构建frp for macOS ==="

# 设置相关变量
FRP_MODULE_DIR="./frp"
FRP_BUILD_DIR="$FRP_MODULE_DIR/build"
OUTPUT_DIR="./Publish"
FRP_OUTPUT_DIR="$OUTPUT_DIR/frp"

# 检查是否在正确的项目目录
echo "检查项目文件..."
if [ ! -d "$FRP_MODULE_DIR" ]; then
    echo "错误: 未找到frp子模块目录，请确保在正确的项目目录中运行脚本"
    exit 1
fi

# 创建输出目录
echo "创建输出目录..."
mkdir -p "$FRP_OUTPUT_DIR"

# 检查Go是否安装
echo "检查Go安装情况..."
if ! command -v go &> /dev/null; then
    echo "错误: 未安装Go，无法编译frp"
    exit 1
fi

# 进入frp目录
echo "进入frp子模块目录..."
cd "$FRP_MODULE_DIR"

# 确保构建目录存在
echo "创建构建目录..."
mkdir -p "build"

# 尝试使用make命令编译
echo "尝试使用Makefile编译frp..."
if [ -f "Makefile" ]; then
    echo "使用Makefile编译..."
    make build
    if [ $? -eq 0 ]; then
        echo "✓ Makefile编译成功"
    else
        echo "✗ Makefile编译失败，尝试直接使用go build"
        # 直接使用go build编译
        echo "直接使用go build编译frpc..."
        go build -o "build/frpc" ./cmd/frpc
        frpc_result=$?
        
        echo "直接使用go build编译frps..."
        go build -o "build/frps" ./cmd/frps
        frps_result=$?
        
        if [ $frpc_result -ne 0 ] || [ $frps_result -ne 0 ]; then
            echo "✗ frp编译失败"
            exit 1
        fi
    fi
else
    # 如果没有Makefile，直接使用go build
    echo "未找到Makefile，直接使用go build编译..."
    echo "编译frpc..."
    go build -o "build/frpc" ./cmd/frpc
    frpc_result=$?
    
    echo "编译frps..."
    go build -o "build/frps" ./cmd/frps
    frps_result=$?
    
    if [ $frpc_result -ne 0 ] || [ $frps_result -ne 0 ]; then
        echo "✗ frp编译失败"
        exit 1
    fi
fi

# 返回上一级目录
cd ..

# 检查编译结果
echo "检查编译结果..."
if [ -f "$FRP_BUILD_DIR/frpc" ] && [ -f "$FRP_BUILD_DIR/frps" ]; then
    echo "✓ frp编译成功"
    
    # 复制编译好的二进制文件到输出目录
    echo "复制frp二进制文件到 $FRP_OUTPUT_DIR..."
    cp -f "$FRP_BUILD_DIR/frpc" "$FRP_OUTPUT_DIR/frpc"
    cp -f "$FRP_BUILD_DIR/frps" "$FRP_OUTPUT_DIR/frps"
    
    # 设置可执行权限
    echo "设置可执行权限..."
    chmod +x "$FRP_OUTPUT_DIR/frpc"
    chmod +x "$FRP_OUTPUT_DIR/frps"
    
    echo "✓ frp二进制文件已成功复制到: $FRP_OUTPUT_DIR"
    
    # 显示复制的文件信息
    echo "\n复制的文件信息:"
    ls -la "$FRP_OUTPUT_DIR"
else
    echo "✗ 编译失败，未找到frpc或frps文件"
    exit 1
fi

echo "=== frp构建完成 ==="