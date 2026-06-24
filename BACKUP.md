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
- **Backups are automatic.** Azure SQL continuously backs the database up. Two
  things are configured manually, once per environment (see below): the **retention
  windows** (35-day PITR + long-term snapshots) and a **delete lock** on the SQL
  server.

## What is backed up, and where it's configured

| Layer | Mechanism | Defined in |
|---|---|---|
| Point-in-time restore (PITR), 35-day rolling window | Azure SQL automated backups + short-term retention policy | **Manual** (this runbook) — see below |
| Long-term retention (LTR): weekly 4 weeks, monthly 6 months, yearly 1 year | Azure SQL long-term retention policy | **Manual** (this runbook) — see below |
| Protection against deleting the SQL server itself | `CanNotDelete` management lock | **Manual** (this runbook) — a lock on the resource group would block `azd` deploys, so it is applied operationally, not in IaC |
| Optional offsite copy (`.bacpac`) | `az sql db export` to Blob Storage | **Manual / scheduled** (this runbook) |

> Retention is **not** codified in the AppHost. `Azure.Provisioning.Sql` 1.1.0
> generates invalid bicep for the retention-policy resources (it omits the required
> `name`, failing `bicep build` with BCP035), so the policies are set by hand with
> the `az sql db str-policy set` / `ltr-policy set` commands below. They are sticky —
> set them once per environment and they survive redeploys.

## One-time setup: lock the SQL server against deletion

This is the single most important manual step. PITR backups are tied to the logical
SQL **server** — if the *server* resource is deleted, its PITR backups go with it
(LTR backups survive, and a deleted *database* can be restored for a while, but a
deleted *server* is the real danger). A `CanNotDelete` lock prevents that while
still allowing normal deploys/modifications.

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

## One-time setup: configure backup retention

Run these once per environment. They are not in the AppHost (see the note above), so
they must be applied by hand; once set they persist across redeploys.

```bash
# Short-term (PITR): 35-day rolling window.
az sql db str-policy set -g "$RG" -s "$SQL_SERVER" -n commongroundDb --retention-days 35

# Long-term (LTR): weekly 4 weeks, monthly 6 months, yearly 1 year (first week).
az sql db ltr-policy set -g "$RG" -s "$SQL_SERVER" -n commongroundDb \
  --weekly-retention P4W --monthly-retention P6M \
  --yearly-retention P1Y --week-of-year 1
```

Verify:

```bash
# Short-term (PITR) — expect retentionDays: 35
az sql db str-policy show -g "$RG" -s "$SQL_SERVER" -n commongroundDb -o jsonc

# Long-term — expect P4W / P6M / P1Y
az sql db ltr-policy show -g "$RG" -s "$SQL_SERVER" -n commongroundDb -o jsonc
```

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
inspect before swapping. Within the 35-day window:

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

### Scenario D — "Restore from a long-term (LTR) snapshot"

For recovery older than the 35-day PITR window (up to 1 year here):

```bash
# List available LTR backups and copy the one you want (its resource ID).
az sql db ltr-backup list \
  -l <region> -g "$RG" -s "$SQL_SERVER" -d commongroundDb -o table

az sql db ltr-backup restore \
  --backup-id "<ltr-backup-resource-id>" \
  --dest-database commongroundDb-ltr-restore \
  --dest-server "$SQL_SERVER" \
  --dest-resource-group "$RG"
```

Then verify and promote as in Scenario B.

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

## Optional: scheduled offsite .bacpac export

Azure SQL's own backups are robust, but a periodic `.bacpac` in your own storage
account gives an independent copy (useful for the "what if the whole subscription is
compromised" tail risk) and a portable artifact you can restore anywhere.

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
2. **Set the retention policies by hand** (35-day PITR + 12-month LTR) — one-time,
   above — and confirm occasionally with the `str-policy show` / `ltr-policy show`
   commands. They survive redeploys, so this is a per-environment setup step.
3. **Optionally schedule a weekly `.bacpac` export** for an independent offsite copy.
4. **Test a restore once** (Scenario B into a throwaway DB name) so the procedure is
   familiar before you ever need it under pressure.
