# Replace content in files
rg 'ImageResizer' -l | ForEach-Object { 
    (Get-Content $_) -replace 'ImageResizer', 'ImageResizer' | Set-Content $_ 
}

# Rename files that contain 'ImageResizer' in their name
Get-ChildItem -Recurse -Filter '*ImageResizer_v3*' | ForEach-Object {
    $newName = $_.Name -replace 'ImageResizer_v3', 'ImageResizer'
    
    # Check if the new filename already exists, and add a suffix if needed
    $counter = 1
    $newPath = $_.DirectoryName + "\" + $newName
    while (Test-Path -Path $newPath) {
        $newPath = $_.DirectoryName + "\" + ($newName -replace '(\.\w+)$', "_$counter$1")
        $counter++
    }

    Rename-Item -Path $_.FullName -NewName (Split-Path -Leaf $newPath)
}

