# Get the solution directory from the first argument
$SOLUTION_DIR = (Resolve-Path -Path $args[0]).Path

# Save the current working directory
$CURRENT_DIR = Get-Location

# Get the directory of the current script
$script_dir = $PSScriptRoot

# Read the GitHub token from the file
$PAT = Get-Content -Path "$script_dir\..\configs\github.token"

Write-Output "Pushing packages to GitHub NuGet repository for solution in $SOLUTION_DIR"

# Change to the solution directory
Set-Location -Path $SOLUTION_DIR

# Pack the .NET solution
& "dotnet.exe" pack

# Find and push all .nupkg files to GitHub NuGet repository
Get-ChildItem -Recurse -Filter "*.nupkg" | ForEach-Object {
    & "dotnet.exe" nuget push $_.FullName --api-key $PAT --source github --skip-duplicate
}

# Change back to the original working directory
Set-Location -Path $CURRENT_DIR
