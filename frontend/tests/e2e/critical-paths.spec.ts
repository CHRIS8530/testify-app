import { test, expect } from '@playwright/test';
import {
  createProject,
  createRunWithCases,
  createTestCase,
  openProject,
  raiseDefect,
  registerViaApi,
  setResult,
  signInViaUi,
  tokenFor,
  uniqueUser,
} from './helpers';

test.describe('Critical paths', () => {
  test('a new user can register and lands on the projects page', async ({ page }) => {
    const user = uniqueUser();

    await page.goto('/register');
    await page.locator('#email').fill(user.email);
    await page.locator('#username').fill(user.username);
    await page.locator('#password').fill(user.password);
    await page.locator('#passwordConfirm').fill(user.password);
    await page.getByRole('button', { name: 'Sign Up' }).click();

    await expect(page).toHaveURL(/\/projects$/);
    await expect(page.getByRole('heading', { name: 'Projects', level: 1 })).toBeVisible();
    await expect(page.locator('.app-user-name')).toContainText(user.username);
  });

  test('a registered user can sign in', async ({ page, request }) => {
    const user = uniqueUser();
    await registerViaApi(request, user);

    await signInViaUi(page, user);

    await expect(page.locator('.app-user-name')).toContainText(user.username);
  });

  test('a wrong password is rejected with a clear message', async ({ page, request }) => {
    const user = uniqueUser();
    await registerViaApi(request, user);

    await page.goto('/login');
    await page.locator('#email').fill(user.email);
    await page.locator('#password').fill('Wrong!Passw0rd1');
    await page.getByRole('button', { name: 'Sign In' }).click();

    await expect(page.getByText('Invalid email or password')).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });

  test('a user can create a project', async ({ page, request }) => {
    const user = uniqueUser();
    await registerViaApi(request, user);
    await signInViaUi(page, user);
    const name = `Checkout ${Date.now()}`;

    await page.getByRole('button', { name: '+ Create project' }).first().click();
    await expect(page).toHaveURL(/\/projects\/new$/);
    await page.locator('#name').fill(name);
    await page.locator('#description').fill('Cart and payment tests');
    await page.getByRole('button', { name: 'Create project' }).click();

    await expect(page.getByRole('heading', { name, level: 1 })).toBeVisible();

    await page.getByRole('link', { name: /Projects/ }).click();
    await expect(page.getByRole('link', { name: new RegExp(name) })).toBeVisible();
    await expect(page.getByText('Owner', { exact: true })).toBeVisible();
  });

  test('a user can create a test case', async ({ page, request }) => {
    const user = uniqueUser();
    await registerViaApi(request, user);
    const token = await tokenFor(request, user);
    const projectName = `Project ${Date.now()}`;
    await createProject(request, token, projectName);
    await signInViaUi(page, user);
    await openProject(page, projectName);

    await page.getByRole('button', { name: 'Add Test Case' }).click();
    await page.locator('form input[type="text"]').fill('Login with valid credentials');
    await page.locator('form textarea').nth(1).fill('1. Open the login page\n2. Sign in');
    await page.locator('form textarea').nth(2).fill('The user is signed in');
    await page.locator('form select').selectOption('High');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByRole('cell', { name: 'Login with valid credentials' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'High' })).toBeVisible();
  });

  test('a user can execute a run and see the live pass rate', async ({ page, request }) => {
    const user = uniqueUser();
    await registerViaApi(request, user);
    const token = await tokenFor(request, user);
    const projectName = `Project ${Date.now()}`;
    const projectId = await createProject(request, token, projectName);
    const caseIds: string[] = [];
    for (const title of ['Case A', 'Case B', 'Case C']) {
      caseIds.push(await createTestCase(request, token, projectId, title));
    }
    await createRunWithCases(request, token, projectId, caseIds);

    await signInViaUi(page, user);
    await openProject(page, projectName);
    await page.getByRole('button', { name: 'Test Runs' }).click();
    await page.getByRole('link', { name: /Execute run/ }).click();

    const button = (title: string, outcome: string) =>
      page
        .locator('.result-card')
        .filter({ hasText: title })
        .getByRole('button', { name: outcome, exact: true });

    await button('Case A', 'Pass').click();
    await expect(button('Case A', 'Pass')).toHaveAttribute('aria-pressed', 'true');
    await button('Case B', 'Fail').click();
    await expect(button('Case B', 'Fail')).toHaveAttribute('aria-pressed', 'true');
    await button('Case C', 'Blocked').click();
    await expect(button('Case C', 'Blocked')).toHaveAttribute('aria-pressed', 'true');

    const summary = page.locator('.run-summary');
    await expect(summary).toContainText('33%');
    await expect(summary).toContainText('1 Passed');
    await expect(summary).toContainText('1 Failed');
    await expect(summary).toContainText('1 Blocked');

    await page.getByRole('button', { name: 'Complete run' }).click();
    await expect(page.locator('.status-badge')).toHaveText('Complete');
    await expect(page.getByRole('button', { name: 'Reopen run' })).toBeVisible();
  });

  test('a user can raise a defect from a failed result', async ({ page, request }) => {
    const user = uniqueUser();
    await registerViaApi(request, user);
    const token = await tokenFor(request, user);
    const projectName = `Project ${Date.now()}`;
    const projectId = await createProject(request, token, projectName);
    const caseId = await createTestCase(request, token, projectId, 'Login button');
    const { runId, results } = await createRunWithCases(request, token, projectId, [caseId]);
    await setResult(request, token, projectId, runId, results[0].id, 'Fail');

    await signInViaUi(page, user);
    await openProject(page, projectName);
    await page.getByRole('button', { name: 'Test Runs' }).click();
    await page.getByRole('link', { name: /Execute run/ }).click();

    await page.getByRole('button', { name: 'Raise defect' }).click();
    const form = page.locator('.defect-form');
    // The title is filled in from the test case.
    await expect(form.locator('input[type="text"]')).toHaveValue('Login button');
    await form.locator('input[type="text"]').fill('Login button does nothing');
    await form.locator('select').selectOption('High');
    await form.getByRole('button', { name: 'Raise defect' }).click();

    const chip = page.getByRole('link', { name: /Defect raised: Login button does nothing/ });
    await expect(chip).toBeVisible();
    await chip.click();

    await expect(page.getByText('Login button does nothing')).toBeVisible();
    await expect(page.locator('.severity-high')).toBeVisible();
    await expect(page.getByLabel(/Status of Login button does nothing/)).toHaveValue('Open');
  });

  test('the dashboard shows the pass rate and open defects by severity', async ({ page, request }) => {
    const user = uniqueUser();
    await registerViaApi(request, user);
    const token = await tokenFor(request, user);
    const projectName = `Project ${Date.now()}`;
    const projectId = await createProject(request, token, projectName);
    const caseA = await createTestCase(request, token, projectId, 'Case A');
    const caseB = await createTestCase(request, token, projectId, 'Case B');
    const { runId, results } = await createRunWithCases(request, token, projectId, [caseA, caseB]);
    const resultFor = (caseId: string) => results.find((r) => r.testCaseId === caseId)!.id;
    await setResult(request, token, projectId, runId, resultFor(caseA), 'Pass');
    await setResult(request, token, projectId, runId, resultFor(caseB), 'Fail');
    await raiseDefect(request, token, projectId, caseB, resultFor(caseB), 'Checkout crashes', 'Critical');

    await signInViaUi(page, user);
    await openProject(page, projectName);
    await page.getByRole('button', { name: 'Dashboard' }).click();

    const card = (label: string) => page.locator('.stat-card').filter({ hasText: label });
    await expect(card('Test cases')).toContainText('2');
    await expect(card('Current pass rate')).toContainText('50%');
    await expect(card('Open defects')).toContainText('1');
    await expect(page.locator('.sev-row').filter({ hasText: 'Critical' })).toContainText('1');
  });
});