#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

GENERATOR_PROJECT="$PROJECT_ROOT/PolyBridge.Generator/PolyBridge.Generator.csproj"
OUTPUT_DLL="$PROJECT_ROOT/build/PolyBridge.Generator/bin/Release/netstandard2.0/PolyBridge.Generator.dll"
TARGET_DIR="$PROJECT_ROOT/PolyBridge.Core/Plugins"

echo "=== PolyBridge Generator Build ==="

# Build
echo "[1/2] Building PolyBridge.Generator (Release)..."
dotnet build "$GENERATOR_PROJECT" -c Release -v quiet

if [ $? -ne 0 ]; then
    echo "ERROR: Build failed."
    exit 1
fi

if [ ! -f "$OUTPUT_DLL" ]; then
    echo "ERROR: Output DLL not found at $OUTPUT_DLL"
    exit 1
fi

# Copy
echo "[2/2] Copying DLL to $TARGET_DIR..."
mkdir -p "$TARGET_DIR"
cp "$OUTPUT_DLL" "$TARGET_DIR/PolyBridge.Generator.dll"

echo ""
echo "=== Done ==="
echo "  Output: $TARGET_DIR/PolyBridge.Generator.dll"
echo ""
echo "  In Unity Inspector, select the DLL and:"
echo "    1. Add label: RoslynAnalyzer"
echo "    2. Uncheck all platforms"
