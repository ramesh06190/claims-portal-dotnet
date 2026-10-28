// Runs every spec twice, once with the light color scheme and once with dark, because the design
// system switches colors with prefers-color-scheme and contrast has to pass in both.
//
// The MVC app is started for you with `dotnet run`. It uses the "Claims" connection string from
// appsettings.json (LocalDB) unless ConnectionStrings__Claims is set, which is how CI points it at a
// SQL Server container. The Web Forms spec only runs when WEBFORMS_URL is set, because the page
// needs IIS Express on Windows.
import { defineConfig, devices } from "@playwright/test";

const PORT = 5290;

export default defineConfig({
  testDir: "specs",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  reporter: process.env.CI ? [["github"], ["list"], ["html", { open: "never" }]] : "list",
  use: {
    baseURL: process.env.BASE_URL ?? `http://127.0.0.1:${PORT}`,
    ...devices["Desktop Chrome"],
  },
  projects: [
    { name: "light", use: { colorScheme: "light" } },
    { name: "dark", use: { colorScheme: "dark" } },
  ],
  webServer: process.env.BASE_URL
    ? undefined
    : {
        command: `dotnet run --project ../../src/ClaimsPortal.Web --configuration Release --no-launch-profile --urls http://127.0.0.1:${PORT}`,
        env: { ASPNETCORE_ENVIRONMENT: "Development" },
        url: `http://127.0.0.1:${PORT}/`,
        timeout: 180_000,
        reuseExistingServer: !process.env.CI,
      },
});
