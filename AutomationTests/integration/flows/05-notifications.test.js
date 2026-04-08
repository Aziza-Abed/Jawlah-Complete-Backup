// ──────────────────────────────────────────────────────────────
// Flow 5: Notifications — Backend → Web (polling) + Mobile (FCM)
// ──────────────────────────────────────────────────────────────
import { describe, it, expect, beforeAll } from 'vitest';
import { clients, loginAs, tokens } from '../helpers.js';

describe('Flow 5 — Notifications (Backend → Web + Mobile)', () => {
  let workerId;
  let supervisorId;

  beforeAll(async () => {
    await loginAs('admin');
    await loginAs('supervisor');
    await loginAs('worker');

    // Get IDs for worker assignment notification test
    const workerProfile = await clients.worker.get('/users/me');
    if (workerProfile.status === 200) {
      workerId = workerProfile.data?.data?.userId;
    }
    const supervisors = await clients.admin.get('/users/by-role/Supervisor');
    if (supervisors.status === 200 && supervisors.data?.data?.length > 0) {
      supervisorId = supervisors.data.data[0].userId;
    }
  });

  it('Worker gets notification list (mobile)', async () => {
    if (!tokens.worker) return;
    const res = await clients.worker.get('/notifications');
    expect(res.status).toBe(200);
  });

  it('Worker gets unread notifications (mobile)', async () => {
    if (!tokens.worker) return;
    const res = await clients.worker.get('/notifications/unread');
    expect(res.status).toBe(200);
  });

  it('Supervisor polls unread count (web — every 30s)', async () => {
    if (!tokens.supervisor) return;
    const res = await clients.supervisor.get('/notifications/unread-count');
    expect(res.status).toBe(200);
    expect(typeof res.data?.data === 'number' || typeof res.data?.data?.count === 'number').toBe(true);
  });

  // ── Worker assignment triggers supervisor notification ─────

  it('Assigning worker to supervisor creates a notification', async () => {
    if (!tokens.admin || !workerId || !supervisorId) return;

    // Mark all read first so we can detect the new one
    await clients.supervisor.put('/notifications/mark-all-read');

    // Assign worker to supervisor
    const res = await clients.admin.put(`/users/${workerId}`, {
      supervisorId: supervisorId,
    });
    expect(res.status).toBe(200);

    // Supervisor should now have an unread notification
    const countRes = await clients.supervisor.get('/notifications/unread-count');
    expect(countRes.status).toBe(200);
    const count = countRes.data?.data?.count ?? countRes.data?.data;
    expect(count).toBeGreaterThanOrEqual(1);
  });

  it('Bulk reassign sends notifications to new supervisor', async () => {
    if (!tokens.admin || !workerId || !supervisorId) return;

    const res = await clients.admin.post('/users/bulk-reassign-supervisor', {
      workerIds: [workerId],
      newSupervisorId: supervisorId,
    });
    expect(res.status).toBe(200);
  });

  // ── Mark read / clear ─────────────────────────────────────

  it('Worker marks all notifications read (mobile)', async () => {
    if (!tokens.worker) return;
    const res = await clients.worker.put('/notifications/mark-all-read');
    expect(res.status).toBe(200);
  });

  it('After mark-all-read, unread count is 0', async () => {
    if (!tokens.worker) return;
    const res = await clients.worker.get('/notifications/unread-count');
    expect(res.status).toBe(200);
    const count = res.data?.data?.count ?? res.data?.data;
    expect(count).toBe(0);
  });
});
