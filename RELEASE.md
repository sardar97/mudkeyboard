# Releasing MudKeyboard

Two artifacts ship from this repo, on **completely separate triggers** so a docs change never
publishes a package and a package release never forces a docs rebuild.

| Artifact          | Trigger                     | Pipeline                                   | Result                                   |
| ----------------- | --------------------------- | ------------------------------------------ | ---------------------------------------- |
| **NuGet package** | push a `v*` **tag**         | `.github/workflows/publish.yml`            | package on nuget.org + a GitHub Release  |
| **Docs website**  | push to the **`docs`** branch | Cloudflare Pages → `build.sh`            | live site at the docs domain             |
| _(every commit)_  | push / PR to **`master`**   | `.github/workflows/ci.yml`                 | build + test gate only (no deploy/publish) |

## Branch & tag model

- **`master`** — the development branch. CI builds and tests the whole solution on every push and
  PR. It never deploys or publishes.
- **`docs`** — a *deploy-only* branch that mirrors what is live on the docs site. You never commit
  to it directly; you fast-forward it from `master` when you want to publish docs. Cloudflare Pages
  watches this branch.
- **`v*` tags** (`v1.2.3`, `v1.1.0-beta.1`, …) — each tag is exactly one NuGet release. The tag
  string **is** the published version (it's passed to `dotnet pack` as `-p:Version`).

---

## One-time setup

Do these once. After that, releasing is just the per-release steps further down.

### 1. Create the `docs` branch and push it

```bash
git switch master
git pull
git switch -c docs
git push -u origin docs
```

### 2. Point Cloudflare Pages at the `docs` branch

In the Cloudflare dashboard → your Pages project → **Settings**:

- **Builds & deployments → Production branch:** `docs`
- **Build configuration:**
  - Build command: `bash build.sh`
  - Build output directory: `publish/wwwroot`
  - Root directory: `/` (leave blank/default)
- **Branch control / non-production deployments:** set automatic deployments to the **production
  branch only** (disable preview builds). This is what stops a library-only push to `master` from
  triggering a docs rebuild.

No GitHub secrets are needed for docs — Cloudflare pulls from GitHub directly and `build.sh`
installs .NET + the `wasm-tools` workload itself.

### 3. Add the NuGet API key secret

GitHub repo → **Settings → Secrets and variables → Actions → New repository secret**:

- **Name:** `NUGET_API_KEY`
- **Value:** an API key from nuget.org with push rights for the `MudKeyboard` package.

`GITHUB_TOKEN` (used to create the GitHub Release) is provided automatically — no setup needed.

---

## Releasing the docs website

The docs site redeploys whenever the `docs` branch moves. To publish the current state of
`master`:

```bash
git fetch origin
git push origin origin/master:docs      # fast-forward docs to master's latest commit
```

Or, if you keep a local `docs` branch checked out:

```bash
git switch docs
git merge --ff-only master
git push
git switch master
```

Cloudflare detects the push, runs `build.sh` (installs .NET, adds the `wasm-tools` workload,
publishes the WASM site with AOT) and deploys `publish/wwwroot`. Watch progress in Cloudflare →
**Deployments**. A build takes a few minutes because of AOT.

**Notes**

- Treat `docs` as deploy-only — never commit to it directly. Always fast-forward it from `master`
  so the push stays a clean fast-forward and the branch can't diverge.
- Docs and the package are independent: redeploy docs any time without cutting a package release,
  and publish a package without touching docs.

---

## Releasing the NuGet package

Versioning follows [SemVer](https://semver.org). The git tag is the source of truth for the
published version.

1. **Pick the version**, e.g. `1.1.0`. Pre-releases use a hyphen: `1.1.0-beta.1`, `1.2.0-rc.1`
   (these are flagged as *pre-release* on GitHub automatically).

2. **Update `CHANGELOG.md`.** Move the items under `## [Unreleased]` into a new
   `## [1.1.0] — YYYY-MM-DD` section, and add the compare-link line at the bottom of the file.
   The GitHub Release notes are pulled verbatim from this version's section.

   Mirror the same change in `src/MudKeyboard.Docs/Shared/ReleaseNotes.cs` — turn the `Unreleased`
   entry into a dated `ReleaseNote` (status `Latest`, demote the previous latest to `Stable`). This is
   what the docs site's **Releases & changelog** page (`/releases`) renders.

3. **Bump `<Version>` in `src/MudKeyboard/MudKeyboard.csproj`** to match. The tag overrides this in
   CI, but keeping them equal means a local `dotnet pack` produces the same version and avoids
   confusion. (The docs footer and Releases page read this version from the built assembly, so they
   update automatically once it's bumped.)

4. **Commit to `master` and let CI pass:**

   ```bash
   git add CHANGELOG.md src/MudKeyboard/MudKeyboard.csproj
   git commit -m "release: v1.1.0"
   git push
   ```

   Wait for the CI run on `master` to go green.

5. **Tag and push the tag:**

   ```bash
   git tag -a v1.1.0 -m "v1.1.0"
   git push origin v1.1.0
   ```

That triggers `publish.yml`, which:

- restores, builds and tests the library,
- packs it with the tag's version,
- pushes the `.nupkg` to nuget.org (`--skip-duplicate`, so re-runs are safe),
- creates a **GitHub Release** for the tag with notes from `CHANGELOG.md`.

**Verify:** `https://www.nuget.org/packages/MudKeyboard` shows the new version (indexing can take a
few minutes) and the repo's **Releases** page shows `v1.1.0`.

### If something goes wrong

- **NuGet versions are immutable** — you can't overwrite `1.1.0`. If a bad package ships, *unlist*
  it on nuget.org and release `1.1.1`.
- The push uses `--skip-duplicate`, so **re-running the failed workflow** (Actions → *Re-run jobs*)
  after a transient error won't fail on an already-pushed version, and the release step updates the
  existing release rather than erroring.
- To **drop a mistaken tag** before it has published cleanly:

  ```bash
  git push origin :refs/tags/v1.1.0   # delete the remote tag
  git tag -d v1.1.0                   # delete it locally
  ```

  Do this quickly — once the package is live on nuget.org you must bump the version instead.

---

## Cutting a full release (library + docs together)

The usual flow when a feature is ready to ship:

1. Merge the work into `master`; CI is green.
2. **Package:** update `CHANGELOG.md` + `<Version>`, commit, then tag `vX.Y.Z` and push the tag.
   → publishes to NuGet and creates the GitHub Release.
3. **Docs:** `git push origin origin/master:docs`.
   → Cloudflare redeploys the site, now documenting the just-released version.

Steps 2 and 3 are independent — do either, both, or neither, in any order.
