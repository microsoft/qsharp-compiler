# summarize_documentation

**NOTICE**: This utility is temporarily disabled because of a dependency on PyYAML, which is vulnerable to [CVE-2020-14343](https://access.redhat.com/security/cve/cve-2020-14343).
`requirements-disabled.txt` should be renamed back to `requirements.txt` and this notice removed when the CVE is resolved.

---

This utility summarizes Markdown documentation gathered from one or more compilation units,
producing namespace and TOC files from the gathered documentation.

For example:

```bash
$ pip install -r requirements.txt
$ python obj/qsharp/docs/*.md obj/qsharp/docs
```
