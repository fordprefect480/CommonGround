# Changelog

## [0.8.0](https://github.com/fordprefect480/CommonGround/compare/v0.7.0...v0.8.0) (2026-06-22)


### Features

* let admins assign a bed to an existing member directly ([68de72d](https://github.com/fordprefect480/CommonGround/commit/68de72df6a159d232d759d8d33920778661dff81))
* record manual payments with an editable amount and show payment method ([2d418f6](https://github.com/fordprefect480/CommonGround/commit/2d418f6198dc4ec0fcc0733d9e53c925784e3a26))
* show a leased beds card on the admin member details page ([150848b](https://github.com/fordprefect480/CommonGround/commit/150848bc04246af1c1da663eea794675255a60f3))


### Bug Fixes

* guard against double-booking a member and surface member-load failures ([9371ee9](https://github.com/fordprefect480/CommonGround/commit/9371ee97e9e099a42593c253ec7947d58ba7fa0c))
* label membership cards with the financial year actually paid through ([112b705](https://github.com/fordprefect480/CommonGround/commit/112b7052505f9deec49fce33e9e8856b0ed46d73))
* refresh payment history after recording a payment, and show the time ([b0a568a](https://github.com/fordprefect480/CommonGround/commit/b0a568a9fb7e0347d6a373ca31818c52a02ca5f9))
* scroll to the top of the page on navigation ([74f5be1](https://github.com/fordprefect480/CommonGround/commit/74f5be1caaded8f457ed6446a13eb08fe72981a4))
* space out the amount field and button in the manual payment modal ([928d55a](https://github.com/fordprefect480/CommonGround/commit/928d55a5274d02213337117e94e5e02c949bde35))
* stop route-param-only action endpoints returning 415 on bodyless POST ([88e20f8](https://github.com/fordprefect480/CommonGround/commit/88e20f842bbb1f208304b2fba2dbfe620ce29393))

## [0.7.0](https://github.com/fordprefect480/CommonGround/compare/v0.6.0...v0.7.0) (2026-06-22)


### Features

* let admins edit a freetext note on each leased bed ([7665345](https://github.com/fordprefect480/CommonGround/commit/766534551e0cd33d28a97af355032c313a7b60cc))


### Bug Fixes

* return empty 200 from Stripe webhook instead of serializing the CancellationToken ([5e689d9](https://github.com/fordprefect480/CommonGround/commit/5e689d9fad7f60ec28a2adebb900987395281bf2))
* serve SPA fallback so deep links like the Stripe redirect don't 404 ([e665aa8](https://github.com/fordprefect480/CommonGround/commit/e665aa821133b1249c99b518923f42efd9253b65))
* show payment history below the membership and leased bed cards on profile ([33cc5aa](https://github.com/fordprefect480/CommonGround/commit/33cc5aa98196438dbd0b56104d9ac827a5b44e30))

## [0.6.0](https://github.com/fordprefect480/CommonGround/compare/v0.5.0...v0.6.0) (2026-06-22)


### Features

* add leased beds management ([bfb8e00](https://github.com/fordprefect480/CommonGround/commit/bfb8e00d962c31069fc65c99b0e7b82fad0663c2))
* add leased beds management ([9ba1936](https://github.com/fordprefect480/CommonGround/commit/9ba19366dd007cd9cf59dcedc5527d957ceafefd))
* free-form bed labels and soft-deletable beds ([6d6edcd](https://github.com/fordprefect480/CommonGround/commit/6d6edcd2435a74d3bffbc60e543a0ac0fd396812))
* label leased-bed activity log entries ([a03360e](https://github.com/fordprefect480/CommonGround/commit/a03360ef894cd462afe5e794f3b3cf9352415f06))
* manage bed inventory from the Leased beds page ([e0391d2](https://github.com/fordprefect480/CommonGround/commit/e0391d23a4c9973337163f06f969bedab83e89db))
* show the configured leased-bed price on the public site ([e6e6814](https://github.com/fordprefect480/CommonGround/commit/e6e6814eab3d05a93ddd8c585eb24824c15d97fb))


### Bug Fixes

* trim whitespace from Turnstile keys so the captcha widget loads ([b95c6af](https://github.com/fordprefect480/CommonGround/commit/b95c6af2881e7006b4d57cf7ece10372c9e577be))
* trim whitespace from Turnstile keys so the captcha widget loads ([db4936a](https://github.com/fordprefect480/CommonGround/commit/db4936ad1a30901c44c944c44e9a25ddc5244035))

## [0.5.0](https://github.com/fordprefect480/CommonGround/compare/v0.4.0...v0.5.0) (2026-06-19)


### Features

* accept membership signup when payments are unavailable ([83b8e23](https://github.com/fordprefect480/CommonGround/commit/83b8e23f0e1243a52126626f151750b19bfca1da))
* add admin shortcut to site header and rename Events nav to "What's On" ([1bbbcf9](https://github.com/fordprefect480/CommonGround/commit/1bbbcf9d0abb83d435513091be295a35eecc164e))
* add edit action to blog admin table ([6e75069](https://github.com/fordprefect480/CommonGround/commit/6e750690f67f8596d2cd15db753d0a3f80d4dde2))
* drag-to-reorder Instagram tiles ([17dfbd4](https://github.com/fordprefect480/CommonGround/commit/17dfbd4f4e5baf3e6176376e4c42e3612c077154))
* improve members admin table and detail page ([edbc9a3](https://github.com/fordprefect480/CommonGround/commit/edbc9a32772bbc19a2eca36ac3f58ef613a6e6cd))
* label membership-price changes in the activity log ([83ecd77](https://github.com/fordprefect480/CommonGround/commit/83ecd77040646dc7997af6a0f215bee82e9847b5))
* make membership price configurable from admin settings ([96bc385](https://github.com/fordprefect480/CommonGround/commit/96bc385951b7b4bec331a9f05fe83aada36e80fb))
* redesign admin shell with sidebar navigation ([bc52b2e](https://github.com/fordprefect480/CommonGround/commit/bc52b2ef60af52a914bfd8239fc1001629920987))
* restyle admin tables with mobile cards and horizontal scroll ([153127f](https://github.com/fordprefect480/CommonGround/commit/153127fe15469729334dffce7894ba5faa3dfd7e))
* show upcoming Eventbrite events in admin events table ([6711706](https://github.com/fordprefect480/CommonGround/commit/6711706e62c5f721b1925e255042bcfcab66cc37))
* streamline admin membership table ([7b48281](https://github.com/fordprefect480/CommonGround/commit/7b482810390f96fbca920772e9b860537a501065))

## [0.4.0](https://github.com/fordprefect480/CommonGround/compare/v0.3.1...v0.4.0) (2026-06-14)


### Features

* add 'Powered by CommonGround' open-source credit to footer ([be10080](https://github.com/fordprefect480/CommonGround/commit/be100807d70a7dcfdcc74c2655d35b1d815df5bb))
* add membership payment status filters and payment history ([eaffb69](https://github.com/fordprefect480/CommonGround/commit/eaffb69b10afc306ada115fecb436d34817ac24a))
* reflow membership, lease, donate, events and resources pages for mobile ([d3af92e](https://github.com/fordprefect480/CommonGround/commit/d3af92e7a3524151499851b6aab6adeaeecd6e19))
* show deployed version and commit SHA in the site footer ([2c8a644](https://github.com/fordprefect480/CommonGround/commit/2c8a6444295ba5f26f69e9d83e94e0b972becd2e))

## [0.3.1](https://github.com/fordprefect480/CommonGround/compare/v0.3.0...v0.3.1) (2026-06-13)


### Bug Fixes

* restore Azure AD SQL auth provider broken by SqlClient 7 bump ([e548964](https://github.com/fordprefect480/CommonGround/commit/e5489648d385baee1e9b4c143382b1d73a06ecc7))

## [0.3.0](https://github.com/fordprefect480/CommonGround/compare/v0.2.0...v0.3.0) (2026-06-13)


### Features

* add breadcrumbs to admin pages for returning to Dashboard ([b143223](https://github.com/fordprefect480/CommonGround/commit/b1432234916d91881a23003fd6854ef076f4271a))
* add dashboard member stats and attribute activity summaries ([34c430f](https://github.com/fordprefect480/CommonGround/commit/34c430fbcf59f8ba8bd47ea2868dd227860230b4))
* add join mailing list signup modal ([2c0ce29](https://github.com/fordprefect480/CommonGround/commit/2c0ce2965555627f141aadcfcf45075bf33230cf))
* add mobile hamburger menu with slide-in drawer ([5f94e10](https://github.com/fordprefect480/CommonGround/commit/5f94e1041b9f0d7ecb11ff19f2c641c349952217))
* add public membership signup with Stripe payment ([9779cb9](https://github.com/fordprefect480/CommonGround/commit/9779cb9dd63cdbe31adc4b4f32e3cef63be5fb89))
* add useMediaQuery hook and home breakpoints ([0f1cc0b](https://github.com/fordprefect480/CommonGround/commit/0f1cc0bf37b84cb0476d06d39a260b54aaeeb46a))
* forward remaining appsettings through apphost and rename Contact to ContactForm ([9686c54](https://github.com/fordprefect480/CommonGround/commit/9686c545b4af69a10e76c52e4d4413d97a456884))
* full-bleed hero with scrim and light text on mobile ([a6ba4a4](https://github.com/fordprefect480/CommonGround/commit/a6ba4a479b1c26ead6040871969055dce899c5f2))
* make MSSection padding responsive on mobile ([6d74942](https://github.com/fordprefect480/CommonGround/commit/6d74942f19aad9ce286ab4d190f4014b9260af1d))
* reflow about, features and events sections for mobile ([99d47b1](https://github.com/fordprefect480/CommonGround/commit/99d47b19c509a7a33ed46b351f8986972c749b66))
* reflow footer columns for tablet and mobile ([364ac94](https://github.com/fordprefect480/CommonGround/commit/364ac94762f6403a7944001e5577ba889cc0ab9b))
* reflow membership, instagram and contact sections for mobile ([d825fc5](https://github.com/fordprefect480/CommonGround/commit/d825fc532b6abef25a58a35957d5960e3f8c1a67))
* rename admin Tools page to Settings with collapsible Advanced panel ([9b6a7c8](https://github.com/fordprefect480/CommonGround/commit/9b6a7c8600a1883120e76b2ca7fc8c7b6504a82f))
* turn partners strip into a clickable logo carousel ([73e50f4](https://github.com/fordprefect480/CommonGround/commit/73e50f4ada79d8e2ac1e63caab64df5be9d982fc))


### Bug Fixes

* drawer a11y (inert + focus management), partners heading clamp, named hero breakpoint ([2c663f8](https://github.com/fordprefect480/CommonGround/commit/2c663f8c0f5eaebaa15e8cc68eb4efbfbb9a4d93))
* drop unused BP_TABLET import from hero ([ccc976f](https://github.com/fordprefect480/CommonGround/commit/ccc976fecd9692722a6a0f65ab4e740b37828790))
* restore upcoming Eventbrite query so past events stop showing ([1602c07](https://github.com/fordprefect480/CommonGround/commit/1602c0707f4de64896a27f0bb38eea11b187b241))


### Performance Improvements

* code-split routes to shrink initial bundle ([274bc5c](https://github.com/fordprefect480/CommonGround/commit/274bc5c22d0c4036adf2ada2282f1fcfe907b749))

## [0.2.0](https://github.com/fordprefect480/CommonGround/compare/v0.1.0...v0.2.0) (2026-06-13)


### Features

* add donate page ([ed29e62](https://github.com/fordprefect480/CommonGround/commit/ed29e628223e5184d0d660a2a8398745c64993eb))
* add events page with list and detail panel ([9d22f10](https://github.com/fordprefect480/CommonGround/commit/9d22f10376570441b16d07963e81d6626f9c9453))
* add lease-a-plot page ([7aac902](https://github.com/fordprefect480/CommonGround/commit/7aac902f0a67c61956ced4bd9c118b26ed8184d9))
* add membership page ([a314572](https://github.com/fordprefect480/CommonGround/commit/a314572246534183d2f285af945d372844d4c30f))
* add resources page ([c32cf0d](https://github.com/fordprefect480/CommonGround/commit/c32cf0de0ca964f190cbf929f02b6c21704e883e))
* center garden map on our pin and remove the card list ([3de5bad](https://github.com/fordprefect480/CommonGround/commit/3de5badd5d186ac2cf2a51c26df24d9ce18a9836))
* fit garden map bounds to all pins on load ([8a0356e](https://github.com/fordprefect480/CommonGround/commit/8a0356edfe390dd00d34edff877acfd4d65c8f75))
* highlight our garden's map pin and update its address ([79f4e9c](https://github.com/fordprefect480/CommonGround/commit/79f4e9c90bf73eac13e226e688a22d4df03e4717))
* remove partners from navigation, keep homepage strip ([77e000c](https://github.com/fordprefect480/CommonGround/commit/77e000cbc8ada1255cb31507e1b269aaf1014063))
* show nearby community gardens on an interactive map ([5f5535d](https://github.com/fordprefect480/CommonGround/commit/5f5535d499007cc0040af8306ba3ea867a9e7ef1))
* update membership price to $25/year ([1e0e31d](https://github.com/fordprefect480/CommonGround/commit/1e0e31d586765f3853ebaa226cc9fb5ed1c19e0b))

## 0.1.0 (2026-06-11)


### Miscellaneous Chores

* release 0.0.1 ([0c3e85c](https://github.com/fordprefect480/CommonGround/commit/0c3e85c5361518c047149e32d9e06ce3a804b39c))
* release 0.1.0 ([d26ef3e](https://github.com/fordprefect480/CommonGround/commit/d26ef3ef2bf8440c5a418eb965ced286990cfc00))
