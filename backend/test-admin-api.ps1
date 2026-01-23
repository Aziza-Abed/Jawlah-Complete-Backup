# Admin API Test Script
$baseUrl = "http://localhost:5000/api"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing Admin API Endpoints" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Login as Admin
Write-Host "`n[1] Testing Login..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin"
    password = "Admin@123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"

    if ($loginResponse.data.requiresOtp -or $loginResponse.requiresOtp) {
        $sessionToken = if ($loginResponse.data.sessionToken) { $loginResponse.data.sessionToken } else { $loginResponse.sessionToken }
        $maskedPhone = if ($loginResponse.data.maskedPhone) { $loginResponse.data.maskedPhone } else { $loginResponse.maskedPhone }

        Write-Host "OTP Required - Session: $sessionToken" -ForegroundColor Yellow
        Write-Host "Masked Phone: $maskedPhone" -ForegroundColor Yellow
        Write-Host "Check backend logs for OTP code (mock SMS)..." -ForegroundColor Yellow

        # Prompt for OTP
        $otpCode = Read-Host "Enter OTP code from backend logs"

        # Verify OTP
        $otpBody = @{
            sessionToken = $sessionToken
            otpCode = $otpCode
        } | ConvertTo-Json

        Write-Host "Verifying OTP..." -ForegroundColor Yellow
        $otpResponse = Invoke-RestMethod -Uri "$baseUrl/auth/verify-otp" -Method Post -Body $otpBody -ContentType "application/json"

        if ($otpResponse.data.success -or $otpResponse.success) {
            $token = if ($otpResponse.data.token) { $otpResponse.data.token } else { $otpResponse.token }
            Write-Host "OTP Verified - Token received" -ForegroundColor Green
        } else {
            Write-Host "OTP Verification FAILED: $($otpResponse.data.error)" -ForegroundColor Red
            exit
        }
    } else {
        $token = if ($loginResponse.data.token) { $loginResponse.data.token } else { $loginResponse.token }
        Write-Host "Login SUCCESS - Token received" -ForegroundColor Green
    }
} catch {
    Write-Host "Login FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# 2. Test Dashboard Overview
Write-Host "`n[2] Testing Dashboard Overview..." -ForegroundColor Yellow
try {
    $dashboard = Invoke-RestMethod -Uri "$baseUrl/dashboard/overview" -Method Get -Headers $headers
    Write-Host "Dashboard SUCCESS" -ForegroundColor Green
    Write-Host "  Workers Total: $($dashboard.data.workers.total)"
    Write-Host "  Workers CheckedIn: $($dashboard.data.workers.checkedIn)"
    Write-Host "  Tasks Pending: $($dashboard.data.tasks.pending)"
    Write-Host "  Issues Unresolved: $($dashboard.data.issues.unresolved)"
} catch {
    Write-Host "Dashboard FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 3. Test Worker Status
Write-Host "`n[3] Testing Worker Status..." -ForegroundColor Yellow
try {
    $workerStatus = Invoke-RestMethod -Uri "$baseUrl/dashboard/worker-status" -Method Get -Headers $headers
    Write-Host "Worker Status SUCCESS - Count: $($workerStatus.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "Worker Status FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. Test Get Users
Write-Host "`n[4] Testing Get Users..." -ForegroundColor Yellow
try {
    $users = Invoke-RestMethod -Uri "$baseUrl/users?page=1&pageSize=10" -Method Get -Headers $headers
    Write-Host "Get Users SUCCESS - Total: $($users.data.totalCount)" -ForegroundColor Green
} catch {
    Write-Host "Get Users FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 5. Test Get Users by Role
Write-Host "`n[5] Testing Get Users by Role (Workers)..." -ForegroundColor Yellow
try {
    $workers = Invoke-RestMethod -Uri "$baseUrl/users/by-role/Worker" -Method Get -Headers $headers
    Write-Host "Get Workers SUCCESS - Count: $($workers.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "Get Workers FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 6. Test Get Tasks
Write-Host "`n[6] Testing Get Tasks..." -ForegroundColor Yellow
try {
    $tasks = Invoke-RestMethod -Uri "$baseUrl/tasks?page=1&pageSize=10" -Method Get -Headers $headers
    Write-Host "Get Tasks SUCCESS - Count: $($tasks.data.items.Count)" -ForegroundColor Green
} catch {
    Write-Host "Get Tasks FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 7. Test Get Issues
Write-Host "`n[7] Testing Get Issues..." -ForegroundColor Yellow
try {
    $issues = Invoke-RestMethod -Uri "$baseUrl/issues?page=1&pageSize=10" -Method Get -Headers $headers
    Write-Host "Get Issues SUCCESS - Count: $($issues.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "Get Issues FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 8. Test Get Zones
Write-Host "`n[8] Testing Get Zones..." -ForegroundColor Yellow
try {
    $zones = Invoke-RestMethod -Uri "$baseUrl/zones" -Method Get -Headers $headers
    Write-Host "Get Zones SUCCESS - Count: $($zones.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "Get Zones FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 9. Test Audit Logs
Write-Host "`n[9] Testing Audit Logs..." -ForegroundColor Yellow
try {
    $auditLogs = Invoke-RestMethod -Uri "$baseUrl/audit?count=10" -Method Get -Headers $headers
    Write-Host "Audit Logs SUCCESS - Count: $($auditLogs.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "Audit Logs FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 10. Test Reports Summary
Write-Host "`n[10] Testing Reports Summary..." -ForegroundColor Yellow
try {
    $reports = Invoke-RestMethod -Uri "$baseUrl/reports/summary" -Method Get -Headers $headers
    Write-Host "Reports SUCCESS" -ForegroundColor Green
    Write-Host "  Total Tasks: $($reports.data.totalTasks)"
    Write-Host "  Completed Tasks: $($reports.data.completedTasks)"
} catch {
    Write-Host "Reports FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 11. Test Attendance Today
Write-Host "`n[11] Testing Attendance Records..." -ForegroundColor Yellow
try {
    $attendance = Invoke-RestMethod -Uri "$baseUrl/attendance/history" -Method Get -Headers $headers
    Write-Host "Attendance SUCCESS" -ForegroundColor Green
} catch {
    Write-Host "Attendance FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 12. Test Notifications
Write-Host "`n[12] Testing Notifications..." -ForegroundColor Yellow
try {
    $notifications = Invoke-RestMethod -Uri "$baseUrl/notifications" -Method Get -Headers $headers
    Write-Host "Notifications SUCCESS - Count: $($notifications.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "Notifications FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 13. Test Municipality Info
Write-Host "`n[13] Testing Municipality Info..." -ForegroundColor Yellow
try {
    $municipality = Invoke-RestMethod -Uri "$baseUrl/municipality" -Method Get -Headers $headers
    Write-Host "Municipality SUCCESS - Name: $($municipality.data.name)" -ForegroundColor Green
} catch {
    Write-Host "Municipality FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# 14. Test GIS Zones
Write-Host "`n[14] Testing GIS Zones..." -ForegroundColor Yellow
try {
    $gisZones = Invoke-RestMethod -Uri "$baseUrl/gis/zones" -Method Get -Headers $headers
    Write-Host "GIS Zones SUCCESS - Count: $($gisZones.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "GIS Zones FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Admin API Testing Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
