import {defineConfig} from '@playwright/test';
export default defineConfig({testDir:'./e2e',use:{baseURL:'http://127.0.0.1:8088',browserName:'chromium'},workers:1,reporter:'list',timeout:30000});
