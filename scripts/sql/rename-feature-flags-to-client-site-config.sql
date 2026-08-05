-- ============================================================================
-- Rename: "Admin.FeatureFlags" permission -> "Admin.ClientSiteConfig"
--
-- TARGET DATABASE: the Auth service's database (../Auth repo), NOT any
-- backoffice-final database. Run this against wherever Auth.Api's
-- ConnectionStrings:Auth points (see Auth.Api/appsettings.json).
--
-- Why this exists: the "Feature Flags" admin page (GM-693) was renamed to
-- "Client Site Config" (Backoffice.Web Pages/Administration/
-- ClientSiteConfig.razor) so it can grow to manage the client's full
-- platform-web site config (feature flags now; themes etc. later). The
-- gating claim was renamed with it: AdminPermission.Admin_ClientSiteConfig
-- (Backoffice.Shared/Enums/PermissionCategory.cs) -> ClaimType
-- "Admin.ClientSiteConfig", checked by WebPropsController's
-- [Authorize("Admin.ClientSiteConfig", ...)] attributes.
--
-- Grants survive the rename WITHOUT migration: AspNetRoleClaims/
-- AspNetUserClaims store the Permission row's PermissionId GUID, not the
-- ClaimType string (Auth.Api/Repositories/AuthRepository.cs joins
-- roleClaims.ClaimType to permissions.PermissionId). The JWT claim string is
-- looked up from this row at login, so updating the row in place carries
-- every existing user-group assignment along.
--
-- SEQUENCING: run this together with the backoffice-final deploy that renames
-- the claim in code. Users must log out and back in afterwards (JWT claims
-- are fixed at login); until they do, the Client Site Config page (only)
-- rejects them because their token still carries the old claim string.
--
-- Idempotent:
--   - old row exists           -> renamed (UPDATE)
--   - new row already exists   -> no-op
--   - neither exists           -> new row inserted (fresh environments)
-- Re-running always converges to exactly one 'Admin.ClientSiteConfig' row.
-- ============================================================================

IF EXISTS (SELECT 1 FROM dbo.Permission WHERE ClaimType = N'Admin.FeatureFlags')
BEGIN
    UPDATE dbo.Permission
    SET ClaimType = N'Admin.ClientSiteConfig',
        Name = N'Client Site Config'
    WHERE ClaimType = N'Admin.FeatureFlags';

    PRINT 'Renamed Admin.FeatureFlags -> Admin.ClientSiteConfig.';
END
ELSE IF EXISTS (SELECT 1 FROM dbo.Permission WHERE ClaimType = N'Admin.ClientSiteConfig')
BEGIN
    PRINT 'Admin.ClientSiteConfig already present - nothing to do.';
END
ELSE
BEGIN
    -- Fresh environment: neither row exists yet. Same row shape as
    -- seed-feature-flags-permission.sql (which this script supersedes).
    INSERT INTO dbo.Permission
        (PermissionId, CategoryId, CategoryName, SubCategoryName, GroupName, Name, ClaimType, HasReadOnly, SortOrder, IsSubUser, UserGroupTemplateIdRef, Version)
    VALUES
        (NEWID(), 0, N'Admin', N'', N'', N'Client Site Config', N'Admin.ClientSiteConfig', 1, 10, 0, 0, 0);

    PRINT 'Inserted Admin.ClientSiteConfig (fresh environment).';
END;
GO

-- After running this script:
--   1. Deploy the backoffice-final release that checks "Admin.ClientSiteConfig".
--   2. Users log out and back in (JWT claims are fixed at login time; no live
--      refresh exists for an already-active session).
--   3. Verify: menu shows "Client Site Config", Manage User Groups shows the
--      renamed row with the existing checkbox state preserved.
