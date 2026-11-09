import {defineConfig, devices} from '@playwright/test';
export default defineConfig({testDir:'.', testMatch:'media-live.spec.mjs', reporter:'list', use:{baseURL:'http://127.0.0.1:5199', reducedMotion:'reduce'}, projects:[{name:'desktop',use:{...devices['Desktop Chrome'],viewport:{width:1440,height:1000}}},{name:'mobile',use:{...devices['Pixel 7'],viewport:{width:360,height:800}}}]});
