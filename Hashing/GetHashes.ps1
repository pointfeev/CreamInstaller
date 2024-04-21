$Array32 = [System.Text.StringBuilder]::new().AppendLine('[')
$Array64 = [System.Text.StringBuilder]::new().AppendLine('[')
function Write-Hash([System.IO.FileInfo] $File, [string] $Version) {
    $Hash = (Get-FileHash $File -Algorithm MD5).Hash
    $Value = "`t`"$Hash`", // CreamAPI $Version"
    if ($File.Name.Contains('64')) {
        $Array64.AppendLine($Value) | Out-Null
    } else {
        $Array32.AppendLine($Value) | Out-Null
    }
}
Get-ChildItem | ForEach-Object {
    if ($_.GetType().Name -eq 'DirectoryInfo') {
        $VersionIndex = $_.Name.IndexOf('v')
        if ($VersionIndex -eq -1) { Return }
        $Release = $_.Name.Substring($VersionIndex).Replace('_', ' ')
        Get-ChildItem $_ | ForEach-Object {
            if ($_.GetType().Name -eq 'DirectoryInfo') {
                $Build = $_.Name -eq 'log_build' ? 'Log build' : 'Non-log build'
                Get-ChildItem $_ | ForEach-Object {
                    if ($_.GetType().Name -eq 'DirectoryInfo') {
                        Get-ChildItem $_ | ForEach-Object {
                            if ($_.Extension -eq '.dll') {
                                Write-Hash $_ "$Release $Build"
                            }
                        }
                    } elseif ($_.Extension -eq '.dll') {
                        Write-Hash $_ "$Release $Build"
                    }
                }
            }
            elseif ($_.Extension -eq '.dll') {
                Write-Hash $_ "$Release"
            }
        }
    }
}
$Array32.Append(']') | Out-Null
$Array64.Append(']') | Out-Null
Write-Host "32-bit: $($Array32.ToString())"
Write-Host "64-bit: $($Array64.ToString())"
Write-Host 'Press enter to exit . . . ' -NoNewline
$Host.UI.ReadLine() | Out-Null