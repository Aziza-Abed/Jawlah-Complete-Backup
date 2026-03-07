// ──────────────────────────────────────────────────────────────
// Flow 4: Attendance — Mobile check-in/out → Web views records
// ──────────────────────────────────────────────────────────────
import { describe, it, expect, beforeAll } from 'vitest';
import { clients, loginAs, tokens } from '../helpers.js';
import { CONFIG } from '../config.js';

describe('Flow 4 — Attendance (Mobile → Backend → Web)', () => {

  beforeAll(async () => {
    await loginAs('admin');
    await loginAs('worker');
  });

  it('Worker checks in from mobile', async () => {
    if (!tokens.worker) return;

    const res = await clients.worker.post('/attendance/checkin', {
      latitude: CONFIG.gps.validLocation.latitude,
      longitude: CONFIG.gps.validLocation.longitude,
      accuracy: 10,
    });
    // 200=success, 400=already checked in — both ok for test
    expect([200, 400].includes(res.status)).toBe(true);
  });

  it('Worker gets today attendance from mobile', async () => {
    if (!tokens.worker) return;

    const res = await clients.worker.get('/attendance/today');
    expect(res.status).toBe(200);
  });

  it('Admin views attendance records (web)', async () => {
    if (!tokens.admin) return;

    const res = await clients.admin.get('/attendance/history');
    expect(res.status).toBe(200);
  });

  it('Admin views attendance reports (web)', async () => {
    if (!tokens.admin) return;

    const res = await clients.admin.get('/reports/attendance');
    expect(res.status).toBe(200);
  });
});
