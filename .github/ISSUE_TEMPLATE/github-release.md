---
name: GitHub release
about: Request a new GitHub release for this project
title: Create v[...]a1 Release
labels: needs triage
assignees: ''

---

This issue captures the mini "release process" for GitHub releases
(no need to edit):

- [ ] Create a PR to update the change log and the version numbers in the installing.md with the new tag for the release
- [ ] Download the artifacts from the CI of that PR ([Actions · qir-alliance/pyqir (github.com)](https://github.com/qir-alliance/pyqir/actions/workflows/ci.yml))
- [ ] Create a new release ([New release · qir-alliance/pyqir (github.com)](https://github.com/qir-alliance/pyqir/releases/new)) of these artifacts with the new tag, where the title should be the new tag, and the description should contain the section in the change log that was previously captured under "Unreleased". Set the pre-release tag.
- [ ] Merge the release PR (the one from the first task)
