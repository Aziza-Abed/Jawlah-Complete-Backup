// ──────────────────────────────────────────────────────────────
// Flow 7: Appeals — Mobile worker submits → Web supervisor reviews
// ──────────────────────────────────────────────────────────────
import { describe, it, expect, beforeAll } from 'vitest';
import { clients, loginAs, tokens } from '../helpers.js';

describe('Flow 7 — Appeals (Mobile → Backend → Web)', () => {

  beforeAll(async () => {
    await loginAs('supervisor');
    await loginAs('worker');
  });

  it('Worker views their appeals (mobile)', async () => {
    if (!tokens.worker) return;
    const res = await clients.worker.get('/appeals/my-appeals');
    expect(res.status).toBe(200);
  });

  it('Supervisor views pending appeals (web)', async () => {
    if (!tokens.supervisor) return;
    const res = await clients.supervisor.get('/appeals/pending');
    expect(res.status).toBe(200);
  });
});
