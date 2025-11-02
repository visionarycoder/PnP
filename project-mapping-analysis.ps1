# Project mapping analysis for C# snippets
$markdownFiles = Get-ChildItem "docs\csharp\" -Name "*.md" | Where-Object { $_ -ne "readme.md" } | Sort-Object
$sourceProjects = Get-ChildItem "src\" -Directory -Name | Where-Object { $_ -like "CSharp.*" } | Sort-Object

Write-Host "=== MARKDOWN TO PROJECT MAPPING ===" -ForegroundColor Yellow

$missing = @()
foreach ($md in $markdownFiles) {
    $baseName = $md -replace '\.md$', ''
    # Convert kebab-case to PascalCase - properly join without spaces
    $parts = $baseName -split '-' | ForEach-Object { 
        $_.Substring(0,1).ToUpper() + $_.Substring(1)
    }
    $expectedProject = "CSharp." + ($parts -join '')
    
    $hasProject = $sourceProjects -contains $expectedProject
    
    if ($hasProject) {
        Write-Host "✓ $md -> $expectedProject" -ForegroundColor Green
    } else {
        Write-Host "✗ $md -> $expectedProject (MISSING)" -ForegroundColor Red
        $missing += $expectedProject
    }
}

Write-Host "`n=== SUMMARY ===" -ForegroundColor Magenta
Write-Host "Total markdown files: $($markdownFiles.Count)"
Write-Host "Total source projects: $($sourceProjects.Count)" 
Write-Host "Missing projects: $($missing.Count)"

if ($missing.Count -gt 0) {
    Write-Host "`nMissing project implementations:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  $_" }
}

# Check for orphaned projects (projects without markdown)
Write-Host "`n=== ORPHANED PROJECTS CHECK ===" -ForegroundColor Cyan
$expectedFromMarkdown = $markdownFiles | ForEach-Object {
    $baseName = $_ -replace '\.md$', ''
    $parts = $baseName -split '-' | ForEach-Object { 
        $_.Substring(0,1).ToUpper() + $_.Substring(1)
    }
    "CSharp." + ($parts -join '')
}

$orphaned = $sourceProjects | Where-Object { $_ -notin $expectedFromMarkdown }
if ($orphaned.Count -gt 0) {
    Write-Host "Projects without corresponding markdown:" -ForegroundColor Yellow
    $orphaned | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host "No orphaned projects found." -ForegroundColor Green
}