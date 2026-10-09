# Continuous integration

The CI files in this directory are centrally managed from `MBW.Tool.GithubConfig`. Changes made directly to managed files may be overwritten the next time standard content is applied. Repository-specific behavior is normally kept in local composite actions; specialized repositories may instead receive a purpose-built managed workflow.

The files listed below may be present depending on the repository profile. Build actions may be centrally managed or maintained by the repository, while deploy workflows may be centrally managed no-ops or repository-specific post-release implementations.

- `workflows/ci.yml` — orchestrates the repository's managed validation, build, publishing, and release work.
- `workflows/deploy.yml` — may perform repository-specific work after a successful SemVer release.
- `actions/initialize_ci/action.yml` — may resolve the ref, version, and eligible publishing target once.
- `actions/build/action.yml` — may build, test, and produce artifacts; this file may be centrally managed.
- `actions/collect_publishables/action.yml` — may detect packages, Dockerfiles, and release assets.
- `actions/publish_nuget/action.yml` — may publish collected NuGet packages.
- `actions/publish_docker/action.yml` — may build and publish discovered Docker images.
- `actions/publish_github_release/action.yml` — may create or update a GitHub Release and upload its assets.

## NuGet retention

After successful NuGet publication, the final CI job prunes the package IDs
produced by that run. It waits for publishing, GitHub Release, and deployment
jobs to finish, including failed or skipped downstream jobs. It never runs on a
schedule and skips cancelled runs or unsuccessful package publication.

The newest three prerelease versions and three stable versions are retained
independently, by publication date. A hyphen in the version identifies a
prerelease. Repositories may set a positive integer `nugetVersionsToKeep` in
`repos.json`; Terraform provisions it as `NUGET_VERSIONS_TO_KEEP`, which applies
to each group separately. Without an override, each group retains three.

Cleanup is best effort: errors remain visible but do not fail CI, and failure
in one group does not prevent the other group or other packages from running.
The pinned deletion action paginates versions and deletes at most 100 versions
per group per invocation; subsequent publishing runs catch up. The repository
must have Admin access under each package's Manage Actions access settings.
This policy affects GitHub Packages only, not NuGet.org or workflow artifacts.
Custom active workflows must integrate the janitor separately; distributing a
disabled generic workflow does not activate cleanup.
