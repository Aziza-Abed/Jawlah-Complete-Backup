import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    testTimeout: 30000,
    hookTimeout: 15000,
    // Run files sequentially to avoid rate limiting on auth endpoints
    fileParallelism: false,
    sequence: { concurrent: false },
    reporters: ['verbose'],
    // Use a single thread so globalThis tokens are shared across all test files
    pool: 'threads',
    poolOptions: {
      threads: {
        singleThread: true,
      },
    },
  },
});
