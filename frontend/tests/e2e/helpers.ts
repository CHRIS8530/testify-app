import { expect, type APIRequestContext, type Page } from '@playwright/test';

export const API_URL = process.env.E2E_API_URL ?? 'http://localhost:5137';
export const PASSWORD = 'Passw0rd!test';

export interface TestUser {
  email: string;
  username: string;
  password: string;
}

export interface RunResult {
  id: string;
  testCaseId: string;
}

// A user nobody else is using, so tests never depend on each other or on seeded data.
export function uniqueUser(): TestUser {
  const id = `${Date.now()}${Math.floor(Math.random() * 100000)}`;
  return { email: `e2e${id}@example.com`, username: `e2e${id}`, password: PASSWORD };
}

// ---- Setup through the API (fast and reliable). The behaviour under test is done in the browser. ----

export async function registerViaApi(request: APIRequestContext, user: TestUser) {
  const response = await request.post(`${API_URL}/api/v1/auth/register`, { data: user });
  expect(response.status()).toBe(201);
}

export async function tokenFor(request: APIRequestContext, user: TestUser): Promise<string> {
  const response = await request.post(`${API_URL}/api/v1/auth/login`, {
    data: { email: user.email, password: user.password },
  });
  expect(response.status()).toBe(200);
  const body = await response.json();
  return body.accessToken as string;
}

async function post(request: APIRequestContext, token: string, path: string, data?: unknown) {
  const response = await request.post(`${API_URL}${path}`, {
    headers: { Authorization: `Bearer ${token}` },
    data,
  });
  expect(response.ok(), `POST ${path} returned ${response.status()}`).toBeTruthy();
  return response.json();
}

export async function createProject(request: APIRequestContext, token: string, name: string) {
  const project = await post(request, token, '/api/v1/projects', {
    name,
    description: 'Created by an end to end test',
  });
  return project.id as string;
}

export async function createTestCase(
  request: APIRequestContext,
  token: string,
  projectId: string,
  title: string
) {
  const created = await post(request, token, `/api/v1/projects/${projectId}/test-cases`, {
    title,
    preconditions: '',
    steps: '1. Do the thing',
    expectedResult: 'It works',
    priority: 'High',
  });
  return created.id as string;
}

export async function createRunWithCases(
  request: APIRequestContext,
  token: string,
  projectId: string,
  caseIds: string[]
) {
  const run = await post(request, token, `/api/v1/projects/${projectId}/test-runs`);
  const results = (await post(
    request,
    token,
    `/api/v1/projects/${projectId}/test-runs/${run.id}/results`,
    { testCaseIds: caseIds }
  )) as RunResult[];
  return { runId: run.id as string, results };
}

export async function setResult(
  request: APIRequestContext,
  token: string,
  projectId: string,
  runId: string,
  resultId: string,
  result: string
) {
  const response = await request.patch(
    `${API_URL}/api/v1/projects/${projectId}/test-runs/${runId}/results/${resultId}`,
    { headers: { Authorization: `Bearer ${token}` }, data: { result, notes: '' } }
  );
  expect(response.ok(), `PATCH result returned ${response.status()}`).toBeTruthy();
}

export async function raiseDefect(
  request: APIRequestContext,
  token: string,
  projectId: string,
  testCaseId: string,
  testRunResultId: string,
  title: string,
  severity: string
) {
  await post(request, token, `/api/v1/projects/${projectId}/defects`, {
    testCaseId,
    testRunResultId,
    title,
    description: '',
    severity,
  });
}

// ---- Steps in the browser ----

export async function signInViaUi(page: Page, user: TestUser) {
  await page.goto('/login');
  await page.locator('#email').fill(user.email);
  await page.locator('#password').fill(user.password);
  await page.getByRole('button', { name: 'Sign In' }).click();
  await expect(page.getByRole('heading', { name: 'Projects', level: 1 })).toBeVisible();
}

export async function openProject(page: Page, name: string) {
  await page.getByRole('link', { name: new RegExp(name) }).click();
  await expect(page.getByRole('heading', { name, level: 1 })).toBeVisible();
}