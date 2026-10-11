param([string]$Model = "qwen3.5:4b", [int]$Samples = 5, [string]$BaseUrl = "http://localhost:11434")
$ErrorActionPreference = "Stop"
if ($Samples -lt 1 -or $Samples -gt 30) { throw "Samples phải từ 1 đến 30." }
$payload = @{
    model = $Model; stream = $false; think = $false; keep_alive = "30m"
    options = @{ num_ctx = 4096; num_predict = 320; temperature = 0.2; presence_penalty = 0 }
    messages = @(
        @{ role = "system"; content = "Bạn là trợ lý du lịch. Trả lời tiếng Việt trong tối đa 5 câu, chỉ dùng dữ liệu được cung cấp." },
        @{ role = "user"; content = "So sánh 2 tour mẫu: A 3 ngày giá 4 triệu khởi hành 20/12/2026, B 4 ngày giá 5 triệu khởi hành 22/12/2026. Ngân sách 5 triệu mỗi người. Gợi ý chọn tour; không tự tạo thông tin." }
    )
} | ConvertTo-Json -Depth 8
$body = [System.Text.Encoding]::UTF8.GetBytes($payload)
Write-Host "Đo model local: $Model. Lượt đầu có thể phải nạp model."
$rows = @()
for ($i = 0; $i -le $Samples; $i++) {
    $clock = [System.Diagnostics.Stopwatch]::StartNew()
    $result = Invoke-RestMethod -Uri "$BaseUrl/api/chat" -Method Post -Body $body -ContentType "application/json; charset=utf-8" -TimeoutSec 120
    $clock.Stop()
    $row = [pscustomobject]@{
        Run = $i; Type = $(if ($i -eq 0) { "ColdOrWarmup" } else { "Warm" })
        Seconds = [math]::Round($clock.Elapsed.TotalSeconds, 2)
        LoadSeconds = [math]::Round($result.load_duration / 1e9, 2)
        PromptTokens = $result.prompt_eval_count; OutputTokens = $result.eval_count
        TokensPerSecond = $(if ($result.eval_duration -gt 0) { [math]::Round($result.eval_count / ($result.eval_duration / 1e9), 2) } else { 0 })
        Completed = $result.done
    }
    $rows += $row; $row | Format-Table -AutoSize
}
$warm = @($rows | Where-Object { $_.Type -eq "Warm" } | Sort-Object Seconds)
$p95 = $warm[[math]::Ceiling($warm.Count * .95) - 1].Seconds
$rows | Export-Csv -Path (Join-Path $PSScriptRoot "ollama-benchmark.csv") -NoTypeInformation -Encoding UTF8
Write-Host "P95 model (warm): $p95 giây. Đây là phép đo model với prompt mẫu; chưa phải độ trễ cả website."
Write-Host "Nếu chậm: kiểm tra ollama ps (GPU/CPU), đóng ứng dụng chiếm VRAM, thử qwen3.5:2b và đổi AI:Model."
