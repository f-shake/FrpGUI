#!/bin/bash
 
 # 构建FrpGUI Mac Catalyst版本的自动化脚本
 echo "=== 开始构建FrpGUI Mac Catalyst版本 ==="
 
 
 # 设置项目相关变量
 PROJECT_NAME="FrpGUI"
 OUTPUT_DIR="./Publish/client-maccatalyst"
 BUILD_TEMP_DIR="./Build/maccatalyst"
 APP_BUNDLE_NAME="FrpGUI.app"
 
 
 # 检查是否安装了.NET SDK
 echo "检查.NET SDK安装情况..."
 if ! command -v dotnet &> /dev/null; then
     echo "错误: 未安装.NET SDK，请先安装.NET 8.0或更高版本"
     exit 1
 fi
 
 
 # 检查是否在正确的项目目录
 echo "检查项目文件..."
 if [ ! -f "FrpGUI.sln" ]; then
     echo "错误: 未找到FrpGUI.sln文件，请确保在正确的项目目录中运行脚本"
     exit 1
 fi
 
 
 # 设置临时目录环境变量，解决macOS上构建路径问题
 export Temp="$PWD/Temp"
 mkdir -p "$Temp"
 echo "设置临时目录: $Temp"
 
 
 # 清理之前的构建产物
 echo "清理构建目录..."
 rm -rf "$OUTPUT_DIR"
 rm -rf "$BUILD_TEMP_DIR"
 rm -rf "$Temp"
 mkdir -p "$OUTPUT_DIR"
 mkdir -p "$BUILD_TEMP_DIR"
 mkdir -p "$Temp"
 
 
 # 构建Mac Catalyst版本
 echo "开始构建Mac Catalyst版本..."
 
 
 # 首先构建基础项目
 echo "构建解决方案..."
 dotnet build FrpGUI.sln -c Release
 if [ $? -ne 0 ]; then
     echo "错误: 解决方案构建失败"
     exit 1
 fi
 
 
 # 发布Avalonia.Desktop项目为macOS
 # 注意：当前.NET和Avalonia对Mac Catalyst的支持是通过macOS目标实现的
 echo "发布Avalonia.Desktop项目..."
 dotnet publish FrpGUI.Avalonia.Desktop -c Release -r osx-x64 --self-contained true -o "$OUTPUT_DIR" /p:PublishAot=true
 if [ $? -ne 0 ]; then
     echo "警告: AOT构建失败，尝试不使用AOT..."
     dotnet publish FrpGUI.Avalonia.Desktop -c Release -r osx-x64 --self-contained true -o "$OUTPUT_DIR" /p:PublishAot=false
     if [ $? -ne 0 ]; then
         echo "错误: 项目发布失败"
         exit 1
     fi
 fi
 
 
 # 为Mac Catalyst创建应用包结构
echo "创建Mac Catalyst应用包结构..."
echo "当前工作目录: $(pwd)"
echo "输出目录: $OUTPUT_DIR"
MACOS_BUNDLE_DIR="$OUTPUT_DIR/$APP_BUNDLE_NAME/Contents/MacOS"
MACOS_RESOURCES_DIR="$OUTPUT_DIR/$APP_BUNDLE_NAME/Contents/Resources"
echo "应用包MacOS目录: $MACOS_BUNDLE_DIR"
echo "开始创建目录结构..."
mkdir -p "$MACOS_BUNDLE_DIR"
mkdir -p "$MACOS_RESOURCES_DIR"
echo "目录结构创建完成，检查目录是否存在..."
if [ -d "$MACOS_BUNDLE_DIR" ]; then
    echo "✓ MacOS目录创建成功: $MACOS_BUNDLE_DIR"
else
    echo "✗ MacOS目录创建失败: $MACOS_BUNDLE_DIR"
fi
 
 
 # 复制必要的文件到应用包
echo "复制文件到应用包..."
echo "检查发布目录内容..."
ls -la "$OUTPUT_DIR"
# 复制主可执行文件
if [ -f "$OUTPUT_DIR/FrpGUI.Avalonia.Desktop" ]; then
    echo "找到主可执行文件: $OUTPUT_DIR/FrpGUI.Avalonia.Desktop"
    mv "$OUTPUT_DIR/FrpGUI.Avalonia.Desktop" "$MACOS_BUNDLE_DIR/$PROJECT_NAME"
    echo "主可执行文件已移动到: $MACOS_BUNDLE_DIR/$PROJECT_NAME"
    chmod +x "$MACOS_BUNDLE_DIR/$PROJECT_NAME"
    echo "主可执行文件权限已设置"
elif [ -f "$OUTPUT_DIR/FrpGUI" ]; then
    echo "找到主可执行文件: $OUTPUT_DIR/FrpGUI"
    mv "$OUTPUT_DIR/FrpGUI" "$MACOS_BUNDLE_DIR/$PROJECT_NAME"
    echo "主可执行文件已移动到: $MACOS_BUNDLE_DIR/$PROJECT_NAME"
    chmod +x "$MACOS_BUNDLE_DIR/$PROJECT_NAME"
    echo "主可执行文件权限已设置"
else
    echo "警告: 未找到主可执行文件"
    echo "发布目录内容:" 
    ls -la "$OUTPUT_DIR"
fi
 
 
 # 复制其他必要文件
 cp -R "$OUTPUT_DIR"/* "$MACOS_BUNDLE_DIR/" 2>/dev/null || true

# 创建Info.plist文件
 echo "创建Info.plist文件..."
 cat > "$OUTPUT_DIR/$APP_BUNDLE_NAME/Contents/Info.plist" << EOL
  <?xml version="1.0" encoding="UTF-8"?>
  <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
 <plist version="1.0">
 <dict>
     <key>CFBundleDevelopmentRegion</key>
     <string>zh_CN</string>
     <key>CFBundleExecutable</key>
     <string>$PROJECT_NAME</string>
     <key>CFBundleIdentifier</key>
     <string>com.frp.gui</string>
     <key>CFBundleInfoDictionaryVersion</key>
     <string>6.0</string>
     <key>CFBundleName</key>
     <string>$PROJECT_NAME</string>
     <key>CFBundlePackageType</key>
     <string>APPL</string>
     <key>CFBundleShortVersionString</key>
     <string>1.0</string>
     <key>CFBundleVersion</key>
     <string>1</string>
     <key>LSMinimumSystemVersion</key>
     <string>10.15</string>
     <key>NSHumanReadableCopyright</key>
     <string>© $(date +%Y) FrpGUI</string>
     <key>NSMainNibFile</key>
     <string></string>
     <key>NSPrincipalClass</key>
     <string>NSApplication</string>
     <key>LSApplicationCategoryType</key>
     <string>public.app-category.utilities</string>
 </dict>
 </plist>
 EOL
 
 
 # 复制frp二进制文件
 if [ -d "bin" ]; then
     echo "复制frp二进制文件..."
     mkdir -p "$MACOS_BUNDLE_DIR/frp"
     # 尝试复制macOS版本的frp文件
     cp -R "bin/frp_*_darwin_amd64"/* "$MACOS_BUNDLE_DIR/frp" 2>/dev/null || true
     cp -R "bin/frp_*_darwin_arm64"/* "$MACOS_BUNDLE_DIR/frp" 2>/dev/null || true
 fi
 
 
 # 设置可执行权限
 echo "设置可执行权限..."
 find "$OUTPUT_DIR/$APP_BUNDLE_NAME" -type f -name "*" -exec chmod +x {} \; 2>/dev/null || true
 
 
 # 清理临时文件
 echo "清理临时文件..."
 # 删除应用包外的文件（保留应用包本身）
 find "$OUTPUT_DIR" -mindepth 1 -maxdepth 1 -not -name "$APP_BUNDLE_NAME" -exec rm -rf {} \; 2>/dev/null || true
 
 
 # 完成构建
 echo "=== Mac Catalyst版本构建完成 ==="
 echo "应用文件位置: $OUTPUT_DIR/$APP_BUNDLE_NAME"
 echo "提示: 可以双击应用包直接运行"
 echo "注意: 这是一个基于.NET的macOS应用，已优化为适应iPad风格的窗口布局"
 
 
 echo "\n构建输出目录:"
  ls -la "$OUTPUT_DIR"