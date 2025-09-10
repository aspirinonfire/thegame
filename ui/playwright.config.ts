import { defineConfig, devices } from '@playwright/test';

/**
 * Read environment variables from file.
 * https://github.com/motdotla/dotenv
 */
// import dotenv from 'dotenv';
// import path from 'path';
// dotenv.config({ path: path.resolve(__dirname, '.env') });

const DEV  = !!process.env.PW_DEV;          // set by `npm run teste2edev`
const PORT = DEV ? 3000 : 3030;             // 3030 = your preview port

const appEnvVars = {
  VITE_GOOGLE_CLIENT_ID: "pw-test-client-id",
  VITE_API_URL: "http://localhost-pw"
}

const webServer = DEV ?
  {
    command: 'react-router dev --port 3000',
    url:     'http://localhost:3000',
    reuseExistingServer: true,
    timeout: 30_000,
    env: appEnvVars
  } :
  {
    command: 'npm run prodpreview',
    url: 'http://localhost:3030',
    timeout: 120_000,
    reuseExistingServer: !process.env.CI,
    env: appEnvVars
  }

/**
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig({
  testDir: './tests',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: 'html',
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    baseURL: `http://localhost:${PORT}`,

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
  },

  /* Run your local dev server before starting the tests */
  webServer: webServer,

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 7'] },
    }
  ]
});
