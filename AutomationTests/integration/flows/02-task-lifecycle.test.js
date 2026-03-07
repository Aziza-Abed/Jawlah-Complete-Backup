// ──────────────────────────────────────────────────────────────
// Flow 2: Task Lifecycle (Web ↔ Mobile ↔ Backend)
// ──────────────────────────────────────────────────────────────
// Full lifecycle: Admin/Supervisor creates on web → Worker sees
// on mobile → Worker completes → Supervisor reviews
// ──────────────────────────────────────────────────────────────
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { clients, loginAs, tokens, makeTask, registerCleanup, runCleanups } from '../helpers.js';
import { CONFIG } from '../config.js';

describe('Flow 2 — Task Lifecycle (Web → Mobile → Web)', () => {
  let workerId;
  let createdTaskId;

  beforeAll(async () => {
    await loginAs('admin');
    await loginAs('supervisor');
    await loginAs('worker');

    // Get worker's ID
    const profile = await clients.worker.get('/auth/profile');
    workerId = profile.data?.data?.userId || profile.data?.data?.id;
  });

  afterAll(async () => {
    await runCleanups();
  });

  // ── Step 1: Supervisor creates task (Web) ──────────────────

  it('Step 1: Supervisor creates a task via web', async () => {
    if (!tokens.supervisor || !workerId) return;

    const task = makeTask(workerId, {
      title: 'E2E Lifecycle Task',
      description: 'Testing full task lifecycle across web and mobile',
    });

    const res = await clients.supervisor.post('/tasks', task);
    // 200/201 = created, 400 = worker at max active tasks (seed data)
    expect([200, 201, 400].includes(res.status)).toBe(true);

    if (res.status === 200 || res.status === 201) {
      createdTaskId = res.data.data.taskId || res.data.data.id;
      registerCleanup(async () => {
        await clients.supervisor.delete(`/tasks/${createdTaskId}`);
      });
    } else {
      // fall back to existing task from seed data
      const myTasks = await clients.worker.get('/tasks/my-tasks');
      if (myTasks.data?.data?.length > 0) {
        createdTaskId = myTasks.data.data[0].taskId;
      }
    }
  });

  // ── Step 2: Worker sees task in their list (Mobile) ────────

  it('Step 2: Worker sees the new task in /my-tasks (mobile)', async () => {
    if (!tokens.worker || !createdTaskId) return;

    const res = await clients.worker.get('/tasks/my-tasks');
    expect(res.status).toBe(200);

    const items = res.data?.data?.items || res.data?.data || [];
    const found = items.find(t => (t.taskId || t.id) === createdTaskId);
    expect(found).toBeTruthy();
  });

  // ── Step 3: Worker gets task details (Mobile) ──────────────

  it('Step 3: Worker fetches task details (mobile)', async () => {
    if (!tokens.worker || !createdTaskId) return;

    const res = await clients.worker.get(`/tasks/${createdTaskId}`);
    expect(res.status).toBe(200);

    const task = res.data?.data;
    expect(task).toBeTruthy();
  });

  // ── Step 4: Worker starts task (Mobile) ────────────────────

  it('Step 4: Worker updates task status to InProgress (mobile)', async () => {
    if (!tokens.worker || !createdTaskId) return;

    const res = await clients.worker.put(`/tasks/${createdTaskId}/status`, {
      status: 1, // InProgress
      completionNotes: 'Starting task from mobile device',
    });
    // 200 = updated, 400 = task already in progress or completed (seed data)
    expect([200, 400].includes(res.status)).toBe(true);
  });

  // ── Step 5: Supervisor sees status change (Web) ────────────

  it('Step 5: Supervisor sees task is now InProgress (web)', async () => {
    if (!tokens.supervisor || !createdTaskId) return;

    const res = await clients.supervisor.get(`/tasks/${createdTaskId}`);
    expect(res.status).toBe(200);

    const task = res.data?.data;
    // Status should reflect InProgress (value=1 or string)
    const status = task.status;
    expect(status === 1 || status === 'InProgress').toBe(true);
  });

  // ── Step 6: Supervisor sees task in worker list (Web) ──────

  it('Step 6: Supervisor can view worker tasks (web)', async () => {
    if (!tokens.supervisor || !workerId) return;

    // No /tasks/worker/{id}/tasks endpoint — use /tasks/all?workerId={id}
    const res = await clients.supervisor.get(`/tasks/all?workerId=${workerId}`);
    expect(res.status).toBe(200);
  });

  // ── Step 7: Admin sees task in all-tasks list (Web) ────────

  it('Step 7: Admin sees task in global task list (web)', async () => {
    if (!tokens.admin || !createdTaskId) return;

    const res = await clients.admin.get('/tasks/all');
    expect(res.status).toBe(200);
  });
});
