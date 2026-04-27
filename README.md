# My First App — Static Web App + Entra-Authenticated C# API

A minimal template proving the pipeline:
**Browser → Entra ID sign-in → Static Web App → authenticated C# Functions API → response**

No database yet. Once this works, swapping the API for a SQL query is a small change.

## What's in here

```
.
├── index.html                    Frontend. Plain HTML + JS, no framework.
├── staticwebapp.config.json      Tells Azure SWA to require login for everything.
└── api/                          C# Functions project (.NET 8 isolated worker).
    ├── api.csproj
    ├── host.json
    ├── local.settings.json       Local-only, gitignored.
    ├── Program.cs
    └── HelloFunction.cs          GET /api/hello → returns "Hello, {your name}!"
```

## Local testing (optional but recommended)

You need:
- .NET 8 SDK
- Azure Functions Core Tools v4: `npm i -g azure-functions-core-tools@4`
- Static Web Apps CLI: `npm i -g @azure/static-web-apps-cli`

Then from the repo root:

```
swa start . --api-location api
```

Open http://localhost:4280. The SWA CLI emulates the auth layer — there's a fake
login page where you can supply a username, and it'll inject the principal header
to your function just like production does.

## Deploying to Azure

1. **Push this repo to GitHub.** (Private repo is fine.)

2. **Create the Entra app registration** (you'll reuse your existing pattern here):
   - Entra admin center → App registrations → New registration
   - Name: `my-first-app`
   - Supported account types: *Accounts in this organizational directory only*
   - Redirect URI: leave blank for now; you'll add it after the SWA exists.
   - After creation, note the **Application (client) ID** and **Directory (tenant) ID**.
   - Certificates & secrets → New client secret → copy the value immediately.
   - API permissions: the defaults (User.Read) are fine for this step.

3. **Create the Static Web App:**
   - Azure Portal → Create resource → Static Web App
   - Plan: **Free** is fine for this test.
   - Deployment: GitHub, point it at your repo, branch `main`.
   - Build presets: **Custom**
     - App location: `/`
     - Api location: `api`
     - Output location: leave blank
   - Click create. Azure commits a GitHub Actions workflow to your repo and kicks
     off the first deploy. Takes ~3 minutes.

4. **Add the redirect URI to the app registration:**
   - Your SWA now has a URL like `https://nice-pebble-12345.azurestaticapps.net`.
   - In the app registration → Authentication → Add platform → Web
   - Redirect URI: `https://<your-swa-url>/.auth/login/aad/callback`
   - Check "ID tokens" under implicit grant.

5. **Tell the SWA about the app registration:**
   - SWA resource → Configuration → add two app settings:
     - `AAD_CLIENT_ID` = the Application (client) ID from step 2
     - `AAD_CLIENT_SECRET` = the client secret value from step 2
   - Save.

6. **Edit `staticwebapp.config.json`** and replace `YOUR_TENANT_ID` with your
   actual tenant ID. Commit and push — the deploy will trigger automatically.

7. **Visit the SWA URL.** You should be redirected to your org's login page,
   sign in, and see the greeting with your name.

## If you want to restrict access to a specific group

By default, anyone in your tenant can sign in. To restrict:

- In the Entra app registration → Enterprise applications → find `my-first-app`
- Properties → set **Assignment required** to Yes
- Users and groups → Add the security group you want to allow

Now only members of that group can load the app at all. This is the same pattern
you likely used for the MAUI app.

## Cost

Static Web Apps Free tier covers this entirely — no charge for hosting,
the bundled Functions, or bandwidth at this scale. Upgrading to Standard
(~$9/month) is needed later for things like bringing your own Functions app
or private endpoints, but for Step 1 there's nothing to pay.
