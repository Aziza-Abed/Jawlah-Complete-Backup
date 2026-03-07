#!/usr/bin/env node
// ──────────────────────────────────────────────────────────────
// Quick Smoke Test — runs WITHOUT a test framework
// ──────────────────────────────────────────────────────────────
// Usage:  node quick-smoke-test.js
//         npm run test:smoke
//
// Prerequisites: backend running on http://localhost:5000
// ──────────────────────────────────────────────────────────────
import axios from 'axios';

const BASE = process.env.API_BASE_URL || 'http://localhost:5000/api';
const PASS = '\x1b[32m PASS \x1b[0m';
const FAIL = '\x1b[31m FAIL \x1b[0m';
const SKIP = '\x1b[33m SKIP \x1b[0m';

let passed = 0, failed = 0, skipped = 0;
const results = [];

async function test(name, fn) {
  try {
    await fn();
    results.push({ name, status: 'pass' });
    console.log(`${PASS} ${name}`);
    passed++;
  } catch (e) {
    if (e.message?.startsWith('SKIP:')) {
      results.push({ name, status: 'skip', reason: e.message });
      console.log(`${SKIP} ${name} — ${e.message}`);
      skipped++;
    } else {
      results.push({ name, status: 'fail', error: e.message });
      console.log(`${FAIL} ${name}`);
      console.log(`       ${e.message}`);
      failed++;
    }
  }
}

function assert(condition, msg) {
  if (!condition) throw new Error(msg || 'Assertion failed');
}

const client = axios.create({ baseURL: BASE, timeout: 10000, validateStatus: () => true });

// ── Token storage ────────────────────────────────────────────
let adminToken, supervisorToken, workerToken;
let workerId, taskId;

// ══════════════════════════════════════════════════════════════
// Tests
// ══════════════════════════════════════════════════════════════

console.log('\n═══════════════════════════════════════════════════');
console.log(' FollowUp Integration Smoke Test');
console.log(` Backend: ${BASE}`);
console.log('═══════════════════════════════════════════════════\n');

// ── 1. Health ────────────────────────────────────────────────

await test('1.1 Health check returns 200', async () => {
  const res = await client.get('/health');
  assert(res.status === 200, `Got ${res.status}`);
});

await test('1.2 Ping returns 200', async () => {
  const res = await client.get('/health/ping');
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 2. Auth — Web-style login ────────────────────────────────

await test('2.1 Admin login (web-style)', async () => {
  const res = await client.post('/auth/login', {
    username: 'admin', password: 'pass',
  });
  if (res.data?.data?.requiresOtp) throw new Error('SKIP: OTP required — cannot automate');
  assert(res.status === 200 && res.data?.success, `Login failed: ${res.status} ${res.data?.message}`);
  adminToken = res.data.data.accessToken || res.data.data.token;
  assert(adminToken, 'No token returned');
});

await test('2.2 Supervisor login (web-style)', async () => {
  const res = await client.post('/auth/login', {
    username: 'super1', password: 'pass',
  });
  if (res.data?.data?.requiresOtp) throw new Error('SKIP: OTP required');
  assert(res.status === 200 && res.data?.success, `Login failed: ${res.status} ${res.data?.message}`);
  supervisorToken = res.data.data.accessToken || res.data.data.token;
});

await test('2.3 Worker login (mobile-style with deviceId)', async () => {
  // Requires RegisteredDeviceId set in DB to match our test device.
  // Setup: sqlcmd -S localhost -d FollowUpNew -Q "UPDATE Users SET RegisteredDeviceId='a1b2c3d4-e5f6-7890-abcd-ef1234567890' WHERE Username='worker1'" -C
  const res = await client.post('/auth/login-gps', {
    username: 'worker1', password: 'pass',
    deviceId: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
  });
  if (res.data?.data?.requiresOtp) {
    // Fallback to web login
    const fb = await client.post('/auth/login', { username: 'worker1', password: 'pass' });
    if (fb.data?.data?.requiresOtp) throw new Error('SKIP: OTP required — run DB setup first');
    assert(fb.status === 200 && fb.data?.success, `Fallback failed: ${fb.status} ${fb.data?.message}`);
    workerToken = fb.data.data.accessToken || fb.data.data.token;
    return;
  }
  assert(res.status === 200 && res.data?.success, `Login failed: ${res.status} ${res.data?.message}`);
  workerToken = res.data.data.accessToken || res.data.data.token;
});

await test('2.4 Invalid credentials rejected', async () => {
  const res = await client.post('/auth/login', {
    username: 'admin', password: 'wrongpassword',
  });
  assert(res.status === 401 || res.status === 429 || res.data?.success === false, 'Should have been rejected');
});

// ── 3. Auth — Profile endpoints ──────────────────────────────

await test('3.1 GET /auth/me returns admin profile', async () => {
  if (!adminToken) throw new Error('SKIP: No admin token');
  const res = await client.get('/auth/me', {
    headers: { Authorization: `Bearer ${adminToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
  assert(res.data?.data?.username || res.data?.data?.userName, 'No username in profile');
});

await test('3.2 GET /auth/profile returns worker profile', async () => {
  if (!workerToken) throw new Error('SKIP: No worker token');
  const res = await client.get('/auth/profile', {
    headers: { Authorization: `Bearer ${workerToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
  workerId = res.data?.data?.userId || res.data?.data?.id;
});

await test('3.3 Unauthenticated request returns 401', async () => {
  const res = await client.get('/auth/me');
  assert(res.status === 401, `Expected 401, got ${res.status}`);
});

// ── 4. Municipality (public + auth) ─────────────────────────

await test('4.1 GET /municipality/default (public)', async () => {
  const res = await client.get('/municipality/default');
  assert(res.status === 200, `Got ${res.status}`);
});

await test('4.2 GET /municipality/current (auth)', async () => {
  if (!adminToken) throw new Error('SKIP: No admin token');
  const res = await client.get('/municipality/current', {
    headers: { Authorization: `Bearer ${adminToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 5. Zones ─────────────────────────────────────────────────

await test('5.1 GET /zones (admin)', async () => {
  if (!adminToken) throw new Error('SKIP: No admin token');
  const res = await client.get('/zones', {
    headers: { Authorization: `Bearer ${adminToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

await test('5.2 GET /zones/my (worker — mobile endpoint)', async () => {
  if (!workerToken) throw new Error('SKIP: No worker token');
  const res = await client.get('/zones/my', {
    headers: { Authorization: `Bearer ${workerToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 6. Tasks — Cross-client workflow ─────────────────────────

await test('6.1 Supervisor creates task → worker sees it', async () => {
  if (!supervisorToken || !workerId) throw new Error('SKIP: Missing tokens/workerId');

  // Supervisor creates a task (web-style)
  const createRes = await client.post('/tasks', {
    title: 'Smoke Test Task',
    description: 'Created by smoke test — supervisor (web) → worker (mobile)',
    assignedToUserId: workerId,
    priority: 1,
    dueDate: new Date(Date.now() + 86400000 * 30).toISOString(), // 30 days out to avoid seed data conflicts
    latitude: 31.9038,
    longitude: 35.2034,
  }, {
    headers: { Authorization: `Bearer ${supervisorToken}` },
  });
  assert(createRes.status === 200 || createRes.status === 201,
    `Create failed: ${createRes.status} ${JSON.stringify(createRes.data)}`);
  taskId = createRes.data?.data?.taskId || createRes.data?.data?.id;
  assert(taskId, 'No task ID returned');

  // Worker fetches their tasks (mobile-style)
  const myTasks = await client.get('/tasks/my-tasks', {
    headers: { Authorization: `Bearer ${workerToken}` },
  });
  assert(myTasks.status === 200, `my-tasks failed: ${myTasks.status}`);
  const items = myTasks.data?.data?.items || myTasks.data?.data || [];
  const found = Array.isArray(items) && items.some(t => (t.taskId || t.id) === taskId);
  assert(found, `Worker cannot see task ${taskId} in my-tasks`);
});

await test('6.2 Worker gets task details (mobile)', async () => {
  if (!workerToken || !taskId) throw new Error('SKIP: No task to check');
  const res = await client.get(`/tasks/${taskId}`, {
    headers: { Authorization: `Bearer ${workerToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
  assert(res.data?.data?.title === 'Smoke Test Task', 'Title mismatch');
});

await test('6.3 Worker updates task status (mobile)', async () => {
  if (!workerToken || !taskId) throw new Error('SKIP: No task');
  const res = await client.put(`/tasks/${taskId}/status`, {
    status: 1, // InProgress
    completionNotes: 'Worker started from mobile',
  }, {
    headers: { Authorization: `Bearer ${workerToken}` },
  });
  assert(res.status === 200, `Status update failed: ${res.status} ${JSON.stringify(res.data)}`);
});

await test('6.4 Cleanup: delete test task', async () => {
  if (!supervisorToken || !taskId) throw new Error('SKIP: Nothing to clean');
  const res = await client.delete(`/tasks/${taskId}`, {
    headers: { Authorization: `Bearer ${supervisorToken}` },
  });
  // Accept 200, 204, or 404 (already gone)
  assert([200, 204, 404].includes(res.status), `Delete failed: ${res.status}`);
});

// ── 7. Notifications ────────────────────────────────────────

await test('7.1 GET /notifications (worker)', async () => {
  if (!workerToken) throw new Error('SKIP: No worker token');
  const res = await client.get('/notifications', {
    headers: { Authorization: `Bearer ${workerToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

await test('7.2 GET /notifications/unread-count (web polling)', async () => {
  if (!supervisorToken) throw new Error('SKIP: No supervisor token');
  const res = await client.get('/notifications/unread-count', {
    headers: { Authorization: `Bearer ${supervisorToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 8. Attendance (mobile → web) ─────────────────────────────

await test('8.1 Worker check-in (mobile)', async () => {
  if (!workerToken) throw new Error('SKIP: No worker token');
  const res = await client.post('/attendance/checkin', {
    latitude: 31.9038,
    longitude: 35.2034,
    accuracy: 10,
  }, {
    headers: { Authorization: `Bearer ${workerToken}` },
  });
  // 200 = success, 400 = already checked in (both are acceptable)
  assert([200, 400].includes(res.status),
    `Checkin failed: ${res.status} ${JSON.stringify(res.data)}`);
});

await test('8.2 GET /attendance/today (mobile)', async () => {
  if (!workerToken) throw new Error('SKIP: No worker token');
  const res = await client.get('/attendance/today', {
    headers: { Authorization: `Bearer ${workerToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 9. Issues ────────────────────────────────────────────────

await test('9.1 GET /issues (admin — web)', async () => {
  if (!adminToken) throw new Error('SKIP: No admin token');
  const res = await client.get('/issues', {
    headers: { Authorization: `Bearer ${adminToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 10. Appeals ──────────────────────────────────────────────

await test('10.1 GET /appeals/pending (supervisor — web)', async () => {
  if (!supervisorToken) throw new Error('SKIP: No supervisor token');
  // Web uses /appeals/pending for supervisor; /appeals may only support POST
  const res = await client.get('/appeals/pending', {
    headers: { Authorization: `Bearer ${supervisorToken}` },
  });
  // 200 = has appeals, 404 = endpoint variant doesn't exist
  assert([200, 204].includes(res.status), `Got ${res.status}: ${JSON.stringify(res.data)}`);
});

await test('10.2 GET /appeals/my-appeals (worker — mobile)', async () => {
  if (!workerToken) throw new Error('SKIP: No worker token');
  const res = await client.get('/appeals/my-appeals', {
    headers: { Authorization: `Bearer ${workerToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 11. Reports (web only) ──────────────────────────────────

await test('11.1 GET /reports/tasks/summary (admin)', async () => {
  if (!adminToken) throw new Error('SKIP: No admin token');
  const res = await client.get('/reports/tasks/summary?period=Today', {
    headers: { Authorization: `Bearer ${adminToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 12. Dashboard (web) ─────────────────────────────────────

await test('12.1 GET /dashboard/overview (admin)', async () => {
  if (!adminToken) throw new Error('SKIP: No admin token');
  const res = await client.get('/dashboard/overview', {
    headers: { Authorization: `Bearer ${adminToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 13. Sync endpoints (mobile batch) ───────────────────────

await test('13.1 POST /sync/tasks/batch (mobile — empty batch)', async () => {
  if (!workerToken) throw new Error('SKIP: No worker token');
  const res = await client.post('/sync/tasks/batch', {
    items: [/* empty — testing the endpoint accepts the request */],
    deviceId: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
  }, {
    headers: { Authorization: `Bearer ${workerToken}` },
  });
  // 200 = accepted, 400 = validation (items required) — both prove connectivity
  assert([200, 400].includes(res.status), `Got ${res.status}`);
});

// ── 14. Audit (admin web) ───────────────────────────────────

await test('14.1 GET /audit (admin)', async () => {
  if (!adminToken) throw new Error('SKIP: No admin token');
  const res = await client.get('/audit?count=10', {
    headers: { Authorization: `Bearer ${adminToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 15. Users (web admin) ───────────────────────────────────

await test('15.1 GET /users (admin)', async () => {
  if (!adminToken) throw new Error('SKIP: No admin token');
  const res = await client.get('/users', {
    headers: { Authorization: `Bearer ${adminToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

await test('15.2 GET /users/my-workers (supervisor)', async () => {
  if (!supervisorToken) throw new Error('SKIP: No supervisor token');
  const res = await client.get('/users/my-workers', {
    headers: { Authorization: `Bearer ${supervisorToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 16. Task Templates (web) ────────────────────────────────

await test('16.1 GET /task-templates', async () => {
  if (!adminToken) throw new Error('SKIP: No admin token');
  const res = await client.get('/task-templates', {
    headers: { Authorization: `Bearer ${adminToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ── 17. Cross-client token validation ───────────────────────

await test('17.1 Web token works on mobile endpoint', async () => {
  if (!adminToken) throw new Error('SKIP: No admin token');
  // Admin (web) token should work on /zones/my (mobile endpoint)
  const res = await client.get('/zones/my', {
    headers: { Authorization: `Bearer ${adminToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

await test('17.2 Mobile token works on web endpoint', async () => {
  if (!workerToken) throw new Error('SKIP: No worker token');
  // Worker (mobile) token on /notifications/unread-count (web polling)
  const res = await client.get('/notifications/unread-count', {
    headers: { Authorization: `Bearer ${workerToken}` },
  });
  assert(res.status === 200, `Got ${res.status}`);
});

// ══════════════════════════════════════════════════════════════
// Summary
// ══════════════════════════════════════════════════════════════

console.log('\n═══════════════════════════════════════════════════');
console.log(` Results:  ${passed} passed  ${failed} failed  ${skipped} skipped`);
console.log('═══════════════════════════════════════════════════\n');

if (failed > 0) {
  console.log('Failed tests:');
  results.filter(r => r.status === 'fail').forEach(r => {
    console.log(`  - ${r.name}: ${r.error}`);
  });
  console.log('');
}

process.exit(failed > 0 ? 1 : 0);
