// ──────────────────────────────────────────────────────────────
// Flow 3: Zones — Admin creates on Web, Worker syncs on Mobile
// ──────────────────────────────────────────────────────────────
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { clients, loginAs, tokens, makeZone, registerCleanup, runCleanups } from '../helpers.js';

describe('Flow 3 — Zones (Web Admin → Mobile Worker sync)', () => {
  let createdZoneId;

  beforeAll(async () => {
    await loginAs('admin');
    await loginAs('worker');
  });

  afterAll(async () => {
    await runCleanups();
  });

  it('Admin fetches all zones (web)', async () => {
    if (!tokens.admin) return;
    const res = await clients.admin.get('/zones');
    expect(res.status).toBe(200);
  });

  it('Admin creates a new zone (web)', async () => {
    if (!tokens.admin) return;

    const zone = makeZone();
    const res = await clients.admin.post('/zones', zone);

    if (res.status === 200 || res.status === 201) {
      createdZoneId = res.data?.data?.zoneId || res.data?.data?.id;
      registerCleanup(async () => {
        await clients.admin.delete(`/zones/${createdZoneId}`);
      });
    } else {
      console.log(`  Zone creation failed: ${res.status}`, JSON.stringify(res.data));
    }
    expect(res.status === 200 || res.status === 201).toBe(true);
  });

  it('Worker can fetch zones list (mobile sync)', async () => {
    if (!tokens.worker) return;
    const res = await clients.worker.get('/zones');
    expect(res.status).toBe(200);
  });

  it('Worker fetches /zones/my for offline geofencing (mobile)', async () => {
    if (!tokens.worker) return;
    const res = await clients.worker.get('/zones/my');
    expect(res.status).toBe(200);
  });

  // ── Zone assignment to worker (was failing with 500 concurrency bug) ──

  it('Supervisor assigns zone to worker', async () => {
    await loginAs('supervisor');
    if (!tokens.supervisor) return;

    // Get a worker ID
    const workersRes = await clients.supervisor.get('/users/my-workers');
    if (workersRes.status !== 200 || !workersRes.data?.data?.length) return;
    const workerId = workersRes.data.data[0].userId;

    // Get available zones
    const zonesRes = await clients.supervisor.get('/zones');
    if (zonesRes.status !== 200 || !zonesRes.data?.data?.length) return;
    const zoneId = zonesRes.data.data[0].zoneId;

    // Assign zone to worker — should return 200 (not 500)
    const res = await clients.supervisor.post(`/users/${workerId}/zones`, {
      zoneIds: [zoneId],
    });
    expect(res.status).toBe(200);
  });

  it('Worker zones are retrievable after assignment', async () => {
    if (!tokens.supervisor) return;

    const workersRes = await clients.supervisor.get('/users/my-workers');
    if (workersRes.status !== 200 || !workersRes.data?.data?.length) return;
    const workerId = workersRes.data.data[0].userId;

    const res = await clients.supervisor.get(`/users/${workerId}/zones`);
    expect(res.status).toBe(200);
  });
});
