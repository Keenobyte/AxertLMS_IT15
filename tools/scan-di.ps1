# PowerShell script: scans project for ctor-injected types and checks Program.cs service registrations.
param(
    [string]$ProjectRoot = ".",
    [string]$ProgramFile = "Program.cs"
)

$excludeDirs = @("bin","obj",".git")
$csFiles = Get-ChildItem -Path $ProjectRoot -Recurse -Include *.cs |
    Where-Object { $excludeDirs -notcontains $_.Directory.Name } |
    Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" }

# read Program.cs registrations
$programText = Get-Content -Path (Join-Path $ProjectRoot $ProgramFile) -Raw

$issues = @()

foreach ($f in $csFiles) {
    $text = Get-Content $f.FullName -Raw
    # find classes that look like Controllers or PageModels
    if ($text -match "class\s+([A-Za-z0-9_]+)(\s*:\s*[A-Za-z0-9_<>,\s]+)?") {
        $className = $matches[1]
        # look for constructor: public ClassName(...)
        $ctorPattern = "public\s+$className\s*\(([^)]*)\)"
        if ($text -match $ctorPattern) {
            $paramList = $matches[1].Trim()
            if ($paramList.Length -gt 0) {
                $params = $paramList -split "," | ForEach-Object {
                    $_.Trim() -replace "\s+=[^,]+$",""  # drop default values
                }
                foreach ($p in $params) {
                    # extract the type token (handles generics)
                    # e.g. IService<My.Type> name -> IService<My.Type>
                    $typeToken = ($p -replace "^\s*","") -replace "\s+[A-Za-z0-9_]+$",""
                    $typeToken = $typeToken.Trim()
                    if ($typeToken) {
                        # quick check in Program.cs: generics and full names
                        $typeSimple = $typeToken -replace "<.*>",""
                        $regFound = $false
                        $searchPatterns = @(
                            "AddDbContext<\s*$typeToken\s*>",
                            "AddDbContext<\s*$typeSimple\s*>",
                            "AddScoped<\s*$typeToken\s*>",
                            "AddSingleton<\s*$typeToken\s*>",
                            "AddTransient<\s*$typeToken\s*>",
                            "AddScoped\(\s*typeof\(\s*$typeSimple\s*\)\s*\)",
                            "AddSingleton\(\s*typeof\(\s*$typeSimple\s*\)\s*\)",
                            "AddTransient\(\s*typeof\(\s*$typeSimple\s*\)\s*\)"
                        )
                        foreach ($pat in $searchPatterns) {
                            if ($programText -match [regex]::Escape($pat) -or ($programText -match $pat)) {
                                $regFound = $true
                                break
                            }
                        }
                        if (-not $regFound) {
                            $issues += [pscustomobject]@{
                                File = $f.FullName
                                Class = $className
                                ConstructorParam = $typeToken
                            }
                        }
                    }
                }
            }
        }
    }
}

if ($issues.Count -eq 0) {
    Write-Host "No obvious DI registration gaps found by the quick scanner."
} else {
    Write-Host "Potential unregistered constructor-injected types:"
    $issues | Format-Table -AutoSize
    Write-Host "`nReview Program.cs and register the missing services (AddDbContext/AddScoped/AddTransient/AddSingleton)."
}