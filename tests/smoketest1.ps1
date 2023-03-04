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
			return $f.value
		}
	}

	return $null
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


#
# Tests
#

Write-Host
Write-Host "Test Group: IcoInfo Fundamentals"

$f = RunIcoInfo $IcoInfo ([System.IO.Path]::Combine($PSScriptRoot, "..\tests\data\all_ext.ico"))
Assert ($f -is [Hashtable]) "RunIcoInfo returns the expected type"
AreEqual 6 $f.Count "Info(all_ext.ico) shows 6 frames"
Assert (MatchFrame (FindFrame $f @{ width=16 }) @{ encoding="Bitmap"; height=16; bitDepth=32 }) "Has x16 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=24 }) @{ encoding="Bitmap"; height=24; bitDepth=32 }) "Has x24 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=48 }) @{ encoding="Bitmap"; height=48; bitDepth=32 }) "Has x48 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=64 }) @{ encoding="Bitmap"; height=64; bitDepth=32 }) "Has x64 bmp frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 png frame"

$f = RunIcoInfo $IcoInfo ([System.IO.Path]::Combine($PSScriptRoot, "..\tests\data\x32_ext.ico"))
AreEqual 1 $f.Count "Info(x32_ext.ico) shows 1 frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="Bitmap"; height=32; bitDepth=32 }) "Has x32 bmp frame"

$f = RunIcoInfo $IcoInfo ([System.IO.Path]::Combine($PSScriptRoot, "..\tests\data\all_png.ico"))
AreEqual 6 $f.Count "Info(all_png.ico) shows 6 frames"
Assert (MatchFrame (FindFrame $f @{ width=16 }) @{ encoding="PNG"; height=16; bitDepth=32 }) "Has x16 png frame"
Assert (MatchFrame (FindFrame $f @{ width=24 }) @{ encoding="PNG"; height=24; bitDepth=32 }) "Has x24 png frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="PNG"; height=32; bitDepth=32 }) "Has x32 png frame"
Assert (MatchFrame (FindFrame $f @{ width=48 }) @{ encoding="PNG"; height=48; bitDepth=32 }) "Has x48 png frame"
Assert (MatchFrame (FindFrame $f @{ width=64 }) @{ encoding="PNG"; height=64; bitDepth=32 }) "Has x64 png frame"
Assert (MatchFrame (FindFrame $f @{ width=256 }) @{ encoding="PNG"; height=256; bitDepth=32 }) "Has x256 png frame"

$f = RunIcoInfo $IcoInfo ([System.IO.Path]::Combine($PSScriptRoot, "..\tests\data\x32_png.ico"))
AreEqual 1 $f.Count "Info(x32_png.ico) shows 1 frame"
Assert (MatchFrame (FindFrame $f @{ width=32 }) @{ encoding="PNG"; height=32; bitDepth=32 }) "Has x32 png frame"


#
# Script end
#

Write-Host
Write-Host "Done."
