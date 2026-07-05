-- ============================================================================
-- Seed: dedicated "Translations" permission category
--
-- TARGET DATABASE: the Auth service's database (../Auth repo), NOT any
-- backoffice-final database. Run this against wherever Auth.Api's
-- ConnectionStrings:Auth points (see Auth.Api/appsettings.json).
--
-- Why this exists: PermissionCategory.Translations (value 4, see
-- Backoffice.Shared/Enums/PermissionCategory.cs) has been a reserved,
-- unwired top-level permission category since before this change. Static
-- and Dynamic Translation admin screens were temporarily gated behind the
-- unrelated SBC.OE.Config (OddsEngine Config Templates) permission as a
-- stopgap; this script adds the two dedicated Permission rows
-- (Translations.Static / Translations.Dynamic) needed once the
-- corresponding [Authorize] attributes in ConfigController.cs are deployed.
--
-- The Auth repo has no migration/seed mechanism for its Permission table
-- (rows are hand-inserted reference data, confirmed: no .sql/Migrations/
-- Seed files exist there) -- this script fills that gap for this one
-- addition, following the idempotent IF NOT EXISTS style already used by
-- bet-genius's own hand-written scripts against a different shared DB.
--
-- SEQUENCING: run this BEFORE deploying the backoffice-final code change
-- that switches the [Authorize] attributes to these new claim types, and
-- BEFORE granting the permission to any role. If the code deploys first,
-- every Translations.Static/Dynamic check fails for everyone (including
-- Super Admin) until this row exists, since a claim that doesn't exist as
-- a Permission definition can't be granted to any role either.
--
-- Idempotent: guarded by IF NOT EXISTS on ClaimType (no unique DB
-- constraint exists on that column today, so this script is the only
-- protection against accidental duplicate rows on re-run).
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE ClaimType = N'Translations.Static')
BEGIN
    INSERT INTO dbo.Permission
        (PermissionId, CategoryId, CategoryName, SubCategoryName, GroupName, Name, ClaimType, HasReadOnly, SortOrder, IsSubUser, UserGroupTemplateIdRef, Version)
    VALUES
        (NEWID(), 4, N'Translations', N'Translation Management', N'', N'Static Translation', N'Translations.Static', 1, 1, 0, 0, 0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE ClaimType = N'Translations.Dynamic')
BEGIN
    INSERT INTO dbo.Permission
        (PermissionId, CategoryId, CategoryName, SubCategoryName, GroupName, Name, ClaimType, HasReadOnly, SortOrder, IsSubUser, UserGroupTemplateIdRef, Version)
    VALUES
        (NEWID(), 4, N'Translations', N'Translation Management', N'', N'Dynamic Translation', N'Translations.Dynamic', 1, 2, 0, 0, 0);
END;
GO

-- After running this script:
--   1. In Backoffice.Web: Administration -> Manage User Groups -> (relevant role,
--      e.g. Super Admin) -> Translations -> Translation Management -> check
--      "Static Translation" / "Dynamic Translation" -> Save.
--      NOTE: the Translations section only appears in that modal once the
--      backoffice-final code that requests categories=0,1,2,4 from the Auth
--      permission/list endpoint is deployed (Backoffice.API
--      AuthenticationRepository.GetPermissionList) -- Auth's endpoint defaults
--      to categories 0,1,2 and silently omits category 4 otherwise.
--   2. Log out and back in (JWT claims are fixed at login time; no live refresh
--      exists for an already-active session).
--   3. Only then deploy/rely on the backoffice-final code that checks these
--      claim types.
