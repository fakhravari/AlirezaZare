import {defineConfig, devices} from '@playwright/test';

export default defineConfig({
  testDir: './tests/browser',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: [['list'], ['html', {open: 'never'}]],
  use: {
    baseURL: 'http://127.0.0.1:5198',
    reducedMotion: 'reduce',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {name: 'desktop', use: {...devices['Desktop Chrome'], viewport: {width: 1440, height: 1000}}},
    {name: 'mobile', use: {...devices['Pixel 7'], viewport: {width: 360, height: 800}}},
  ],
  webServer: {
    command: 'dotnet run --no-build --project Shop/Shop.csproj --no-launch-profile --urls http://127.0.0.1:5198',
    url: 'http://127.0.0.1:5198',
    reuseExistingServer: !process.env.CI,
    env: {ASPNETCORE_ENVIRONMENT: 'Development'},
    timeout: 60000,
  },
});
