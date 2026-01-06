const { defineConfig, devices } = require('@playwright/test');

module.exports = defineConfig({
    testDir: './tests',
    fullyParallel: false, // Run tests serially to avoid race conditions with inventory
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 1,
    workers: 1, // Single worker to ensure test order
    reporter: process.env.CI 
        ? [['junit', { outputFile: 'test-results/results.xml' }], ['html', { open: 'never' }]]
        : 'html',
    globalSetup: require.resolve('./global-setup.js'),
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
