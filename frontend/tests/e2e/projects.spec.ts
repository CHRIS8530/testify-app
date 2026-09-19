import { test, expect } from '@playwright/test';

test.describe('Projects Flow', () => {
  test('should create a new project', async ({ page }) => {
    // Login first
    await page.goto('http://localhost:5173/login');
    await page.fill('input[type="email"]', 'test@example.com');
    await page.fill('input[type="password"]', 'TestPassword123');
    await page.click('button[type="submit"]');
    await page.waitForNavigation();

    // Create project
    await page.click('button:has-text("Create Project")');
    await page.fill('input[placeholder*="Project Name"]', 'Test Project');
    await page.fill('textarea', 'Test Description');
    await page.click('button[type="submit"]');
    
    // Verify project created
    await expect(page.locator('text=Test Project')).toBeVisible();
  });

  test('should list projects', async ({ page }) => {
    await page.goto('http://localhost:5173/login');
    await page.fill('input[type="email"]', 'test@example.com');
    await page.fill('input[type="password"]', 'TestPassword123');
    await page.click('button[type="submit"]');
    await page.waitForNavigation();
    
    expect(page.url()).toContain('/projects');
    await expect(page.locator('h1:has-text("Projects")')).toBeVisible();
  });
});