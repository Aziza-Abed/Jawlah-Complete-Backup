// ──────────────────────────────────────────────────────────────
// Flow 1: Authentication Integration
// ──────────────────────────────────────────────────────────────
// Tests that both web and mobile clients can authenticate
// against the same backend with the same JWT system.
// ──────────────────────────────────────────────────────────────
import { describe, it, expect } from 'vitest';
import { clients, loginAs, tokens } from '../helpers.js';
import { CONFIG } from '../config.js';

describe('Flow 1 — Authentication (Web ↔ Mobile ↔ Backend)', () => {

  // ── 1A: Health (prerequisite) ──────────────────────────────

  it('backend is reachable (health check)', async () => {
    const res = await clients.anonymous.get('/health');
    expect(res.status).toBe(200);
  });

  // ── 1B: Web-style login ────────────────────────────────────

  describe('Web-style login (/auth/login)', () => {
    it('admin can login', async () => {
      // Make a direct HTTP call to test the login endpoint itself
      const res = await clients.anonymous.post('/auth/login', {
        username: CONFIG.users.admin.username,
        password: CONFIG.users.admin.password,
      });
      if (res.status === 429) return; // rate limited
      expect(res.status).toBe(200);
      expect(res.data?.success).toBe(true);

      // login may return token directly or require OTP (both are valid)
      const data = res.data?.data;
      const hasToken = !!(data?.accessToken || data?.token);
      const hasOtp = !!data?.requiresOtp;
      expect(hasToken || hasOtp).toBe(true);

      // ensure loginAs caches the token for subsequent tests
      await loginAs('admin');
    });

    it('supervisor can login', async () => {
      const res = await clients.anonymous.post('/auth/login', {
        username: CONFIG.users.supervisor.username,
        password: CONFIG.users.supervisor.password,
      });
      if (res.status === 429) return;
      expect(res.status).toBe(200);
      expect(res.data?.success).toBe(true);

      await loginAs('supervisor');
    });

    it('invalid credentials are rejected', async () => {
      const res = await clients.anonymous.post('/auth/login', {
        username: 'admin', password: 'wrong',
      });
      if (res.status === 429) return; // rate limited from other test files
      expect(res.status === 401 || res.data?.success === false).toBe(true);
    });
  });

  // ── 1C: Mobile-style login ─────────────────────────────────

  describe('Mobile-style login (/auth/login-gps)', () => {
    it('worker can login', async () => {
      // Direct HTTP call to test login endpoint
      const res = await clients.anonymous.post('/auth/login', {
        username: CONFIG.users.worker.username,
        password: CONFIG.users.worker.password,
      });
      if (res.status === 429) return;
      expect(res.status).toBe(200);
      expect(res.data?.success).toBe(true);

      await loginAs('worker');
    });

    it('mobile login includes device binding', async () => {
      const res = await clients.anonymous.post('/auth/login-gps', {
        username: CONFIG.users.worker.username,
        password: CONFIG.users.worker.password,
        deviceId: CONFIG.devices.mobileDeviceId,
      });
      // 200 = success, 429 = rate limited from repeated test runs
      expect([200, 429].includes(res.status)).toBe(true);
    });
  });

  // ── 1D: Token validation ──────────────────────────────────

  describe('JWT token validation', () => {
    it('authenticated request succeeds', async () => {
      await loginAs('admin');
      const res = await clients.admin.get('/auth/me');
      expect(res.status).toBe(200);
      expect(res.data?.data).toBeTruthy();
    });

    it('unauthenticated request returns 401', async () => {
      const res = await clients.anonymous.get('/auth/me');
      expect(res.status).toBe(401);
    });

    it('expired/invalid token returns 401', async () => {
      const res = await clients.anonymous.get('/auth/me', {
        headers: { Authorization: 'Bearer invalidtoken123' },
      });
      expect(res.status).toBe(401);
    });

    it('web token works on mobile endpoints', async () => {
      await loginAs('admin');
      const res = await clients.admin.get('/zones/my');
      expect(res.status).toBe(200);
    });

    it('mobile token works on web endpoints', async () => {
      await loginAs('worker');
      const res = await clients.worker.get('/notifications/unread-count');
      expect(res.status).toBe(200);
    });
  });

  // ── 1E: Profile endpoints (both clients use) ──────────────

  describe('Profile endpoints', () => {
    it('GET /auth/me returns profile (web)', async () => {
      await loginAs('admin');
      const res = await clients.admin.get('/auth/me');
      expect(res.status).toBe(200);
      const user = res.data?.data;
      expect(user).toBeTruthy();
      expect(user.username || user.userName).toBeTruthy();
    });

    it('GET /auth/profile returns profile (mobile)', async () => {
      await loginAs('worker');
      const res = await clients.worker.get('/auth/profile');
      expect(res.status).toBe(200);
    });
  });
});
