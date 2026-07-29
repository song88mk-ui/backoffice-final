-- BO-87 (fallout): Widen Auth..ActivityLog.Token from nvarchar(3000) to nvarchar(max).
--
-- Why: Auth.Api SaveToken() inserts the full JWT into ActivityLog.Token on every login
-- (Auth.Api/Repositories/AuthRepository.cs). The superadmin token embeds one claim per
-- row of the Permission table, so each new permission grows the JWT. Adding the BO-87
-- permission (SBC.OE.MarketSetting.SelectorTemplates) pushed the superadmin token from
-- 2,948 to ~3,010 chars, exceeding the old nvarchar(3000) limit. The insert then failed
-- with a truncation error inside a DbUpdateException and login was rejected with
-- "An error occurred while saving the entity changes. See the inner exception for details."
--
-- Idempotent: re-running is a no-op once the column is already nvarchar(max)
-- (CHARACTER_MAXIMUM_LENGTH = -1). Applied to Staging on 2026-07-23.
--
-- Run against the Auth database of the target environment.
-- Production execution requires explicit approval per the repository safety rules,
-- and MUST be run before (or together with) BO-87_Add_SelectorTemplates_Permission.sql,
-- otherwise superadmin login breaks in Production the same way it broke in Staging.

IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
      AND TABLE_NAME = 'ActivityLog'
      AND COLUMN_NAME = 'Token'
      AND CHARACTER_MAXIMUM_LENGTH <> -1
)
BEGIN
    ALTER TABLE dbo.ActivityLog ALTER COLUMN Token nvarchar(max) NULL;
    PRINT 'ActivityLog.Token widened to nvarchar(max).';
END
ELSE
BEGIN
    PRINT 'ActivityLog.Token is already nvarchar(max). No change made.';
END
