import { test, expect } from '@playwright/test';

test.describe('Auth Flow', () => {
  const testEmail = `test${Date.now()}@example.com`;
  const testPassword = 'TestPassword123';
  const testUsername = `testuser${Date.now()}`;

  test('should register a new user', async ({ page }) => {
    await page.goto('http://localhost:5173/register');
    await page.fill('input[type="email"]', testEmail);
    await page.fill('input#username', testUsername);
    await page.fill('input#password', testPassword);
    await page.fill('input#passwordConfirm', testPassword);
    await page.click('button[type="submit"]');
    await page.waitForNavigation();
    expect(page.url()).toContain('/projects');
  });

  test('should login with registered user', async ({ page }) => {
    await page.goto('http://localhost:5173/login');
    await page.fill('input[type="email"]', testEmail);
    await page.fill('input[type="password"]', testPassword);
    await page.click('button[type="submit"]');
    await page.waitForNavigation();
    expect(page.url()).toContain('/projects');
  });
});