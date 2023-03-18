#
# Smoke Tests
# for IcoInfo, IcoCat, IcoCut, and IcoExtract
#
param([parameter(Mandatory=$true)][string]$configuration)

Write-Host "Smoke Tests" $configuration

#
# Setup
#

# Detect binaries
function FindExecutable($name)
{
	$path = [System.IO.Path]::Combine($PSScriptRoot, "..\src\$name\bin\$configuration")
	$file = gci -path $path -Recurse -File -include ($name + ".exe") -ErrorAction Stop
	if ($file -eq $null)
	{
		throw ("'" + $name + ".exe' not found")
	}
	elseif ($file -is [Array])
	{
		if ($file.length -eq 1)
		{
			$file = $file[0]
		}
		elseif ($file.length -lt 1)
		{
			throw ("'" + $name + ".exe' not found")
		}
		else # $file.length -gt 1
		{
			throw ("Build not clean. Multiple files '" + $name + ".exe' found.")
		}
	}
	$filepath = $file.FullName
	Write-Host "using: $filepath"
	return $filepath
}

Write-Host
$IcoInfo = FindExecutable("IcoInfo")
$IcoCat = FindExecutable("IcoCat")
$IcoCut = FindExecutable("IcoCut")
$IcoExtract = FindExecutable("IcoExtract")

$testDir = [System.IO.Path]::Combine($PSScriptRoot, "temp-" + ([System.Diagnostics.Process]::GetCurrentProcess().Id))
Write-Host "Test Directory: " $testDir
New-Item -ItemType Directory -Force -Path $testDir | Out-Null
$startLocation = Get-Location

#
# Utility functions to run tools
#

# Run IcoInfo and parse and structure the output as return object
function RunIcoInfo
{
	param([string]$IcoInfo, [string]$iconPath)

	$text = [string](& $IcoInfo -i $iconPath 2>&1)

	$info = [regex]::split($text, "\sFrame\s+#(\d)")
	if ($info.Length -lt 1)
	{
		throw "IcoInfo output seems empty"
	}

	$fileEcho, $framePairs = $info

	$found = $fileEcho -match "^File:.+\\([^\\\s]+)\s*$"
	if (-not $found)
	{
		throw "IcoInfo first line file echo format mismatched"
	}
	$firstMatch = if ($found) { $matches[1] } else { '' }
	if ($firstMatch -ne [System.IO.Path]::GetFileName($iconPath))
	{
		throw "IcoInfo file echo file name mismatched"
	}

	if ($framePairs.length % 2 -ne 0)
	{
		throw "Unexpected unmatched frame pairs"
	}

	$frames = @{}

	$idx = 0;
	while ($idx -lt $framePairs.Length)
	{
		$frameNum = [int]$framePairs[$idx++]
		$frameInfo = $framePairs[$idx++] + ' '

		$found = $frameInfo -match 'Encoding:\s+(\S+)\s'
		$encoding = if ($found) { $matches[1] } else { 'unknown' }

		$found = $frameInfo -match 'Bitmap type:\s+(\S+(\s\S+)*)\s'
		$bitmapType = if ($found) { $matches[1] } else { '' }

		$found = $frameInfo -match 'Bytes on disk:\s+(\S+)\s'
		$bytesOnDisk = if ($found) { [int]$matches[1] } else { 0 }

		$found = $frameInfo -match 'Width:\s+(\d+)\s+(\d+)\s'
		$width = if ($found) { [int]$matches[1] } else { 0 }
		$widthFromHeader = if ($found) { [int]$matches[2] } else { 0 }

		$found = $frameInfo -match 'Height:\s+(\d+)\s+(\d+)\s'
		$height = if ($found) { [int]$matches[1] } else { 0 }
		$heightFromHeader = if ($found) { [int]$matches[2] } else { 0 }

		$found = $frameInfo -match 'Bit depth:\s+(\d+)\s+(\d+)\s'
		$bitDepth = if ($found) { [int]$matches[1] } else { 0 }
		$bitDepthFromHeader = if ($found) { [int]$matches[2] } else { 0 }

		$frames[$frameNum] = [PSCustomObject]@{
			encoding = $encoding
			bitmapType = $bitmapType
			bytesOnDisk = $bytesOnDisk
			size = [PSCustomObject]@{
				width = $width
				height = $height
				bitDepth = $bitDepth
			}
			sizeFromHeader = [PSCustomObject]@{
				width = $widthFromHeader
				height = $heightFromHeader
				bitDepth = $bitDepthFromHeader
			}
		}
	}

	$frames
}

function RunIcoExtract()
{
	param([string]$IcoExtract, [string]$iconPath)
	& $IcoExtract -i $iconPath 2>&1
}

function RunIcoCat()
{
	param([string]$IcoCat, [string]$iconPath, [string]$imagePath, [string]$encoding)
	& $IcoCat -i $iconPath -s $imagePath -e $encoding 2>&1
}

function RunIcoCutSize()
{
	param([string]$IcoCut, [string]$iconPath, [int]$size)
	& $IcoCut -i $iconPath -s $size 2>&1
}

function RunIcoCutFrame()
{
	param([string]$IcoCut, [string]$iconPath, [int]$frameNumber)
	& $IcoCut -i $iconPath -d $frameNumber 2>&1
}

function MatchFrame()
{
	param(
		$frame,
		[parameter(Mandatory=$true)]$needle
	)

	if ($frame -eq $null) { return $false }
	if (-not $frame -is [PSCustomObject]) { throw "Unexpected type for 'frame'" }

	foreach ($n in $needle.GetEnumerator())
	{
		$val = $frame.($n.Name)
		if ($val -eq $null) { $val = $frame.size.($n.Name) }
		if ($val -eq $null) { $val = $frame.sizeFromHeader.($n.Name) }

		if ($val -eq $null) { return $false }

		if ($n.Value -ne $val) { return $false }
	}

	return $true
}

function FindFrame()
{
	param(
		[parameter(Mandatory=$true)][Hashtable]$frames,
		[parameter(Mandatory=$true)]$needle
	)

	foreach ($f in $frames.GetEnumerator())
	{
		if (MatchFrame $f.value $needle)
		{
			$o = $f.value.psobject.copy()
			$o | Add-Member -MemberType NoteProperty -Name "Num" -Value $f.key
			return $o
		}
	}

	return $null
}

function ClearTestDir()
{
	param([string]$path)
	Get-ChildItem -Path $path -Include *.* -File -Recurse | foreach { $_.Delete()}
}

function MatchImage()
{
	param(
		[System.Drawing.Bitmap]$image,
		[int]$size,
		[int]$r,
		[int]$g,
		[int]$b
	)
	$col = $image.GetPixel(0, 0)
	(($image.Width -eq $size) -and ($col.R -eq $r) -and ($col.G -eq $g) -and ($col.B -eq $b))
}

function FindImage()
{
	param(
		[array]$images,
		[int]$size
	)
	return $images | where width -eq $size
}


#
# Utility functions to test
#
$hasError = $false

function Assert()
{
	param(
		[parameter(Mandatory=$true)][bool]$condition,
		[parameter(Mandatory=$true)][string]$message,
		[string]$details = "",
		[bool]$critical = $true
	)

	if ($condition)
	{
		Write-Host " ✅ $message"
	}
	else
	{
		Write-Host " ❌ $details" -NoNewLine
		$hasError = $true
		if ($critical)
		{
			throw "$message"
		}
		else
		{
			Write-Error "$message"
		}
	}
}

function AreEqual()
{
	param(
		[parameter(Mandatory=$true)]$expected,
		[parameter(Mandatory=$true)]$actual,
		[parameter(Mandatory=$true)][string]$message,
		[bool]$critical = $true
	)

	Assert ($expected -eq $actual) $message "[Expected: $expected; Actual: $actual] " $critical
}

try
{

#
# Tests
#

Set-Location $testDir

$all_ext_ico = ([System.IO.Path]::Combine($PSScriptRoot, "..\tests\data\all_ext.ico"))
$x32_ext_ico = ([System.IO.Path]::Combine($PSScriptRoot, "..\tests\data\x32_ext.ico"))

#
# Test Group
Write-Host
Write-Host "Test Group: IcoInfo Fundamentals"

$f = RunIcoInfo $IcoInfo $all_ext_ico
Assert ($f -is [Hashtable]) "RunIcoInfo returns the expected type"
AreEqual 6 $f.Count "Info(all_ext.ico) shows 6 frames"
Assert (MatchFrame (FindFrame $f @{ width=16 }) @{ encoding="Bitmap"; height=16; bitDepth=32 }) "Has x16 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=24 }) @{ encoding="Bitmap"; height=24; bitDepth=32 }) "Has x24 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=48 }) @{ encoding="Bitmap"; height=48; bitDepth=32 }) "Has x48 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=64 }) @{ encoding="Bitmap"; height=64; bitDepth=32 }) "Has x64 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 png frame"

$f = RunIcoInfo $IcoInfo $x32_ext_ico
AreEqual 1 $f.Count "Info(x32_ext.ico) shows 1 frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"

#
# Test Group
Write-Host
Write-Host "Test Group: IcoExtract"

ClearTestDir $testDir
AreEqual 0 (Get-ChildItem -Path $testDir -Include *.* -File -Recurse).Length "Test directory is empty"

copy $all_ext_ico "i1.ico"
RunIcoExtract $IcoExtract "i1.ico"
$files = Get-ChildItem -Path $testDir -Include *.png -File -Recurse
if ($files -isnot [Array]) { $files = @( $files ) }
AreEqual 6 $files.Length "Six png frames extracted"

$images = $files | foreach { new-object System.Drawing.Bitmap($_.FullName) }
Assert (MatchImage (FindImage $images 16) 16 255 0 0) "Extracted x16 image with right size and color"
Assert (MatchImage (FindImage $images 24) 24 0 255 0) "Extracted x24 image with right size and color"
Assert (MatchImage (FindImage $images 32) 32 255 255 0) "Extracted x32 image with right size and color"
Assert (MatchImage (FindImage $images 48) 48 0 0 255) "Extracted x48 image with right size and color"
Assert (MatchImage (FindImage $images 64) 64 255 0 255) "Extracted x64 image with right size and color"
Assert (MatchImage (FindImage $images 256) 256 0 255 255) "Extracted x256 image with right size and color"
$images = $null
[System.GC]::Collect()

ClearTestDir $testDir
AreEqual 0 (Get-ChildItem -Path $testDir -Include *.* -File -Recurse).Length "Test directory is empty"

copy $x32_ext_ico "i2.ico"
RunIcoExtract $IcoExtract "i2.ico"
$files = Get-ChildItem -Path $testDir -Include *.png -File -Recurse
if ($files -isnot [Array]) { $files = @( $files ) }
AreEqual 1 $files.Length "One png frame extracted"

$images = $files | foreach { new-object System.Drawing.Bitmap($_.FullName) }
Assert (MatchImage (FindImage $images 32) 32 255 255 0) "Extracted x32 image with right size and color"
$images = $null
[System.GC]::Collect()

ClearTestDir $testDir
AreEqual 0 (Get-ChildItem -Path $testDir -Include *.* -File -Recurse).Length "Test directory is empty"

#
# Test Group
Write-Host
Write-Host "Test Group: IcoCat"

ClearTestDir $testDir
AreEqual 0 (Get-ChildItem -Path $testDir -Include *.* -File -Recurse).Length "Test directory is empty"

RunIcoCat $IcoCat "i3.ico" "../Data/x256.png" "PNG"
Assert (Test-Path "i3.ico") "Icon file generated"
$f = RunIcoInfo $IcoInfo "i3.ico"
AreEqual 1 $f.Count "Info(i3.ico) shows 1 frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 PNG frame"

RunIcoCat $IcoCat "i3.ico" "../Data/x32.png" "Bitmap"
$f = RunIcoInfo $IcoInfo "i3.ico"
AreEqual 2 $f.Count "Info(i3.ico) shows 2 frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 PNG frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"

RunIcoCat $IcoCat "i3.ico" "../Data/x64.png" "Bitmap"
$f = RunIcoInfo $IcoInfo "i3.ico"
AreEqual 3 $f.Count "Info(i3.ico) shows 3 frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 PNG frame"
Assert (MatchFrame (FindFrame $f @{ width=64 }) @{ encoding="Bitmap"; height=64; bitDepth=32 }) "Has x64 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"

copy $x32_ext_ico "i4.ico"
$f = RunIcoInfo $IcoInfo "i4.ico"
AreEqual 1 $f.Count "Info(i4.ico) shows 1 frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"

RunIcoCat $IcoCat "i4.ico" "../Data/x48.png" "Bitmap"
$f = RunIcoInfo $IcoInfo "i4.ico"
AreEqual 2 $f.Count "Info(i4.ico) shows 2 frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=48 }) @{ encoding="Bitmap"; height=48; bitDepth=32 }) "Has x48 bmp frame"

ClearTestDir $testDir
AreEqual 0 (Get-ChildItem -Path $testDir -Include *.* -File -Recurse).Length "Test directory is empty"

#
# Test Group
Write-Host
Write-Host "Test Group: IcoCut"

ClearTestDir $testDir
AreEqual 0 (Get-ChildItem -Path $testDir -Include *.* -File -Recurse).Length "Test directory is empty"

copy $all_ext_ico "i5.ico"
$f = RunIcoInfo $IcoInfo "i5.ico"
AreEqual 6 $f.Count "Info(i5.ico) shows 6 frames"
Assert (MatchFrame (FindFrame $f @{ width=16 }) @{ encoding="Bitmap"; height=16; bitDepth=32 }) "Has x16 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=24 }) @{ encoding="Bitmap"; height=24; bitDepth=32 }) "Has x24 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=48 }) @{ encoding="Bitmap"; height=48; bitDepth=32 }) "Has x48 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=64 }) @{ encoding="Bitmap"; height=64; bitDepth=32 }) "Has x64 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 png frame"

$frameNumber = (FindFrame $f @{ width=64 }).Num
RunIcoCutFrame $IcoCut "i5.ico" $frameNumber
$f = RunIcoInfo $IcoInfo "i5.ico"
AreEqual 5 $f.Count "Info(i5.ico) shows 5 frames"
Assert (MatchFrame (FindFrame $f @{ width=16 }) @{ encoding="Bitmap"; height=16; bitDepth=32 }) "Has x16 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=24 }) @{ encoding="Bitmap"; height=24; bitDepth=32 }) "Has x24 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=48 }) @{ encoding="Bitmap"; height=48; bitDepth=32 }) "Has x48 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 png frame"

RunIcoCutSize $IcoCut "i5.ico" 32
$f = RunIcoInfo $IcoInfo "i5.ico"
AreEqual 4 $f.Count "Info(i5.ico) shows 4 frames"
Assert (MatchFrame (FindFrame $f @{ width=16 }) @{ encoding="Bitmap"; height=16; bitDepth=32 }) "Has x16 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=24 }) @{ encoding="Bitmap"; height=24; bitDepth=32 }) "Has x24 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=48 }) @{ encoding="Bitmap"; height=48; bitDepth=32 }) "Has x48 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 png frame"

ClearTestDir $testDir
AreEqual 0 (Get-ChildItem -Path $testDir -Include *.* -File -Recurse).Length "Test directory is empty"

#
# Test Group
Write-Host
Write-Host "Test Group: Mixed IcoCat & IcoCut"

ClearTestDir $testDir
AreEqual 0 (Get-ChildItem -Path $testDir -Include *.* -File -Recurse).Length "Test directory is empty"

copy $x32_ext_ico "i6.ico"
$f = RunIcoInfo $IcoInfo "i6.ico"
AreEqual 1 $f.Count "Info(i6.ico) shows 1 frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"

RunIcoCat $IcoCat "i6.ico" "../Data/x256.png" "PNG"
$f = RunIcoInfo $IcoInfo "i6.ico"
AreEqual 2 $f.Count "Info(i6.ico) shows 2 frames"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 PNG frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"

RunIcoCat $IcoCat "i6.ico" "../Data/x64.png" "Bitmap"
$f = RunIcoInfo $IcoInfo "i6.ico"
AreEqual 3 $f.Count "Info(i6.ico) shows 3 frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 PNG frame"
Assert (MatchFrame (FindFrame $f @{ width=64 }) @{ encoding="Bitmap"; height=64; bitDepth=32 }) "Has x64 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"

RunIcoCutSize $IcoCut "i6.ico" 32
$f = RunIcoInfo $IcoInfo "i6.ico"
AreEqual 2 $f.Count "Info(i6.ico) shows 2 frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 PNG frame"
Assert (MatchFrame (FindFrame $f @{ width=64 }) @{ encoding="Bitmap"; height=64; bitDepth=32 }) "Has x64 bmp frame"

RunIcoCat $IcoCat "i6.ico" "../Data/x16.png" "Bitmap"
$f = RunIcoInfo $IcoInfo "i6.ico"
AreEqual 3 $f.Count "Info(i6.ico) shows 3 frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 PNG frame"
Assert (MatchFrame (FindFrame $f @{ width=64 }) @{ encoding="Bitmap"; height=64; bitDepth=32 }) "Has x64 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=16 }) @{ encoding="Bitmap"; height=16; bitDepth=32 }) "Has x16 bmp frame"


ClearTestDir $testDir
AreEqual 0 (Get-ChildItem -Path $testDir -Include *.* -File -Recurse).Length "Test directory is empty"


#
# Script end
#

Write-Host
Assert $true "All tests completed"
Write-Host "Done."

}
finally
{
	$images = $null
	[System.GC]::Collect()

	Set-Location $startLocation
	if (Test-Path $testDir)
	{
		Remove-Item $testDir -Recurse -Force
	}
}
