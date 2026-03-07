# FollowUp Integration Test Plan — Manual Testing

> **Purpose**: Step-by-step checklist for manually testing the integration
> between Backend (.NET API), Web (React), and Mobile (Flutter).
>
> **Prerequisites**: All three apps running simultaneously.

---

## Setup Checklist

- [ ] Backend running at `http://localhost:5000` (SQL Server connected)
- [ ] Web running at `http://localhost:5173` (Vite dev server)
- [ ] Mobile running on emulator/device (pointing to backend IP)
- [ ] Database seeded with test data (users, zones, etc.)
- [ ] DeveloperMode enabled in `appsettings.json`:
  - `DisableGeofencing: true`
  - `DisableDeviceBinding: true`

### Test Accounts (matching AlBirehSeedData.sql)

| Role       | Username  | Password   |
|------------|-----------|------------|
| Admin      | admin     | pass123@   |
| Supervisor | super1    | pass123@   |
| Worker     | worker1   | pass123@   |

---

## Flow 1: Authentication

### 1.1 Web Login
- [ ] Open web at `http://localhost:5173/login`
- [ ] Login as **admin** → should redirect to `/dashboard`
- [ ] Verify top bar shows admin name/role
- [ ] Open DevTools → Application → localStorage → confirm `followup_token` exists
- [ ] Logout → confirm redirect to login, localStorage cleared

### 1.2 Mobile Login
- [ ] Open mobile app → login screen
- [ ] Login as **worker001** with password
- [ ] If OTP required → enter code from SMS
- [ ] Verify home screen loads with worker's tasks
- [ ] Check that token is stored (via app logs or secure storage debug)

### 1.3 Cross-Token Validation
- [ ] Copy admin JWT from web localStorage
- [ ] Use Postman to call `GET /api/zones/my` with that token → should work
- [ ] Copy worker JWT from mobile (debug logs)
- [ ] Use Postman to call `GET /api/notifications/unread-count` → should work

### 1.4 Token Expiry & Refresh
- [ ] Wait for token to expire (or set `ExpirationMinutes: 1` temporarily)
- [ ] On web: make an action → should auto-refresh token silently
- [ ] On mobile: make an action → should auto-refresh token silently
- [ ] If refresh fails → should redirect to login

---

## Flow 2: Task Lifecycle (Full Cross-Platform)

### 2.1 Supervisor Creates Task (Web)
- [ ] Login as **supervisor** on web
- [ ] Navigate to Tasks → Create Task
- [ ] Fill in: title, description, assign to **worker001**, set priority, due date
- [ ] Set location on the map
- [ ] Submit → confirm task appears in task list

### 2.2 Worker Sees Task (Mobile)
- [ ] On mobile (logged in as **worker001**)
- [ ] Pull to refresh task list (or wait for sync)
- [ ] **Verify**: new task appears with correct title, priority, due date
- [ ] Tap task → verify details match what supervisor entered
- [ ] Verify location/map pin matches

### 2.3 Worker Starts Task (Mobile)
- [ ] Tap "Start Task" or change status to "In Progress"
- [ ] Add optional notes
- [ ] **Verify**: status updated successfully

### 2.4 Supervisor Sees Status Change (Web)
- [ ] On web, refresh task list or task details
- [ ] **Verify**: task status shows "In Progress"
- [ ] **Verify**: worker's notes are visible

### 2.5 Worker Completes Task with Photo (Mobile)
- [ ] On mobile, tap "Complete Task"
- [ ] Take a photo (or select from gallery)
- [ ] Add completion notes
- [ ] GPS location captured automatically
- [ ] Submit → confirm success message

### 2.6 Supervisor Reviews Completion (Web)
- [ ] On web, view the completed task
- [ ] **Verify**: completion photo is visible
- [ ] **Verify**: completion notes and GPS location shown
- [ ] Approve or reject the completion
- [ ] If rejected: **verify** worker sees rejection on mobile

---

## Flow 3: Real-Time GPS Tracking

### 3.1 Worker Enables Tracking (Mobile)
- [ ] Worker is logged in and checked in
- [ ] GPS tracking starts automatically (background service)
- [ ] Check logs: SignalR connection established to `/hubs/tracking`

### 3.2 Supervisor Views Live Map (Web)
- [ ] Login as **supervisor** on web
- [ ] Navigate to Tracking / Live Map page
- [ ] **Verify**: worker's marker appears on the map
- [ ] **Verify**: marker updates as worker moves (or simulates movement)
- [ ] **Verify**: SignalR connection shown in DevTools → Network → WS tab

### 3.3 Location History
- [ ] On web, view worker's location history for today
- [ ] **Verify**: trail/path shown on map
- [ ] **Verify**: timestamps are correct

---

## Flow 4: Attendance

### 4.1 Worker Check-In (Mobile)
- [ ] Worker opens app in the morning
- [ ] Check-in with GPS location
- [ ] **Verify**: success message, attendance record created
- [ ] **Verify**: cannot check in again (duplicate prevented)

### 4.2 Admin Views Attendance (Web)
- [ ] Login as **admin** on web
- [ ] Navigate to Attendance page
- [ ] **Verify**: worker's check-in record visible with time and location
- [ ] View attendance report → **verify** data matches

### 4.3 Worker Check-Out (Mobile)
- [ ] At end of day, worker checks out
- [ ] **Verify**: check-out time recorded
- [ ] **Verify**: total hours calculated

### 4.4 Admin Sees Full Record (Web)
- [ ] Refresh attendance page
- [ ] **Verify**: check-in AND check-out both visible
- [ ] Export to Excel → **verify** data correct

---

## Flow 5: Issue Reporting

### 5.1 Worker Reports Issue (Mobile)
- [ ] Worker navigates to "Report Issue"
- [ ] Takes photo of the issue (mandatory)
- [ ] Selects issue type and severity
- [ ] GPS location auto-captured
- [ ] Adds description
- [ ] Submit → confirm success

### 5.2 Admin Sees Issue (Web)
- [ ] Login as **admin** on web
- [ ] Navigate to Issues page
- [ ] **Verify**: new issue appears with:
  - [ ] Correct photo
  - [ ] Correct type and severity
  - [ ] GPS location on map
  - [ ] Reporter name matches worker

### 5.3 Admin Resolves Issue (Web)
- [ ] Click on the issue
- [ ] Update status to "Resolved"
- [ ] Add resolution notes
- [ ] **Verify**: status updated

### 5.4 Worker Sees Resolution (Mobile)
- [ ] On mobile, refresh issues list
- [ ] **Verify**: issue status shows "Resolved"

---

## Flow 6: Notifications

### 6.1 Task Assignment Notification
- [ ] Supervisor creates a new task for worker (web)
- [ ] **Verify**: worker receives push notification on mobile (FCM)
- [ ] **Verify**: notification appears in app notification list
- [ ] Tap notification → navigates to task details

### 6.2 Web Notification Polling
- [ ] On web (supervisor logged in)
- [ ] **Verify**: notification badge in top bar shows count
- [ ] **Verify**: count updates when new notifications arrive
- [ ] Click notification bell → see notification list
- [ ] Mark as read → count decreases

---

## Flow 7: Offline Sync (Mobile)

### 7.1 Go Offline
- [ ] On mobile, enable airplane mode
- [ ] Complete a task (should save locally)
- [ ] Report an issue (should save locally)
- [ ] **Verify**: data saved to local Hive storage

### 7.2 Come Back Online
- [ ] Disable airplane mode
- [ ] **Verify**: sync indicator appears
- [ ] **Verify**: pending data uploaded to backend
- [ ] **Verify**: new data from server downloaded

### 7.3 Verify Synced Data (Web)
- [ ] On web, verify the offline-completed task shows as completed
- [ ] Verify the offline-reported issue appears in issues list

---

## Flow 8: Appeals

### 8.1 Worker Submits Appeal (Mobile)
- [ ] Find a rejected task
- [ ] Submit appeal with reason
- [ ] **Verify**: appeal created successfully

### 8.2 Supervisor Reviews Appeal (Web)
- [ ] Login as supervisor on web
- [ ] Navigate to Appeals page
- [ ] **Verify**: worker's appeal is visible
- [ ] Approve or reject with notes
- [ ] **Verify**: status updated

### 8.3 Worker Sees Decision (Mobile)
- [ ] On mobile, check appeal status
- [ ] **Verify**: shows approved/rejected with supervisor's notes

---

## Flow 9: Zones & Geofencing

### 9.1 Admin Creates Zone (Web)
- [ ] Login as admin on web
- [ ] Navigate to Zones page
- [ ] Create new zone with polygon on map
- [ ] Assign zone to worker

### 9.2 Worker Syncs Zones (Mobile)
- [ ] On mobile, sync or pull-to-refresh
- [ ] **Verify**: new zone appears in worker's zone list
- [ ] **Verify**: zone boundaries available for offline geofencing

### 9.3 Geofence Validation
- [ ] Worker tries to check in outside zone → should warn/reject
- [ ] Worker checks in inside zone → should succeed

---

## Flow 10: Dashboard Aggregation

### 10.1 Data Created on Mobile Shows on Web Dashboard
- [ ] After running flows 2-9 above
- [ ] Login as admin on web
- [ ] Navigate to Dashboard
- [ ] **Verify**: today's task count includes tasks from mobile
- [ ] **Verify**: attendance count matches mobile check-ins
- [ ] **Verify**: issue count matches mobile reports
- [ ] **Verify**: worker online/offline status is correct

---

## Cross-Cutting Concerns

### C1: Arabic / RTL
- [ ] Web displays Arabic text correctly (RTL layout)
- [ ] Mobile displays Arabic text correctly (RTL layout)
- [ ] Data created in Arabic on one platform displays correctly on the other

### C2: Photo/File Serving
- [ ] Photos uploaded from mobile are viewable on web
- [ ] File URLs from backend (`/api/files/{folder}/{filename}`) require auth
- [ ] Unauthenticated file request returns 401

### C3: Role-Based Access
- [ ] Worker cannot access admin endpoints (web returns 403)
- [ ] Worker cannot create tasks (mobile doesn't show option, API rejects)
- [ ] Supervisor can only see their own workers' data
- [ ] Admin can see everything

### C4: Concurrent Usage
- [ ] Multiple workers logged in simultaneously on mobile
- [ ] Admin and supervisor on web simultaneously
- [ ] Actions from one don't interfere with another
- [ ] SignalR broadcasts to all connected supervisors

---

## Results Summary

| Flow | Status | Notes |
|------|--------|-------|
| 1. Auth | ☐ | |
| 2. Task Lifecycle | ☐ | |
| 3. GPS Tracking | ☐ | |
| 4. Attendance | ☐ | |
| 5. Issues | ☐ | |
| 6. Notifications | ☐ | |
| 7. Offline Sync | ☐ | |
| 8. Appeals | ☐ | |
| 9. Zones | ☐ | |
| 10. Dashboard | ☐ | |
| C1. Arabic/RTL | ☐ | |
| C2. Photos/Files | ☐ | |
| C3. RBAC | ☐ | |
| C4. Concurrent | ☐ | |

**Tester**: _______________  **Date**: _______________
