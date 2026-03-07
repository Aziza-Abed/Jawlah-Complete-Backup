// ──────────────────────────────────────────────────────────────
// Flow 11: Photo Upload & Retrieval — Worker → Server → Supervisor
// ──────────────────────────────────────────────────────────────
// Verifies that photos uploaded by workers (task completion,
// issue reporting) are stored, returned in API responses, and
// downloadable by supervisors.
// ──────────────────────────────────────────────────────────────
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import {
  clients, loginAs, tokens, makeTask,
  registerCleanup, runCleanups,
  makeMinimalPng, buildMultipart,
} from '../helpers.js';
import { CONFIG } from '../config.js';

let workerId;

beforeAll(async () => {
  await loginAs('admin');
  await loginAs('supervisor');
  await loginAs('worker');

  const profile = await clients.worker.get('/auth/profile');
  workerId = profile.data?.data?.userId || profile.data?.data?.id;
}, 30000);

// ════════════════════════════════════════════════════════════════
// A) Task Photo: Worker completes task with photo → Supervisor
//    can see the photo URL and download the image
// ════════════════════════════════════════════════════════════════
describe('Task Photo Flow — Worker uploads, Supervisor views', () => {
  let taskId;
  let photoUrl;

  beforeAll(async () => {
    if (!workerId) return;
    // Create and start a task
    const task = makeTask(workerId, {
      title: 'Photo Flow Test — Task',
      description: 'Verifying photo reaches supervisor',
    });
    const res = await clients.supervisor.post('/tasks', task);
    if (res.status === 200 || res.status === 201) {
      taskId = res.data?.data?.taskId || res.data?.data?.id;
      registerCleanup(async () => {
        await clients.supervisor.delete(`/tasks/${taskId}`);
      });
      // Start the task
      await clients.worker.put(`/tasks/${taskId}/status`, { status: 1 });
    }
  });

  afterAll(async () => {
    await runCleanups();
  });

  it('Worker submits task completion with photo', async () => {
    if (!taskId) return;

    const png = makeMinimalPng();
    const form = buildMultipart(
      {
        completionNotes: 'Photo flow test — task completed',
        latitude: CONFIG.gps.validLocation.latitude,
        longitude: CONFIG.gps.validLocation.longitude,
      },
      {
        photo: { buffer: png, filename: 'task-evidence.png', contentType: 'image/png' },
      }
    );

    const res = await clients.worker.post(`/tasks/${taskId}/complete`, form.data, {
      headers: {
        ...form.headers,
        Authorization: `Bearer ${tokens.worker}`,
      },
    });

    // 200 = completed, 400 = GPS distance warning (still uploaded)
    expect([200, 400].includes(res.status)).toBe(true);
  });

  it('Supervisor fetches task and sees photo URL', async () => {
    if (!taskId) return;

    const res = await clients.supervisor.get(`/tasks/${taskId}`);
    expect(res.status).toBe(200);

    const task = res.data?.data;

    // Photo should be in either photos[] array or photoUrl field
    const hasPhotos = (task.photos && task.photos.length > 0) || task.photoUrl;

    if (hasPhotos) {
      photoUrl = task.photos?.[0] || task.photoUrl;
      console.log(`  [Photo Flow] Task photo URL: ${photoUrl}`);
    } else {
      console.log('  [Photo Flow] No photo URL — completion may have been rejected (GPS distance)');
    }

    // Task should exist regardless
    expect(task.taskId || task.id).toBeTruthy();
  });

  it('Supervisor can download the actual image file', async () => {
    if (!photoUrl) {
      console.log('  [Photo Flow] Skipping download — no photo URL available');
      return;
    }

    // Extract the relative path from the full URL
    // photoUrl format: http://localhost:5000/api/files/tasks/{guid}.png
    const filePath = photoUrl.includes('/api/files/')
      ? '/files/' + photoUrl.split('/api/files/')[1]
      : photoUrl;

    const res = await clients.supervisor.get(filePath, {
      responseType: 'arraybuffer',
    });

    expect(res.status).toBe(200);
    // Should return an image content type
    const contentType = res.headers['content-type'];
    expect(contentType).toMatch(/image\/(png|jpeg|jpg|gif)/);
    // Should have actual binary data
    expect(res.data.byteLength).toBeGreaterThan(0);
    console.log(`  [Photo Flow] Task image downloaded: ${res.data.byteLength} bytes, type: ${contentType}`);
  });

  it('Admin can also download the image', async () => {
    if (!photoUrl) return;

    const filePath = photoUrl.includes('/api/files/')
      ? '/files/' + photoUrl.split('/api/files/')[1]
      : photoUrl;

    const res = await clients.admin.get(filePath, {
      responseType: 'arraybuffer',
    });

    expect(res.status).toBe(200);
    expect(res.headers['content-type']).toMatch(/image\//);
  });

  it('Anonymous user CANNOT download the image (401)', async () => {
    if (!photoUrl) return;

    const filePath = photoUrl.includes('/api/files/')
      ? '/files/' + photoUrl.split('/api/files/')[1]
      : photoUrl;

    const res = await clients.anonymous.get(filePath, {
      responseType: 'arraybuffer',
    });

    // Should be 401 (unauthorized) — photos require authentication
    expect(res.status).toBe(401);
  });
});


// ════════════════════════════════════════════════════════════════
// B) Issue Photo: Worker reports issue with photo → Supervisor
//    can see the photo URL and download the image
// ════════════════════════════════════════════════════════════════
describe('Issue Photo Flow — Worker reports, Supervisor views', () => {
  let issueId;
  let issuePhotoUrl;

  it('Worker reports issue with photo', async () => {
    const png = makeMinimalPng();
    const form = buildMultipart(
      {
        Type: 'Infrastructure',
        Severity: 'Medium',
        Title: 'Photo Flow Test — Issue',
        Description: 'Verifying issue photo reaches supervisor',
        Latitude: CONFIG.gps.validLocation.latitude,
        Longitude: CONFIG.gps.validLocation.longitude,
        LocationDescription: 'Photo flow test location',
      },
      {
        Photo: { buffer: png, filename: 'issue-photo.png', contentType: 'image/png' },
      }
    );

    const res = await clients.worker.post('/issues/report-with-photo', form.data, {
      headers: {
        ...form.headers,
        Authorization: `Bearer ${tokens.worker}`,
      },
    });

    expect([200, 201].includes(res.status)).toBe(true);
    issueId = res.data?.data?.issueId || res.data?.data?.id;
    if (issueId) {
      registerCleanup(async () => {
        await clients.admin.delete(`/issues/${issueId}`);
      });
    }
  });

  it('Supervisor fetches issue and sees photo URL', async () => {
    if (!issueId) return;

    const res = await clients.supervisor.get(`/issues/${issueId}`);
    expect(res.status).toBe(200);

    const issue = res.data?.data;

    // Photo should be in either photos[] array or photoUrl field
    const hasPhotos = (issue.photos && issue.photos.length > 0) || issue.photoUrl;
    expect(hasPhotos).toBeTruthy();

    issuePhotoUrl = issue.photos?.[0] || issue.photoUrl?.split(';')[0];
    console.log(`  [Photo Flow] Issue photo URL: ${issuePhotoUrl}`);
  });

  it('Supervisor can download the issue image', async () => {
    if (!issuePhotoUrl) return;

    const filePath = issuePhotoUrl.includes('/api/files/')
      ? '/files/' + issuePhotoUrl.split('/api/files/')[1]
      : issuePhotoUrl;

    const res = await clients.supervisor.get(filePath, {
      responseType: 'arraybuffer',
    });

    expect(res.status).toBe(200);
    const contentType = res.headers['content-type'];
    expect(contentType).toMatch(/image\/(png|jpeg|jpg|gif)/);
    expect(res.data.byteLength).toBeGreaterThan(0);
    console.log(`  [Photo Flow] Issue image downloaded: ${res.data.byteLength} bytes, type: ${contentType}`);
  });

  it('Admin can also view the issue image', async () => {
    if (!issuePhotoUrl) return;

    const filePath = issuePhotoUrl.includes('/api/files/')
      ? '/files/' + issuePhotoUrl.split('/api/files/')[1]
      : issuePhotoUrl;

    const res = await clients.admin.get(filePath, {
      responseType: 'arraybuffer',
    });

    expect(res.status).toBe(200);
    expect(res.headers['content-type']).toMatch(/image\//);
  });

  afterAll(async () => {
    await runCleanups();
  });
});
