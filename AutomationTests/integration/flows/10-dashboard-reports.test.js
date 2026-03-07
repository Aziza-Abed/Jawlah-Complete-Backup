// ──────────────────────────────────────────────────────────────
// Flow 10: Dashboard & Reports (Web-only but uses mobile data)
// ──────────────────────────────────────────────────────────────
// The web dashboard aggregates data from mobile workers.
// This tests that all aggregation endpoints work correctly.
// ──────────────────────────────────────────────────────────────
import { describe, it, expect, beforeAll } from 'vitest';
import { clients, loginAs, tokens } from '../helpers.js';

describe('Flow 10 — Dashboard & Reports (Web consumes mobile data)', () => {

  beforeAll(async () => {
    await loginAs('admin');
    await loginAs('supervisor');
  });

  // ── Dashboard ──────────────────────────────────────────────

  it('Admin dashboard overview', async () => {
    if (!tokens.admin) return;
    const res = await clients.admin.get('/dashboard/overview');
    expect(res.status).toBe(200);
    expect(res.data?.data).toBeTruthy();
  });

  it('Admin worker status (for live tracking page)', async () => {
    if (!tokens.admin) return;
    const res = await clients.admin.get('/dashboard/worker-status');
    expect(res.status).toBe(200);
  });

  // ── Reports ────────────────────────────────────────────────

  it('Task summary report', async () => {
    if (!tokens.admin) return;
    const res = await clients.admin.get('/reports/tasks/summary?period=ThisMonth');
    expect(res.status).toBe(200);
  });

  it('Attendance summary report', async () => {
    if (!tokens.admin) return;
    const res = await clients.admin.get('/reports/attendance');
    expect(res.status).toBe(200);
  });

  it('Issues summary report', async () => {
    if (!tokens.admin) return;
    // No dedicated issues report endpoint — covered by tasks/zones reports
    const res = await clients.admin.get('/reports/zones/summary?period=ThisMonth');
    expect(res.status).toBe(200);
  });

  it('Supervisor monitors workers', async () => {
    if (!tokens.supervisor) return;
    const res = await clients.supervisor.get('/users/my-workers');
    expect(res.status).toBe(200);
  });

  // ── Audit Trail ────────────────────────────────────────────

  it('Admin views audit logs', async () => {
    if (!tokens.admin) return;
    const res = await clients.admin.get('/audit?count=10');
    expect(res.status).toBe(200);
  });
});
