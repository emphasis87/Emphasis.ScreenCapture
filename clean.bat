@powershell -Command "ls -Path .\ -Include bin,obj,.vs -Recurse | %% { try { Write-Host $_; Remove-Item $_ -Force -Recurse -ErrorAction SilentlyContinue } catch { } }"
