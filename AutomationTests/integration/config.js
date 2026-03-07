// ──────────────────────────────────────────────────────────────
// Integration Test Configuration
// ──────────────────────────────────────────────────────────────
// Adjust these values to match your local development environment.
// The backend must be running before tests are executed.
// ──────────────────────────────────────────────────────────────

export const CONFIG = {
  // Backend API base URL (must be running)
  API_BASE_URL: process.env.API_BASE_URL || 'http://localhost:5000/api',

  // SignalR hub URL
  SIGNALR_HUB_URL: process.env.SIGNALR_HUB_URL || 'http://localhost:5000/hubs/tracking',

  // Test user credentials (must match seed data in AlBirehSeedData.sql)
  users: {
    admin: {
      username: process.env.ADMIN_USER || 'admin',
      password: process.env.ADMIN_PASS || 'pass123@',
    },
    supervisor: {
      username: process.env.SUPERVISOR_USER || 'super1',
      password: process.env.SUPERVISOR_PASS || 'pass123@',
    },
    worker: {
      username: process.env.WORKER_USER || 'worker1',
      password: process.env.WORKER_PASS || 'pass123@',
    },
  },

  // Simulated GPS coordinates (Al-Bireh area)
  gps: {
    validLocation: { latitude: 31.9038, longitude: 35.2034 },
    officeLocation: { latitude: 31.9050, longitude: 35.2050 },
    farLocation: { latitude: 32.0000, longitude: 35.3000 },
  },

  // Test device IDs (simulating mobile and web)
  devices: {
    mobileDeviceId: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    webDeviceId: 'b2c3d4e5-f6a7-8901-bcde-f12345678901',
  },
};
