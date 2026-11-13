#
# Copyright (c) Jeninnet.
# Part of the Jeninnet Platform.
# Repository: https://github.com/TarekNajem04/Jeninnet.Validation
# Licensed under the MIT License.
#

<#
.SYNOPSIS
    Generates the Jeninnet.Validation brand icon as PNG assets.

.DESCRIPTION
    Renders a deterministic 512x512 master icon and a 128x128 NuGet package icon.
    The design is a navy rounded tile with a cyan-to-emerald validation shield and
    a bold checkmark. Keeping the renderer in source control makes the icon
    reproducible and avoids hand-edited binary assets.

    The 128x128 output is RGBA (32-bit ARGB) as required by NuGet package icons
    and by the PackagingBaselineTests architecture test.

.PARAMETER OutputDirectory
    Directory that receives the generated PNG files. Defaults to the repository
    assets folder.

.EXAMPLE
    pwsh ./scripts/assets/generate-icon.ps1

.EXAMPLE
    pwsh ./scripts/assets/generate-icon.ps1 -OutputDirectory ./artifacts/icons
#>

[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot ".." ".." "assets")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$masterSize = 512
$packageSize = 128
$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($outputPath) | Out-Null

function New-RoundedRectanglePath {
    param(
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius
    )

    $diameter = $Radius * 2
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-ShieldPath {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddLine(140, 126, 372, 126)
    $path.AddLine(372, 126, 372, 246)
    $path.AddBezier(372, 246, 372, 338, 324, 398, 256, 434)
    $path.AddBezier(256, 434, 188, 398, 140, 338, 140, 246)
    $path.CloseFigure()
    return $path
}

$bitmap = [System.Drawing.Bitmap]::new(
    $masterSize,
    $masterSize,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$graphics.Clear([System.Drawing.Color]::Transparent)

$tilePath = New-RoundedRectanglePath -X 24 -Y 24 -Width 464 -Height 464 -Radius 92
$tileBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    [System.Drawing.PointF]::new(24, 24),
    [System.Drawing.PointF]::new(488, 488),
    [System.Drawing.Color]::FromArgb(255, 7, 15, 30),
    [System.Drawing.Color]::FromArgb(255, 16, 39, 64))
$graphics.FillPath($tileBrush, $tilePath)

$graphics.SetClip($tilePath)
$gridPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(16, 125, 211, 252), 1)
for($offset = 64; $offset -lt $masterSize; $offset += 64) {
    $graphics.DrawLine($gridPen, $offset, 0, $offset, $masterSize)
    $graphics.DrawLine($gridPen, 0, $offset, $masterSize, $offset)
}
$graphics.ResetClip()

$glowPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
$glowPath.AddEllipse(72, 72, 368, 368)
$glowBrush = [System.Drawing.Drawing2D.PathGradientBrush]::new($glowPath)
$glowBrush.CenterColor = [System.Drawing.Color]::FromArgb(64, 34, 211, 238)
$glowBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 34, 211, 238))
$graphics.FillPath($glowBrush, $glowPath)

$shieldPath = New-ShieldPath
$shieldBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    [System.Drawing.PointF]::new(140, 126),
    [System.Drawing.PointF]::new(372, 434),
    [System.Drawing.Color]::FromArgb(255, 34, 211, 238),
    [System.Drawing.Color]::FromArgb(255, 52, 211, 153))
$graphics.FillPath($shieldBrush, $shieldPath)

$shieldHighlight = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    [System.Drawing.PointF]::new(140, 126),
    [System.Drawing.PointF]::new(372, 300),
    [System.Drawing.Color]::FromArgb(80, 255, 255, 255),
    [System.Drawing.Color]::FromArgb(0, 255, 255, 255))
$graphics.FillPath($shieldHighlight, $shieldPath)

$shieldPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(190, 236, 254, 255), 6)
$shieldPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$graphics.DrawPath($shieldPen, $shieldPath)

[System.Drawing.PointF[]]$checkPoints = @(
    [System.Drawing.PointF]::new(188, 262),
    [System.Drawing.PointF]::new(238, 314),
    [System.Drawing.PointF]::new(332, 206)
)
$checkGlowPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(80, 34, 211, 238), 58)
$checkGlowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$checkGlowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$checkGlowPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$graphics.DrawLines($checkGlowPen, $checkPoints)

$checkPen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, 34)
$checkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$checkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$checkPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$graphics.DrawLines($checkPen, $checkPoints)

# A small source-generator spark above the shield's right shoulder.
$sparkPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(210, 236, 254, 255), 7)
$sparkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$sparkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$graphics.DrawLine($sparkPen, 398, 92, 398, 124)
$graphics.DrawLine($sparkPen, 382, 108, 414, 108)

$borderPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(70, 125, 211, 252), 2)
$graphics.DrawPath($borderPen, $tilePath)

$masterPath = Join-Path $outputPath "jeninnet-validation-icon-master.png"
$bitmap.Save($masterPath, [System.Drawing.Imaging.ImageFormat]::Png)

$packageBitmap = [System.Drawing.Bitmap]::new(
    $packageSize,
    $packageSize,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$packageGraphics = [System.Drawing.Graphics]::FromImage($packageBitmap)
$packageGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$packageGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$packageGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$packageGraphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$packageGraphics.Clear([System.Drawing.Color]::Transparent)
$packageGraphics.DrawImage($bitmap, 0, 0, $packageSize, $packageSize)

$packagePath = Join-Path $outputPath "jeninnet-validation-icon-128.png"
$packageBitmap.Save($packagePath, [System.Drawing.Imaging.ImageFormat]::Png)

$packageGraphics.Dispose()
$packageBitmap.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
$tilePath.Dispose()
$tileBrush.Dispose()
$gridPen.Dispose()
$glowPath.Dispose()
$glowBrush.Dispose()
$shieldPath.Dispose()
$shieldBrush.Dispose()
$shieldHighlight.Dispose()
$shieldPen.Dispose()
$checkGlowPen.Dispose()
$checkPen.Dispose()
$sparkPen.Dispose()
$borderPen.Dispose()

Write-Host "Icon master : $masterPath"
Write-Host "Package icon: $packagePath"
