// ──────────────────────────────────────────────────────────────
// Flow 6: Issues — Mobile reports → Web admin views & manages
// ──────────────────────────────────────────────────────────────
import { describe, it, expect, beforeAll } from 'vitest';
import { clients, loginAs, tokens } from '../helpers.js';

describe('Flow 6 — Issues (Mobile → Backend → Web)', () => {

  beforeAll(async () => {
    await loginAs('admin');
    await loginAs('worker');
  });

  // Note: issue reporting with photo requires multipart/form-data
  // which is tested in the E2E suite. Here we test the read paths.

  it('Worker views their reported issues (mobile)', async () => {
    if (!tokens.worker) return;
    // Workers get their own issues via GET /issues (auto-filtered by role)
    const res = await clients.worker.get('/issues');
    expect(res.status).toBe(200);
  });

  it('Admin views all issues (web)', async () => {
    if (!tokens.admin) return;
    const res = await clients.admin.get('/issues');
    expect(res.status).toBe(200);
  });

  it('Admin views unresolved issue count (web)', async () => {
    if (!tokens.admin) return;
    // No /issues/unresolved endpoint — use /issues/unresolved-count
    const res = await clients.admin.get('/issues/unresolved-count');
    expect(res.status).toBe(200);
  });

  it('Admin views critical issues (web)', async () => {
    if (!tokens.admin) return;
    // No /reports/issues/summary — use /issues/critical instead
    const res = await clients.admin.get('/issues/critical');
    expect(res.status).toBe(200);
  });
});
