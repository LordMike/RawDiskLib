# Continuous integration

The CI files in this directory are centrally managed from `MBW.Tool.GithubConfig`. Changes made directly to managed files may be overwritten the next time standard content is applied. Repository-specific behavior is kept in local composite actions so the repository can still describe how it builds and publishes without adding more event-triggered workflows.

The build action may be centrally managed for repositories using a standard build, or maintained by the repository when custom tooling is required. The deploy workflow may likewise be a centrally managed no-op or a repository-specific post-release implementation.

- `workflows/ci.yml` — orchestrates build, test, packaging, publishing, and release jobs in one workflow run.
- `workflows/deploy.yml` — performs optional repository-specific work after a successful SemVer release.
- `actions/initialize_ci/action.yml` — resolves the ref, version, and eligible publishing target once.
- `actions/build/action.yml` — builds, tests, and produces artifacts; this file may be centrally managed.
- `actions/collect_publishables/action.yml` — detects packages, Dockerfiles, and release assets.
- `actions/publish_nuget/action.yml` — publishes collected NuGet packages.
- `actions/publish_docker/action.yml` — builds and publishes discovered Docker images.
- `actions/publish_github_release/action.yml` — creates or updates a GitHub Release and uploads its assets.
