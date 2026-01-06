const { defineConfig, devices } = require('@playwright/test');

module.exports = defineConfig({
    testDir: './tests',
    fullyParallel: true,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 1,
    workers: process.env.CI ? 1 : undefined,
    reporter: process.env.CI 
        ? [['junit', { outputFile: 'test-results/results.xml' }], ['html', { open: 'never' }]]
        : 'html',
    use: {
        baseURL: 'http://127.0.0.1:5002',
        trace: 'on',
        screenshot: 'on',
        video: 'on',
        headless: !!process.env.CI,
    },
    projects: [
        {
            name: 'chromium',
            use: { ...devices['Desktop Chrome'] },
        },
    ],
});
