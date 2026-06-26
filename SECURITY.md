# Security Policy

![Pkl.Net Logo](assets/icon.jpg)

## Supported Versions

Only the latest release receives security patches.

| Version | Supported |
|---------|-----------|
| 1.0.x   | âœ… Yes    |
| < 1.0   | âŒ No     |

---

## Reporting a Vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

Report vulnerabilities privately by one of the following methods:

- **Email:** passaroweb@gmail.com  
  Subject: `[SECURITY] Pkl.Net â€” <short description>`
- **GitHub Private Advisory:** use the *"Report a vulnerability"* button on  
  <https://github.com/francescopaolopassaro/Pkl.Net/security/advisories/new>

Include as much detail as possible:

1. Affected component (`PklNet.Core`, `PklNet.Tools`, `PklNet.Extensions.Configuration`)
2. Version(s) affected
3. Description of the vulnerability and potential impact
4. Reproduction steps or proof-of-concept (if available)
5. Suggested fix (optional)

We aim to acknowledge reports within **72 hours** and to release a patch within **14 days** for confirmed high/critical issues.

---

## Security Considerations

### pkl binary process

Pkl.Net spawns the `pkl` CLI binary as a child process and communicates with it over stdin/stdout pipes. The following risks apply:

| Risk | Mitigation |
|------|-----------|
| Arbitrary `pkl` binary on PATH | Set `PKL_EXEC` to an absolute, trusted path in production. |
| Pkl modules fetched from `http://` | Use `AllowedModules` / `AllowedResources` in `EvaluatorOptions` to restrict which URIs Pkl may load. Default `Preconfigured()` allows `https:` but you should tighten this for untrusted input. |
| User-controlled Pkl text evaluation | Never evaluate untrusted, user-supplied Pkl text in a privileged context without sandboxing. |
| `prop:` and `env:` readers | External properties and environment variables are passed to the pkl process. Avoid exposing secrets via `EvaluatorOptions.Properties` or `Env`. |

### MessagePack

The built-in MsgPack decoder does not execute arbitrary code and does not deserialize into dynamic types by default. However:

- Deeply nested structures may cause excessive memory allocation. Set `EvaluatorOptions.Timeout` in production.
- Untrusted binary MsgPack bytes passed directly to `MsgPackReader` could cause `EndOfStreamException`; always validate input length before decoding from untrusted sources.

### ASP.NET Core Configuration

- `reloadOnChange: true` uses a `FileSystemWatcher`. Ensure the Pkl file path has appropriate filesystem ACLs so unprivileged users cannot replace it.
- Secrets (connection strings, API keys) should remain in `appsettings.secrets.json` (excluded by `.gitignore`) or environment variables, not inside committed `.pkl` files.

---

## Dependency Security

PklNet.Core has **zero external NuGet dependencies**. The only external surface is:

- `pkl` CLI binary â€” sourced from <https://pkl-lang.org> (Apple Inc.)
- `Microsoft.Extensions.Configuration` (â‰¥ 9.0) â€” used only by `PklNet.Extensions.Configuration`

---

## License & Attribution

Pkl.Net is released under the MIT License with Attribution Requirement.  
See [LICENSE](LICENSE) for details.

---

*Pkl.Net â€” Passaro Francesco Paolo 2026*  
<https://github.com/francescopaolopassaro/Pkl.Net>

