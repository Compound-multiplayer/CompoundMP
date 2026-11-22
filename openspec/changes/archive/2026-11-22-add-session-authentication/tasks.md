## 1. Implementation
- [x] `Auth` helpers (password key, nonces, proofs, session key, tag)
- [x] Tag and verify packets in `Transport`
- [x] Hello/Welcome handshake with proofs and protocol version
- [x] Reject downgrade and missing password
- [x] Unit tests: tagged accepted, untagged and wrong-key dropped, handshake exempt
- [x] Live test: host and five clients with a password, wrong password rejected
