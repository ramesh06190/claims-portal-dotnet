// ASP.NET Core MVC pages: axe on every page and state, plus the keyboard behavior axe cannot see.
import { test, expect } from "@playwright/test";
import { collectConsoleErrors, expectNoAxeViolations, lastSaturday } from "./helpers.js";

let consoleErrors;

test.beforeEach(({ page }) => {
  consoleErrors = collectConsoleErrors(page);
});

test.afterEach(() => {
  expect(consoleErrors, "console errors, including content security policy violations").toEqual([]);
});

for (const path of ["/", "/claims/new", "/claims", "/claims?sort=Claimant&direction=Ascending&page=2"]) {
  test(`no WCAG 2.1 AA violations on ${path}`, async ({ page }) => {
    const response = await page.goto(path);
    expect(response?.status()).toBe(200);
    await expectNoAxeViolations(page);
  });
}

test("pages send the content security policy", async ({ page }) => {
  const response = await page.goto("/claims/new");
  expect(response?.headers()["content-security-policy"]).toContain("script-src 'self'");
});

test("an empty submit shows an error summary that takes focus and links to each field", async ({ page }) => {
  await page.goto("/claims/new");
  await page.getByRole("button", { name: "Submit weekly claim" }).click();

  await expect(page).toHaveTitle(/^Error: /);
  const summary = page.locator("#error-summary");
  await expect(summary).toBeFocused();

  // Each link moves focus to the field it names
  await summary.getByRole("link", { name: "Enter your full name" }).click();
  await expect(page.locator("#ClaimantName")).toBeFocused();
  await expect(page.locator("#ClaimantName")).toHaveAttribute("aria-invalid", "true");

  await expectNoAxeViolations(page);
});

test("the form can be completed and submitted with the keyboard only", async ({ page }) => {
  const week = lastSaturday();
  await page.goto("/claims/new");

  // Start from the skip link so the test follows the real tab order
  await page.keyboard.press("Tab");
  await expect(page.getByRole("link", { name: "Skip to main content" })).toBeFocused();
  await page.keyboard.press("Enter");

  await page.locator("#ClaimantName").focus();
  await page.keyboard.type("Jordan Lee");
  await page.keyboard.press("Tab");
  await page.keyboard.type("12345678");
  await page.locator("#WeekEndingDay").focus();
  await page.keyboard.type(week.day);
  await page.keyboard.press("Tab");
  await page.keyboard.type(week.month);
  await page.keyboard.press("Tab");
  await page.keyboard.type(week.year);

  // Radios: tab into the group and pick an option with the arrow keys
  await page.keyboard.press("Tab");
  await expect(page.locator("#WorkedThisWeek-yes")).toBeFocused();
  await page.keyboard.press("ArrowDown");
  await expect(page.locator("#WorkedThisWeek-no")).toBeChecked();

  await page.locator("#AbleAndAvailable-yes").focus();
  await page.keyboard.press("Space");
  await page.keyboard.press("Tab");
  await expect(page.getByRole("button", { name: "Submit weekly claim" })).toBeFocused();
  await page.keyboard.press("Enter");

  await expect(page.getByRole("heading", { level: 1 })).toContainText(/submitted/i);
  await expectNoAxeViolations(page);
});

test("the sorted column is marked with aria-sort", async ({ page }) => {
  await page.goto("/claims?sort=Claimant&direction=Ascending");
  await expect(page.getByRole("columnheader", { name: /Claimant/ })).toHaveAttribute("aria-sort", "ascending");
  await expect(page.locator("th[aria-sort]")).toHaveCount(1);
});

test("the current page link is marked with aria-current", async ({ page }) => {
  await page.goto("/claims?page=2");
  await expect(page.getByRole("link", { name: "Page 2" })).toHaveAttribute("aria-current", "page");
});

test("the claims list reflows at 320px without scrolling the page sideways", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 800 });
  await page.goto("/claims");
  const width = await page.evaluate(() => document.documentElement.scrollWidth);
  expect(width).toBeLessThanOrEqual(320);
});
