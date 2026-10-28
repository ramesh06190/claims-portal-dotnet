import AxeBuilder from "@axe-core/playwright";
import { expect } from "@playwright/test";

const WCAG_21_AA = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"];

/** Fails with a readable list if axe finds any WCAG 2.1 A or AA violation on the page. */
export async function expectNoAxeViolations(page) {
  const results = await new AxeBuilder({ page }).withTags(WCAG_21_AA).analyze();
  const summary = results.violations.map(
    (v) => `${v.id} (${v.impact}): ${v.help}\n  ${v.nodes.map((n) => n.target.join(" ")).join("\n  ")}`,
  );
  expect(summary, `axe violations on ${page.url()}`).toEqual([]);
}

/**
 * Collects console errors for the whole test. A blocked inline script shows up here as a
 * content security policy error, so an empty list also proves the policy and the page agree.
 */
export function collectConsoleErrors(page) {
  const errors = [];
  page.on("console", (message) => {
    if (message.type() === "error") errors.push(message.text());
  });
  page.on("pageerror", (error) => errors.push(error.message));
  return errors;
}

/** The most recent Saturday before today, since the claim form wants a week that has ended. */
export function lastSaturday() {
  const date = new Date();
  const daysBack = (date.getDay() + 1) % 7 || 7;
  date.setDate(date.getDate() - daysBack);
  return { day: String(date.getDate()), month: String(date.getMonth() + 1), year: String(date.getFullYear()) };
}
