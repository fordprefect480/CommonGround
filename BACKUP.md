# Backup & restore runbook

This document explains how CommonGround's data is protected in Azure and how to
recover from the scenarios people actually worry about: *"I accidentally deleted
the Container App / the database / a row, how do I get it back?"*

## TL;DR — what to know in 30 seconds

- **The Container App holds no data.** It is stateless: the image contains the
  compiled API plus the bundled React frontend (`wwwroot`), and nothing else is
  written to its filesystem at runtime. If you delete the Container App, you lose
  nothing permanent — `azd up` / `aspire deploy` redeploys it and it reconnects to
  the database.
- **The Azure SQL database is the only durable store.** Members, memberships,
  Stripe payment records, bed leases, blog posts, **blog images** (stored as
  `varbinary` BLOBs, not on disk), community events, sent-email records and the
  audit log all live in SQL. This is the thing to protect.
- **Backups are automatic.** Azure SQL continuously backs the database up, giving a
  **7-day point-in-time restore (PITR)** window — 7 days is the maximum (and the
  default) on the **Basic tier** this database runs on. One thing is configured
  manually, once per environment (see below): a **delete lock** on the SQL server.
  For protection older than 7 days, take periodic **`.bacpac` exports** — native
  long-term retention (LTR) is not configured here; the `.bacpac` exports are the
  long-term story (they are offsite and portable, which LTR snapshots are not).

## What is backed up, and where it's configured

| Layer | Mechanism | Defined in |
|---|---|---|
| Point-in-time restore (PITR), 7-day rolling window | Azure SQL automated backups + short-term retention policy | Nothing to set — 7 days is the Basic-tier default *and* maximum |
| Long-term retention (LTR): weekly/monthly/yearly snapshots | *Not used* — available on Basic tier, but `.bacpac` exports are preferred (offsite, portable, restorable anywhere) | — (use `.bacpac` exports instead) |
| Protection against deleting the SQL server itself | `CanNotDelete` management lock | **Manual** (this runbook) — a lock on the resource group would block `azd` deploys, so it is applied operationally, not in IaC |
| Long-term / offsite copy (`.bacpac`) | `az sql db export` to Blob Storage | **Manual / scheduled** (this runbook) |

> PITR retention is **not** codified in the AppHost. `Azure.Provisioning.Sql` 1.1.0
> generates invalid bicep for the retention-policy resources (it omits the required
> `name`, failing `bicep build` with BCP035). On the Basic tier this doesn't matter:
> 7 days is both the default and the maximum, so there is nothing to set — just
> verify it (below). A longer window (up to 35 days) requires upgrading the database
> to Standard tier or above.
>
> **Native LTR is not configured.** It became technically possible when the database
> moved from serverless (where auto-pause blocked it) to Basic tier, but the
> `.bacpac` exports below remain the long-term story: they live in your own storage
> account (surviving even server or subscription loss) and restore anywhere, which
> LTR snapshots do not.

## One-time setup: lock the SQL server against deletion

This is the single most important manual step. PITR backups are tied to the logical
SQL **server** — if the *server* resource is deleted, its PITR backups go with it
(a deleted *database* can be restored for a while, but a deleted *server* is the real
danger, and `.bacpac` exports are your only off-server copy). A `CanNotDelete` lock
prevents that while still allowing normal deploys/modifications.

```bash
# Fill these in for your environment.
RG=<resource-group>
SQL_SERVER=<logical-sql-server-name>   # e.g. sql-xxxxxxxx (the Microsoft.Sql/servers resource)

az lock create \
  --name cg-no-delete-sql \
  --lock-type CanNotDelete \
  --resource-group "$RG" \
  --resource-name "$SQL_SERVER" \
  --resource-type Microsoft.Sql/servers \
  --namespace Microsoft.Sql
```

Verify:

```bash
az lock list --resource-group "$RG" -o table
```

To find the server/database names if you don't know them:

```bash
az sql server list -g "$RG" -o table
az sql db list -g "$RG" -s "$SQL_SERVER" -o table   # the app DB is "commongroundDb"
```

## One-time check: verify PITR retention

Nothing to configure on the Basic tier — 7 days is the default and the maximum.
Just confirm it once per environment:

```bash
# Expect retentionDays: 7 (the Basic-tier maximum)
az sql db str-policy show -g "$RG" -s "$SQL_SERVER" -n commongroundDb -o jsonc
```

> `az sql db str-policy set` with anything above 7 days fails on Basic. If a longer
> window is ever needed, upgrade the database to Standard tier or above first.
> Long-term coverage comes from the `.bacpac` export below instead.

## Restore procedures

### Scenario A — "I deleted the Container App" (no data loss)

Nothing to restore. Redeploy from the repo root:

```bash
azd up        # or: aspire deploy
```

The new Container App reads the same `ConnectionStrings:commongroundDb` and picks up
exactly where it left off. (Existing login sessions stay valid because the ASP.NET
Data Protection keys live in the `DataProtectionKeys` table in SQL, not in the
container.)

### Scenario B — "I corrupted/deleted data and need to roll back" (point-in-time restore)

PITR restores into a **new** database (it never overwrites the live one), so you can
inspect before swapping. Within the 7-day window:

```bash
# Restore to a point in time, into a new DB name.
az sql db restore \
  -g "$RG" -s "$SQL_SERVER" \
  -n commongroundDb \
  --dest-name commongroundDb-restore \
  --time "2026-06-23T09:00:00Z"      # UTC, the moment before the bad change
```

Then either:
- point the app at `commongroundDb-restore` temporarily to verify, or
- rename to promote the restore (do this during a short maintenance window):

```bash
# Verify commongroundDb-restore looks right first!
az sql db rename -g "$RG" -s "$SQL_SERVER" -n commongroundDb         --new-name commongroundDb-old
az sql db rename -g "$RG" -s "$SQL_SERVER" -n commongroundDb-restore --new-name commongroundDb
# Once confident, delete commongroundDb-old.
```

### Scenario C — "The database was deleted" (restore a dropped database)

A deleted database can be restored as long as the **server** still exists (this is
exactly why the lock in the one-time setup matters):

```bash
# Find the deletion timestamp.
az sql db list-deleted -g "$RG" -s "$SQL_SERVER" -o table

az sql db restore \
  -g "$RG" -s "$SQL_SERVER" \
  -n commongroundDb \
  --dest-name commongroundDb \
  --deleted-time "2026-06-23T09:00:00Z"
```

### Scenario D — "Recovery older than the 7-day PITR window"

There are **no native LTR snapshots** for this database (LTR is not configured — see
the note near the top). Recovery beyond 7 days relies on the `.bacpac` exports —
restore one as in **Scenario E** below.

### Scenario E — "Total loss / restore outside Azure SQL" (from a .bacpac)

If you keep offsite `.bacpac` exports (see below), you can import one into any SQL
Server — a fresh Azure SQL server, a local SQL Server, anywhere:

```bash
az sql db import \
  -g "$RG" -s "$SQL_SERVER" -n commongroundDb-imported \
  --admin-user "$SQL_ADMIN" --admin-password "$SQL_PASSWORD" \
  --storage-key-type StorageAccessKey --storage-key "$STORAGE_KEY" \
  --storage-uri "https://<account>.blob.core.windows.net/backups/<file>.bacpac"
```

## Scheduled offsite .bacpac export (the long-term backup)

A periodic `.bacpac` in your own storage account **is** the long-term retention story
(plus the "what if the whole subscription is compromised" tail risk, and a portable
artifact you can restore anywhere). With PITR now covering only 7 days on the Basic
tier, this matters more than it used to — schedule it to recover anything older.

```bash
az sql db export \
  -g "$RG" -s "$SQL_SERVER" -n commongroundDb \
  --admin-user "$SQL_ADMIN" --admin-password "$SQL_PASSWORD" \
  --storage-key-type StorageAccessKey --storage-key "$STORAGE_KEY" \
  --storage-uri "https://<account>.blob.core.windows.net/backups/commongroundDb-$(date +%F).bacpac"
```

Run it on a schedule via an Azure Automation runbook, a scheduled GitHub Action, or a
cron job on any trusted machine with the Azure CLI. Apply a lifecycle policy on the
blob container to age old exports out.

## Recommended posture (the short list)

1. **Apply the `CanNotDelete` lock on the SQL server** — one-time, above. This is the
   real guard against "I blew the whole thing away."
2. **Verify the PITR retention** occasionally with the `str-policy show` command —
   nothing to set on the Basic tier (7 days is the default and the maximum), this is
   just a sanity check.
3. **Schedule a weekly `.bacpac` export** — this is your only coverage beyond the
   7-day PITR window (native LTR is not configured; `.bacpac` exports are preferred).
4. **Test a restore once** (Scenario B into a throwaway DB name) so the procedure is
   familiar before you ever need it under pressure.
