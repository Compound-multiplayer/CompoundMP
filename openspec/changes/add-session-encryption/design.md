# Design: Add session encryption

## Options
- Use `System.Security.Cryptography` only (available in net472): HMAC-SHA256 for the challenge, AES-GCM is not in net472, so authenticate-then-encrypt with AES-CBC + HMAC, or authenticate only.
- Authentication-only is probably enough: the data is game poses, not secrets; what matters is that nobody can inject damage or kick players.
