---
name: jira-ticket-attachments
description: How to read everything on a Jira ticket yourself before asking the user for it - download attached screenshots and files through the Atlassian MCP, and render Adobe XD or other design links with headless Chromium. Use whenever a ticket has attachments, images or a "Mock-up link". Never ask the user to attach or paste what is already on the ticket.
---

# Read the whole ticket yourself

Attachments, embedded screenshots and mock-up links are part of the ticket. Fetch and look at them
before planning; do not ask the user to paste or re-attach them. Everything below is read-only, so
the `jira-updates` approval rules do not apply to it.

(Added 2026-10-29 after BO-128. I told the user the Atlassian tools could not download attachments.
That was wrong: I had never run `discover` for them. The user had to correct me.)

## 1. Attachments (screenshots, PDFs, files)

1. `getJiraIssue` with `fields: ["description", "attachment", ...]`. Note each attachment's numeric
   `id`, `filename` and `size`. Images embedded in the description as `blob:` URLs are these same
   attachments; match them by filename and never try to open the blob.
2. `discover` with "download a jira issue attachment". It returns `downloadJiraIssueAttachment`, run
   through `executeRead` with the site `cloudId` (in `context.cloudId` of any `getJiraIssue` result)
   and `inputs: { attachmentId, outputPath }`.
3. The result holds a short-lived `downloadCommand` (a `curl` with a signed token, valid for minutes).
   Run it at once with the Bash tool. Do not echo the URL into a file, a log or a commit.
   - Save into a NEW, empty directory under the session scratchpad, never into the repo.
   - Use forward slashes in `--output` (`C:/Users/.../scratchpad/<dir>/x.png`) and add `-sSfL`;
     backslashes get collapsed by the shell.
   - Check the byte size against the ticket's `size`, and run `file` on it.
4. View images with the Read tool.

## 2. Mock-up and design links

- **Adobe XD** (`xd.adobe.com/view/...`): a JavaScript shell. `WebFetch` returns only "Adobe XD.".
  Render it with Playwright in a scratchpad console project (no browser install needed, the
  Chromium builds are already in `%LOCALAPPDATA%\ms-playwright`):

  ```
  dotnet new console -n xdshot -o <scratchpad>/xdshot
  dotnet add package Microsoft.Playwright --version 1.49.0
  XD_URL=<link> XD_OUT=<scratchpad>/xd dotnet run
  ```

  Pass inputs as environment variables, not arguments (`dotnet run -- args` can shift them).
  `Program.cs` that walks every screen of the prototype and screenshots each one:

  ```csharp
  using System.Text.RegularExpressions;
  using Microsoft.Playwright;
  var url = Environment.GetEnvironmentVariable("XD_URL")!;
  var outDir = Environment.GetEnvironmentVariable("XD_OUT")!;
  Directory.CreateDirectory(outDir);
  using var pw = await Playwright.CreateAsync();
  await using var browser = await pw.Chromium.LaunchAsync(new() { Headless = true });
  var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1920, Height = 1080 } });
  await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });
  await page.WaitForTimeoutAsync(5000);
  await page.Mouse.ClickAsync(1807, 248);   // dismiss the Specs Mode popup
  await page.Mouse.ClickAsync(699, 1039);   // home icon: first screen
  await page.WaitForTimeoutAsync(3000);
  var seen = new HashSet<int>();
  for (var i = 0; i < 200; i++)
  {
      var text = await page.EvaluateAsync<string>("document.body.innerText");
      var name = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).Skip(1).FirstOrDefault();
      var m = Regex.Match(text, @"(\d+) of (\d+)");
      var idx = m.Success ? int.Parse(m.Groups[1].Value) : -1;
      Console.WriteLine($"{idx,3} | {name}");
      if (idx < 0 || !seen.Add(idx)) break;   // wrapped around
      await page.ScreenshotAsync(new() { Path = Path.Combine(outDir, $"screen-{idx:D2}.png") });
      await page.Mouse.ClickAsync(804, 1039);   // next screen
      await page.WaitForTimeoutAsync(1800);
  }
  ```

  The click coordinates are for the 1920x1080 viewport only. The screen names printed by the walk
  are the index: read the screenshots whose names match the ticket, not all of them.
- **Figma** links: use the figma MCP (`get_design_context`, `get_screenshot`), not this recipe.
- **Confluence** pages and attachments: `getConfluenceContent` and `downloadConfluenceAttachment`
  (same `discover` then `executeRead` route).

## 3. Read what you fetched critically

- A screenshot on a ticket can be a stale baseline. BO-128's three attachments were 2023 captures
  of the page as it already existed (the filenames carry the date), not the new design.
  Compare them with the current code before treating them as the target.
- Check that the mock-up shows what the ticket text claims. BO-128 asks for Matches and Leagues; no
  screen of its 63-screen XD prototype shows a Match. When the text and the design disagree, report
  the mismatch with the screens as evidence and ask which one is right. Do not pick silently.
- Say which screens or files you looked at and which you skipped, so nobody assumes full coverage.
