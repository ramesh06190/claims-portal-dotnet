// ASP.NET Web Forms page (.NET Framework 4.8). It needs IIS Express on Windows, so it only runs
// when WEBFORMS_URL is set, for example WEBFORMS_URL=http://localhost:5291/
import { test, expect } from "@playwright/test";
import { collectConsoleErrors, expectNoAxeViolations } from "./helpers.js";

const webFormsUrl = process.env.WEBFORMS_URL;

test.skip(!webFormsUrl, "Set WEBFORMS_URL to the running Web Forms site to run these tests");

let consoleErrors;

test.beforeEach(({ page }) => {
  consoleErrors = collectConsoleErrors(page);
});

test.afterEach(() => {
  expect(consoleErrors, "console errors, including content security policy violations").toEqual([]);
});

test("no WCAG 2.1 AA violations on first load, and the CSP is sent", async ({ page }) => {
  const response = await page.goto(webFormsUrl);
  expect(response?.headers()["content-security-policy"]).toContain("script-src 'self'");
  expect(response?.headers()["x-powered-by"]).toBeUndefined();
  await expectNoAxeViolations(page);
});

test("a bad claimant ID shows an error summary that takes focus", async ({ page }) => {
  await page.goto(webFormsUrl);
  await page.getByLabel("Claimant ID").fill("123");
  await page.getByRole("button", { name: "Find claims" }).click();

  await expect(page.locator("#error-summary")).toBeFocused();
  await expect(page.getByLabel("Claimant ID")).toHaveAttribute("aria-invalid", "true");
  await page.getByRole("link", { name: /must be 8 digits/ }).click();
  await expect(page.getByLabel("Claimant ID")).toBeFocused();
  await expectNoAxeViolations(page);
});

test("a search moves focus to the results, and the grid has real table headers", async ({ page }) => {
  await page.goto(webFormsUrl);
  await page.getByLabel("Claimant ID").fill("12345678");
  await page.getByRole("button", { name: "Find claims" }).click();

  await expect(page.locator("#results-title")).toBeFocused();
  await expect(page.locator("table thead th[scope=col]")).toHaveCount(4);
  await expectNoAxeViolations(page);
});
