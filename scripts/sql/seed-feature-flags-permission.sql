-- ============================================================================
-- Seed: "Admin.FeatureFlags" permission
--
-- TARGET DATABASE: the Auth service's database (../Auth repo), NOT any
-- backoffice-final database. Run this against wherever Auth.Api's
-- ConnectionStrings:Auth points (see Auth.Api/appsettings.json).
--
-- Why this exists: the new "Feature Flags" admin page (Backoffice.Web
-- Pages/Administration/FeatureFlags.razor) manages per-client feature flags
-- in the sibling webprops-api service. It is gated by a brand-new claim,
-- AdminPermission.Admin_FeatureFlags (Backoffice.Shared/Enums/
-- PermissionCategory.cs) -> ClaimType "Admin.FeatureFlags", checked by
-- WebPropsController's [Authorize("Admin.FeatureFlags", ...)] attributes.
--
-- The Auth repo has no migration/seed mechanism for its Permission table
-- (rows are hand-inserted reference data) -- this script follows the same
-- precedent as seed-translations-permission.sql for the same gap.
--
-- SEQUENCING: run this BEFORE deploying the backoffice-final code that adds
-- the [Authorize("Admin.FeatureFlags", ...)] attributes, and BEFORE granting
-- the permission to any usergroup. If the code deploys first, every
-- Admin.FeatureFlags check fails for everyone (including Super Admin) until
-- this row exists.
--
-- CategoryId = 0 (Admin) is already included in the default categories list
-- requested by Backoffice.API/Repositories/AuthenticationRepository.cs
-- GetPermissionList -- unlike Translations (category 4), no change to that
-- categories list is needed for this permission to show up in Manage User
-- Groups.
--
-- NOTE: SortOrder below is a best-effort placement after the other Admin
-- category items visible in Backoffice.Shared/Enums/PermissionCategory.cs
-- (Dashboard, ManageUsers, ManageUserGroups, 4x Settlements, CancelBets,
-- CancelBetsHistory, Maintenance) -- verify/adjust against the actual
-- persisted SortOrder values in dbo.Permission before running, so the new
-- item lands in the expected place in the Manage User Groups permission
-- tree UI.
--
-- Idempotent: guarded by IF NOT EXISTS on ClaimType (no unique DB constraint
-- exists on that column today, so this script is the only protection
-- against accidental duplicate rows on re-run).
--
-- SUPERSEDED (GM-693): the permission was renamed to "Admin.ClientSiteConfig"
-- by rename-feature-flags-to-client-site-config.sql, which also handles fresh
-- environments. Use that script instead. The guard below checks BOTH claim
-- types so re-running this script after the rename cannot re-insert the old
-- row as a duplicate.
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE ClaimType IN (N'Admin.FeatureFlags', N'Admin.ClientSiteConfig'))
BEGIN
    INSERT INTO dbo.Permission
        (PermissionId, CategoryId, CategoryName, SubCategoryName, GroupName, Name, ClaimType, HasReadOnly, SortOrder, IsSubUser, UserGroupTemplateIdRef, Version)
    VALUES
        (NEWID(), 0, N'Admin', N'', N'', N'Feature Flags', N'Admin.FeatureFlags', 1, 10, 0, 0, 0);
END;
GO

-- After running this script:
--   1. In Backoffice.Web: Administration -> Manage User Groups -> (relevant role,
--      e.g. Super Admin) -> Admin -> Feature Flags -> check the box -> Save.
--   2. Log out and back in (JWT claims are fixed at login time; no live refresh
--      exists for an already-active session).
--   3. Only then deploy/rely on the backoffice-final code that checks
--      "Admin.FeatureFlags".
