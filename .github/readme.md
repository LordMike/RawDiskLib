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
