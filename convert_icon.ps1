Add-Type -AssemblyName System.Drawing

$img = [System.Drawing.Image]::FromFile("D:\dga\DevGitAtom.WPF\Assets\logo.jpg")
$bmp = New-Object System.Drawing.Bitmap($img)
$iconHandle = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($iconHandle)

$fs = New-Object System.IO.FileStream("D:\dga\DevGitAtom.WPF\Assets\logo.ico", [System.IO.FileMode]::Create)
$icon.Save($fs)

$fs.Close()
$icon.Dispose()
$bmp.Dispose()
$img.Dispose()

Write-Host "Converted logo.jpg to logo.ico successfully!"
