$files = @(
    "d:\VisionaryCoder\Internal.Snippet\docs\design-patterns\command.md",
    "d:\VisionaryCoder\Internal.Snippet\docs\design-patterns\singleton.md"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        # Fix private field underscore prefixes
        $content = $content -replace 'private readonly ([^_]+) _(\w+)', 'private readonly $1 $2'
        $content = $content -replace 'private ([^_]+) _(\w+)', 'private $1 $2'
        # Fix references to those fields
        $content = $content -replace '(?<=\W)_(\w+)(?=\W)', '$1'
        Set-Content $file $content -NoNewline
        Write-Host "Fixed $file"
    }
}
