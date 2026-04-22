Get-ChildItem -File | ForEach-Object {
    $lowerName = $_.Name.ToLower()

    try {
        # Step 1: temporary rename (prepend a GUID to ensure uniqueness)
        $tempName = "_tmp_" + [guid]::NewGuid().ToString() + $lowerName
        Rename-Item -Path $_.FullName -NewName $tempName -ErrorAction Stop

        # Step 2: rename to lowercase name
        Rename-Item -Path (Join-Path $_.DirectoryName $tempName) -NewName $lowerName -ErrorAction Stop

        Write-Host "Renamed: '$($_.Name)' -> '$lowerName'" -ForegroundColor Green
    }
    catch {
        Write-Host "ERROR renaming '$($_.Name)': $_" -ForegroundColor Red
    }
}