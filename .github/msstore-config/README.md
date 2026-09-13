# Microsoft Store listings

`Listings` contains the localized, source-controlled fields that are merged into
the current draft through the Store Submission API. Dynamic submission fields,
package state, and existing listing images remain owned by Partner Center.

The initial configuration contains only `en-US`. Add a locale only after its
listing language and screenshots have been created in Partner Center, then add
the matching `Listings/<locale>.json` file and update `locales.json`.

Before running the workflow, configure the `microsoft-store` GitHub
environment with these secrets:

- `PARTNER_CENTER_CLIENT_ID`
- `PARTNER_CENTER_CLIENT_SECRET`
- `PARTNER_CENTER_SELLER_ID`
- `PARTNER_CENTER_TENANT_ID`

Also configure the environment variable `MICROSOFT_STORE_PRODUCT_ID` with the
RegistryEditor Product ID from Partner Center. The workflow intentionally has no
fallback Product ID.

The CD workflow first creates a StoreUpload MSIX package, validates the package
on x64 and arm64 runners, and stages the package and localized listing metadata
through the Microsoft Store Submission API. Set `submit` to true only when the
draft should be committed for certification.

Release notes are loaded from `../release-notes` using the first three
components of the manifest version. For version `1.0.0.0`, update
`../release-notes/1.0.0.md` before starting a Store submission.

