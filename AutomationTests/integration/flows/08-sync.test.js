// ──────────────────────────────────────────────────────────────
// Flow 8: Offline Sync — Mobile batches → Backend processes
// ──────────────────────────────────────────────────────────────
import { describe, it, expect, beforeAll } from 'vitest';
import { clients, loginAs, tokens } from '../helpers.js';
import { CONFIG } from '../config.js';

describe('Flow 8 — Offline Sync (Mobile batch → Backend)', () => {

  beforeAll(async () => {
    await loginAs('worker');
  });

  it('Task batch sync endpoint is reachable', async () => {
    if (!tokens.worker) return;
    const res = await clients.worker.post('/sync/tasks/batch', {
      deviceId: CONFIG.devices.mobileDeviceId,
      clientTime: new Date().toISOString(),
      items: [{ taskId: 1, status: 'InProgress', completionNotes: 'test sync', eventTime: new Date().toISOString() }],
    });
    // 200 = synced, 400 = validation error
    expect([200, 400].includes(res.status)).toBe(true);
  });

  it('Attendance batch sync endpoint is reachable', async () => {
    if (!tokens.worker) return;
    const res = await clients.worker.post('/sync/attendance/batch', {
      deviceId: CONFIG.devices.mobileDeviceId,
      clientTime: new Date().toISOString(),
      items: [{ attendanceType: 'CheckIn', latitude: 31.9038, longitude: 35.2034, eventTime: new Date().toISOString() }],
    });
    // 200 = synced, 400 = validation error
    expect([200, 400].includes(res.status)).toBe(true);
  });

  it('Location tracking endpoint is reachable', async () => {
    if (!tokens.worker) return;
    const res = await clients.worker.post('/tracking/location', {
      latitude: 31.9,
      longitude: 35.2,
      accuracy: 10,
      batteryLevel: 80,
    });
    // 200 = saved, 400 = validation error
    expect([200, 400].includes(res.status)).toBe(true);
  });
});
