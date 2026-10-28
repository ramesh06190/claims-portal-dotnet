// Captures the README screenshots and the keyboard walkthrough video into docs/images.
// Start both apps first: the MVC app on http://127.0.0.1:5290 and Web Forms on http://localhost:5291.
// The video is turned into a GIF with ffmpeg, see the README.
import { chromium } from "@playwright/test";
import { mkdir, readdir, rename, rm } from "node:fs/promises";
import path from "node:path";
import { lastSaturday } from "./specs/helpers.js";

const MVC = process.env.BASE_URL ?? "http://127.0.0.1:5290";
const WEB_FORMS = process.env.WEBFORMS_URL ?? "http://localhost:5291/";
const OUT = path.resolve("../../docs/images");
const VIDEO_DIR = path.join(OUT, "video-tmp");

await mkdir(OUT, { recursive: true });
const browser = await chromium.launch();

async function shot(name, url, { colorScheme = "light", width = 1280, height = 860, fullPage = false, before } = {}) {
  const page = await browser.newPage({ colorScheme, viewport: { width, height }, deviceScaleFactor: 1 });
  await page.goto(url);
  if (before) await before(page);
  await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage });
  await page.close();
}

const submitEmpty = (page) => page.getByRole("button", { name: "Submit weekly claim" }).click();

await shot("mvc-error-summary", `${MVC}/claims/new`, { before: submitEmpty });
await shot("mvc-error-summary-dark", `${MVC}/claims/new`, { colorScheme: "dark", before: submitEmpty });
await shot("mvc-claims-list", `${MVC}/claims?sort=WeekEnding&direction=Descending`);
await shot("mvc-mobile-320", `${MVC}/claims/new`, { width: 320, height: 860, before: submitEmpty });
await shot("webforms-results", WEB_FORMS, {
  fullPage: true,
  before: async (page) => {
    await page.getByLabel("Claimant ID").fill("12345678");
    await page.getByRole("button", { name: "Find claims" }).click();
  },
});

// Keyboard only walkthrough, slowed down so the focus ring is easy to follow
await rm(VIDEO_DIR, { recursive: true, force: true });
const context = await browser.newContext({
  viewport: { width: 1000, height: 720 },
  recordVideo: { dir: VIDEO_DIR, size: { width: 1000, height: 720 } },
});
const page = await context.newPage();
const pause = (ms = 450) => page.waitForTimeout(ms);
const tab = async (times = 1) => {
  for (let i = 0; i < times; i++) {
    await page.keyboard.press("Tab");
    await pause(350);
  }
};
const type = (text) => page.keyboard.type(text, { delay: 70 });

await page.goto(`${MVC}/claims/new`);
await pause(800);
await tab();
await page.keyboard.press("Enter");
await pause();

// Submit with nothing filled in, to show the error summary taking focus
const submit = page.getByRole("button", { name: "Submit weekly claim" });
await submit.focus();
await pause();
await page.keyboard.press("Enter");
await page.waitForLoadState();
await pause(1600);

// Follow the first error link back to its field and fix the form
await tab();
await page.keyboard.press("Enter");
await pause();
const week = lastSaturday();
await type("Jordan Lee");
await tab();
await type("12345678");
await tab(4);
await type(week.day);
await tab();
await type(week.month);
await tab();
await type(week.year);
await tab();
await page.keyboard.press("ArrowDown");
await pause();
await tab(2);
await page.keyboard.press("Space");
await pause();
await tab();
await page.keyboard.press("Enter");
await page.waitForLoadState();
await pause(1800);

await context.close();
const [video] = await readdir(VIDEO_DIR);
await rename(path.join(VIDEO_DIR, video), path.join(OUT, "keyboard-walkthrough.webm"));
await rm(VIDEO_DIR, { recursive: true, force: true });
await browser.close();
console.log(`Saved to ${OUT}`);
