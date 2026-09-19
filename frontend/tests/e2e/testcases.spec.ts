import { test, expect } from '@playwright/test';

test.describe('Test Cases Flow', () => {
  test('should create a test case', async ({ page }) => {
    // Login
    await page.goto('http://localhost:5173/login');
    await page.fill('input[type="email"]', 'test@example.com');
    await page.fill('input[type="password"]', 'TestPassword123');
    await page.click('button[type="submit"]');
    await page.waitForNavigation();

    // Navigate to project
    await page.click('text=Test Project');
    await page.waitForNavigation();

    // Click Test Cases tab
    await page.click('button:has-text("Test Cases")');

    // Click Add Test Case
    await page.click('button:has-text("Add Test Case")');

    // Fill form
    await page.fill('input[value=""]', 'Login Test Case');
    await page.fill('textarea', '1. Navigate to login\n2. Enter credentials\n3. Click login');
    await page.selectOption('select', 'High');
    
    // Submit
    await page.click('button:has-text("Save")');

    // Verify
    await expect(page.locator('text=Login Test Case')).toBeVisible();
  });
});