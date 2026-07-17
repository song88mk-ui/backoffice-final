-- BO-87: Register the new "Selector Market Templates" permission in the Auth database
-- so it appears in Manage User Groups and can be granted per user group.
--
-- The row is cloned from the existing "SBC.OE.MarketSetting.SortOrder" permission so
-- CategoryId/CategoryName/SubCategoryName/GroupName/HasReadOnly/IsSubUser/Version match
-- the Market Settings group it sits under. Idempotent: re-running is a no-op.
--
-- Run against the Auth database of the target environment (Staging first).
-- Production execution requires explicit approval per the repository safety rules.

INSERT INTO dbo.Permission
    (PermissionId, CategoryId, CategoryName, SubCategoryName, GroupName, Name, ClaimType,
     HasReadOnly, SortOrder, IsSubUser, UserGroupTemplateIdRef, Version)
SELECT
    NEWID(),
    p.CategoryId,
    p.CategoryName,
    p.SubCategoryName,
    p.GroupName,
    'Selector Market Templates',
    'SBC.OE.MarketSetting.SelectorTemplates',
    p.HasReadOnly,
    p.SortOrder + 1,
    p.IsSubUser,
    p.UserGroupTemplateIdRef,
    p.Version
FROM dbo.Permission p
WHERE p.ClaimType = 'SBC.OE.MarketSetting.SortOrder'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Permission x
      WHERE x.ClaimType = 'SBC.OE.MarketSetting.SelectorTemplates'
  );
