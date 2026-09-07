# Script Đóng gói Ứng dụng DevGitAtom thành file Setup.exe (1-Click Install)
# Yêu cầu: Đã cài đặt .NET SDK

$ErrorActionPreference = "Stop"
$ProjectFolder = ".\DevGitAtom.WPF"
$PublishFolder = "$ProjectFolder\bin\Release\net10.0-windows\win-x64\publish"
$ReleaseFolder = ".\Releases"

Write-Host "🚀 BƯỚC 1: Đang biên dịch (Build) dự án thành file chạy độc lập..." -ForegroundColor Cyan
dotnet publish $ProjectFolder\DevGitAtom.WPF.csproj -c Release -r win-x64 --self-contained false

Write-Host "`n🚀 BƯỚC 2: Cài đặt công cụ đóng gói Velopack (nếu chưa có)..." -ForegroundColor Cyan
try {
    vpk --version > $null 2>&1
} catch {
    Write-Host "Đang cài đặt Velopack Global Tool..." -ForegroundColor Yellow
    dotnet tool install -g vpk
}

Write-Host "`n🚀 BƯỚC 3: Đang tạo file Setup.exe..." -ForegroundColor Cyan
if (!(Test-Path $ReleaseFolder)) {
    New-Item -ItemType Directory -Path $ReleaseFolder | Out-Null
}

# Đóng gói với Velopack
vpk pack -u DevGitAtom -v 1.0.0 -p $PublishFolder -e DevGitAtom.WPF.exe -o $ReleaseFolder

Write-Host "`n✅ THÀNH CÔNG! File cài đặt đã được tạo tại thư mục:" -ForegroundColor Green
Write-Host "👉 $(Resolve-Path $ReleaseFolder)\DevGitAtom-Setup-1.0.0.exe" -ForegroundColor Green
Write-Host "Bạn chỉ cần gửi file này cho người khác. Khi họ bấm đúp vào, ứng dụng sẽ tự cài và xuất hiện ngoài Desktop!" -ForegroundColor Yellow
