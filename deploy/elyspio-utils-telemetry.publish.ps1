# Get the directory of the current script
$script_dir = $PSScriptRoot

Write-Output "Publishing Elyspio.Utils.Telemetry"

# Run the publish-dotnet.sh script with the target directory as an argument
& "$script_dir\scripts\publish-dotnet.ps1" "$script_dir\..\elyspio-utils-telemetry"
