# DevScope Release Guide

This runbook is the source of truth for publishing `RolandUI.DevScope` to NuGet and creating the matching GitHub Release. It adopts AvaScope's release-gate principle, but it matches DevScope's simpler release workflow: publishing a GitHub Release triggers `.github/workflows/nuget-publish.yml`.

Publishing a GitHub Release is the irreversible step. Complete every preparation and validation step before changing a draft release to published.

## Release model

Every release must have:

- a SemVer package version and matching `v<version>` Git tag;
- a GitHub release-tracking issue with locked scope;
- all in-scope issues closed or explicitly deferred;
- a clean, pushed `main` commit that passed the full release gate;
- reviewed release notes;
- a successful GitHub Actions release run;
- a matching immutable NuGet package and GitHub Release assets.

Use prerelease versions such as `0.1.0-preview.1` until the public API is ready for a stable release. Never reuse a version that has reached NuGet.

## One-time repository setup

DevScope uses NuGet Trusted Publishing. It does not store a long-lived NuGet API key in GitHub. The workflow requests a GitHub OIDC token, and `NuGet/login@v1` exchanges that token once for a short-lived NuGet API key immediately before package publication.

Before the first release, create this policy under the RolandUI account on the nuget.org **Trusted Publishing** page:

| Field | Value |
| --- | --- |
| Policy Name | `DevScope GitHub Release` |
| Package Owner | `RolandUI` |
| Repository Owner | `RolandUI` |
| Repository | `DevScope` |
| Workflow File | `nuget-publish.yml` |
| Environment | Leave empty |

The workflow file field contains only the filename, not `.github/workflows/nuget-publish.yml`. Leave Environment empty because the release job does not declare a GitHub Actions environment.

The repository workflow must retain both permissions:

```yaml
permissions:
  contents: write
  id-token: write
```

`contents: write` allows release asset uploads. `id-token: write` allows GitHub to issue the OIDC token used by Trusted Publishing. No `NUGET_API_KEY` repository secret is required.

## 1. Define and lock the release

Create one release-tracking issue and record:

- target version and release type (`preview`, patch, minor, or major);
- included issues and explicit deferrals;
- compatibility targets (`net8.0`, `net10.0`, and Avalonia version);
- required documentation changes;
- validation results and final release commit.

Use variables for the remaining commands:

```powershell
$version = "<version>" # example: 0.1.0-preview.1
$tag = "v$version"
$repo = "RolandUI/DevScope"
```

Check that neither the tag nor a GitHub Release already exists:

```powershell
git ls-remote --tags origin "refs/tags/$tag"
gh release view $tag --repo $repo
```

`git ls-remote` must return no matching tag, and `gh release view` must report that the release was not found. The non-zero `gh release view` exit is expected at this stage.

## 2. Prepare the release candidate

Work from the exact commit that will be tagged:

```powershell
git switch main
git pull --ff-only origin main
git status --short --branch
```

The worktree must be clean, `main` must match `origin/main`, and all release-scope commits must already be pushed. The final release candidate must not contain unfinished feature work.

Review the release-facing metadata:

- package ID: `RolandUI.DevScope`;
- project and repository URLs: `https://github.com/RolandUI/DevScope`;
- license and preserved attribution;
- README installation and compatibility text;
- release notes, including breaking changes and known limitations.

## 3. Run the local release gate

Restore once, then validate both configurations and both target frameworks:

```powershell
dotnet restore DevScope.slnx
dotnet test DevScope.slnx --configuration Debug --no-restore
dotnet test DevScope.slnx --configuration Release --no-restore
dotnet build src/DevScope.csproj --configuration Release --no-restore
git diff --check
```

Create the same package shape as GitHub Actions, using the intended version:

```powershell
$out = Join-Path $env:TEMP "DevScope-release-$version-$([guid]::NewGuid().ToString('N'))"
dotnet pack src/DevScope.csproj `
  --configuration Release `
  --no-build `
  --no-restore `
  -p:PackageVersion=$version `
  -p:IncludeSymbols=true `
  -p:SymbolPackageFormat=snupkg `
  --output $out
```

The directory must contain exactly one `.nupkg` and its `.snupkg`. Inspect the primary package:

```powershell
$package = Get-Item (Join-Path $out "RolandUI.DevScope.$version.nupkg")
Get-FileHash $package.FullName -Algorithm SHA256
tar -tf $package.FullName
tar -xOf $package.FullName RolandUI.DevScope.nuspec
```

Verify at minimum:

- package ID and version are `RolandUI.DevScope` and `$version`;
- repository URL and commit point to the release candidate;
- dependencies use the intended Avalonia version;
- `README.md` is present;
- `lib/net8.0/RolandUI.DevScope.dll` and `lib/net10.0/RolandUI.DevScope.dll` are present;
- no unexpected files or legacy project identifiers are present.

Record the commands, test counts, package SHA-256, and release candidate commit in the release issue. Do not continue if any gate fails.

## 4. Create and review the draft release

Tag only the validated commit, then create a draft release:

```powershell
git tag -a $tag -m "DevScope $version"
git push origin "refs/tags/$tag"
gh release create $tag `
  --repo $repo `
  --draft `
  --verify-tag `
  --title "DevScope $version" `
  --generate-notes
```

Before publishing the draft, verify:

- the tag resolves to the recorded release candidate commit;
- the title, release notes, compatibility information, and known limitations are correct;
- the nuget.org Trusted Publishing policy is active and exactly matches the repository and workflow fields above;
- there is no existing NuGet version with the same number.

Publishing the draft starts the release workflow:

```powershell
gh release edit $tag --repo $repo --draft=false --latest
```

Do not run this command without explicit release authorization.

## 5. Monitor publication

The `Publish NuGet Package` workflow will:

1. check out the tagged source;
2. restore and build `src/DevScope.csproj` in Release mode;
3. pack `RolandUI.DevScope` using the tag as the package version;
4. attach `.nupkg` and `.snupkg` files to the GitHub Release;
5. exchange the GitHub OIDC token for a short-lived NuGet credential;
6. push the packages to nuget.org.

Find and watch the run:

```powershell
gh run list --repo $repo --workflow nuget-publish.yml --limit 5
gh run watch <run-id> --repo $repo --exit-status
```

If GitHub records the published release event but no run is created, use the workflow's tag-bound manual recovery trigger. It validates that the release is already published and checks out the immutable release tag rather than `main`:

```powershell
gh workflow run nuget-publish.yml --repo $repo --ref main -f tag=$tag
```

Do not use the manual trigger before the matching GitHub Release is published, and never use it to publish an untagged commit.

If the run fails, inspect it before retrying:

```powershell
gh run view <run-id> --repo $repo --log-failed
```

## 6. Verify the published release

Confirm the tag, release assets, and NuGet registration:

```powershell
git ls-remote --tags origin "refs/tags/$tag"
gh release view $tag --repo $repo --json url,tagName,isDraft,assets
$versions = Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/rolandui.devscope/index.json"
$versions.versions -contains $version
```

The final expression must return `True`. NuGet indexing can take a few minutes; retry verification before treating a successful workflow as failed.

Install the exact published version in a disposable sample or real consumer application and confirm that it restores for both supported target frameworks. Then update the release issue with:

- GitHub Release URL;
- workflow run URL;
- tag and commit hash;
- NuGet version;
- published package SHA-256;
- smoke-test result.

Close the release issue only after all publication checks pass.

## Failure and recovery rules

- If publication fails before NuGet accepts the package, correct the Trusted Publishing policy, OIDC permission, or workflow problem and rerun the failed workflow. The workflow uses `--skip-duplicate`, so a partial retry is safe.
- If the `release: published` event is recorded but does not create a run, use the documented `workflow_dispatch` recovery with the existing immutable release tag.
- If NuGet accepted the package, its version is immutable. Never overwrite, delete, move, or reuse that version; publish a new patch or prerelease version for corrections.
- If a bad package reaches NuGet, unlist it when appropriate and create a follow-up release. Deleting the GitHub Release does not remove the NuGet package.
- Do not move or recreate a public release tag after publication.
- Do not publish manually from a developer machine while the GitHub workflow is the documented release path.
