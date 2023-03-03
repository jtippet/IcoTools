#
# Smoke Tests
# for IcoInfo, IcoCat, IcoCut, and IcoExtract
#
param([parameter(Mandatory=$true)][string]$configuration)

Write-Host "Smoke Tests" $configuration
Write-Host

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

$IcoInfo = FindExecutable("IcoInfo")
$IcoCat = FindExecutable("IcoCat")
$IcoCut = FindExecutable("IcoCut")
$IcoExtract = FindExecutable("IcoExtract")
Write-Host

# Utility to parse and structure the output of IcoInfo
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

	$testFileEcho = $fileEcho -match "^File:.+\\([^\\\s]+)\s*$"
	if ($matches -ne $null) { $firstMatch = $matches[1] } else { $firstMatch = "" }
	if (-not $testFileEcho)
	{
		throw "IcoInfo first line file echo format mismatched"
	}
	if ($firstMatch -ne [System.IO.Path]::GetFileName($iconPath))
	{
		throw "IcoInfo file echo file name mismatched"
	}

	if ($framePairs.length % 2 -ne 0)
	{
		throw "Unexpected unmatched frame pairs"
	}

	$framePairs
}

RunIcoInfo $IcoInfo ([System.IO.Path]::Combine($PSScriptRoot, "..\tests\data\all.ico"))

