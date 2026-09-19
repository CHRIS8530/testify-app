import { test, expect } from '@playwright/test';

test.describe('Defects Flow', () => {
  test('should create a defect', async ({ page }) => {
    // Login
    await page.goto('http://localhost:5173/login');
    await page.fill('input[type="email"]', 'test@example.com');
    await page.fill('input[type="password"]', 'TestPassword123');
    await page.click('button[type="submit"]');
    await page.waitForNavigation();

    // Navigate to project
    await page.click('text=Test Project');
    await page.waitForNavigation();

    // Click Defects tab
    await page.click('button:has-text("Defects")');

    // Click Create Defect
    await page.click('button:has-text("Create Defect")');

    // Fill form
    await page.fill('input[placeholder*="Title"]', 'Login button not working');
    await page.fill('textarea', 'Login button is not clickable on Chrome');
    await page.selectOption('select[name="severity"]', 'High');
    
    // Submit
    await page.click('button:has-text("Save")');

    // Verify
    await expect(page.locator('text=Login button not working')).toBeVisible();
  });
});