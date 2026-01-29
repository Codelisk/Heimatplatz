# Resize Uno Desktop App to Mobile Size
# Usage: .\resize-to-mobile.ps1 [-Size <phone|tablet>] [-Width <int>] [-Height <int>]

param(
    [ValidateSet("iphone-se", "iphone-14", "iphone-14-pro-max", "pixel", "samsung", "tablet")]
    [string]$Size = "iphone-14",
    [int]$Width = 0,
    [int]$Height = 0
)

# Predefined sizes (width x height)
$sizes = @{
    "iphone-se"        = @{ Width = 375;  Height = 667 }
    "iphone-14"        = @{ Width = 390;  Height = 844 }
    "iphone-14-pro-max"= @{ Width = 430;  Height = 932 }
    "pixel"            = @{ Width = 412;  Height = 915 }
    "samsung"          = @{ Width = 360;  Height = 800 }
    "tablet"           = @{ Width = 768;  Height = 1024 }
}

# Use custom size if provided, otherwise use preset
if ($Width -gt 0 -and $Height -gt 0) {
    $targetWidth = $Width
    $targetHeight = $Height
} else {
    $targetWidth = $sizes[$Size].Width
    $targetHeight = $sizes[$Size].Height
}

Write-Host "Resizing to $Size size: ${targetWidth}x${targetHeight}" -ForegroundColor Cyan

# Add Windows API for window manipulation
Add-Type @"
using System;
using System.Runtime.InteropServices;

public class WindowHelper {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
"@

# Find the Heimatplatz.App process
$process = Get-Process -Name "Heimatplatz.App" -ErrorAction SilentlyContinue

if (-not $process) {
    Write-Host "Heimatplatz.App is not running. Please start the app first." -ForegroundColor Red
    exit 1
}

$hwnd = $process.MainWindowHandle

if ($hwnd -eq [IntPtr]::Zero) {
    Write-Host "Could not find main window handle." -ForegroundColor Red
    exit 1
}

# Get current window position
$rect = New-Object WindowHelper+RECT
[WindowHelper]::GetWindowRect($hwnd, [ref]$rect) | Out-Null

$currentX = $rect.Left
$currentY = $rect.Top

# Move and resize the window
$result = [WindowHelper]::MoveWindow($hwnd, $currentX, $currentY, $targetWidth, $targetHeight, $true)

if ($result) {
    Write-Host "Window resized successfully!" -ForegroundColor Green
    [WindowHelper]::SetForegroundWindow($hwnd) | Out-Null
} else {
    Write-Host "Failed to resize window." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Available presets:" -ForegroundColor Yellow
Write-Host "  -Size iphone-se         (375x667)"
Write-Host "  -Size iphone-14         (390x844) [default]"
Write-Host "  -Size iphone-14-pro-max (430x932)"
Write-Host "  -Size pixel             (412x915)"
Write-Host "  -Size samsung           (360x800)"
Write-Host "  -Size tablet            (768x1024)"
Write-Host ""
Write-Host "Custom size: -Width 400 -Height 800"
