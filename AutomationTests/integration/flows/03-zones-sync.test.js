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
});
