param([string]$Model = "qwen3.5:4b")
$ErrorActionPreference = "Stop"
if (-not (Get-Command ollama -ErrorAction SilentlyContinue)) { throw "Cài Ollama cho Windows tại https://ollama.com/download rồi chạy lại script." }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "Cài .NET 10 SDK rồi chạy lại script." }
Write-Host "Phiên bản SDK và Ollama:"
dotnet --version
ollama --version
try { $null = Invoke-RestMethod http://localhost:11434/api/tags -TimeoutSec 3 }
catch { throw "Hãy mở ứng dụng Ollama hoặc chạy 'ollama serve' trong cửa sổ riêng." }
ollama pull $Model
if ($LASTEXITCODE -ne 0) { throw "Không tải được model. Kiểm tra mạng và phiên bản Ollama." }
$payload = @{ model = $Model; messages = @(); stream = $false; think = $false; keep_alive = "30m"; options = @{ num_ctx = 4096 } } | ConvertTo-Json -Depth 5
$null = Invoke-RestMethod http://localhost:11434/api/chat -Method Post -Body ([Text.Encoding]::UTF8.GetBytes($payload)) -ContentType "application/json; charset=utf-8" -TimeoutSec 120
ollama ps
Write-Host "Model đã nạp. Tiếp theo chạy Update-Database trong Visual Studio và mở dự án."
