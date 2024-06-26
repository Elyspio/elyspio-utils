# Get the directory of the current script
$script_dir = $PSScriptRoot

Write-Output "Publishing @elyspio/vite-eslint-config"

# Save the current working directory
$current_wd = Get-Location

# Change to the target directory
Set-Location -Path "$script_dir\..\vite-eslint-config"

# Run npm pack
npm run publish

# Change back to the original working directory
Set-Location -Path $current_wd
