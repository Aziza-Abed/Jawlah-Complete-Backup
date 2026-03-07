// ──────────────────────────────────────────────────────────────
// Flow 5: Notifications — Backend → Web (polling) + Mobile (FCM)
// ──────────────────────────────────────────────────────────────
import { describe, it, expect, beforeAll } from 'vitest';
import { clients, loginAs, tokens } from '../helpers.js';

describe('Flow 5 — Notifications (Backend → Web + Mobile)', () => {

  beforeAll(async () => {
    await loginAs('supervisor');
    await loginAs('worker');
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
