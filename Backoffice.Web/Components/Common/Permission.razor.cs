using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Security.Claims;
using static Backoffice.Shared.Models.PermissionTreeModel;

namespace Backoffice.Web.Components.Common
{
    public partial class Permission : IDisposable
    {
        #region Properties
        [Parameter] public List<PermissionModel> Permissions { get; set; }
        [Parameter] public ClientModel ClientModel { get; set; }
        [Parameter] public bool IsReadOnly { get; set; }
        public PermissionTreeModel PermissionTree { get; set; }
        public List<string> SelectedClients { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            try
            {
                SelectedClients = new List<string>();
                PermissionTree = await GetPermissionTree();

                var clientAccess = Permissions.FirstOrDefault(p => p.ClaimType == SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."));
                if (clientAccess != null && !string.IsNullOrEmpty(clientAccess.ClaimValue))
                    SelectedClients = clientAccess.ClaimValue.Split(",").ToList();
            }
            catch (Exception)
            {

            }
        }

        public async Task OnClientsClicked(ChangeEventArgs e, string clientCode, bool isLabel = false)
        {
            if ((isLabel && !SelectedClients.Contains(clientCode)) || (e != null && Convert.ToBoolean(e.Value) == true))
            {
                if (!SelectedClients.Any(s => s == clientCode))
                    SelectedClients.Add(clientCode);
            }
            else
            {
                SelectedClients.Remove(clientCode);
            }

            var permission = Permissions.FirstOrDefault(permission => permission.ClaimType == SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."));
            if (permission != null)
            {
                permission.ClaimValue = SelectedClients.Any() ? string.Join(",", SelectedClients) : string.Empty;
            }
        }

        public void Dispose()
        {
        }
        #endregion

        #region Utilities
        private async Task<PermissionTreeModel> GetPermissionTree()
        {
            var permissionTree = new PermissionTreeModel();

            if (Permissions != null && Permissions.Any())
            {
                var permissionCategories = Permissions.GroupBy(p => p.CategoryId).Select(s => new PermissionTreeModel.PermissionCategory
                {
                    CategoryId = s.Key,
                    CategoryName = s.FirstOrDefault().CategoryName,
                }).Distinct().ToList();

                foreach (var permission in permissionCategories)
                {
                    var subcategories = Permissions.Where(p => p.CategoryId == permission.CategoryId)
                        .GroupBy(p => p.SubCategoryName).Select(s => new PermissionTreeModel.SubCategory
                    {
                        SubCategoryName = s.Key,
                    }).Distinct().ToList();

                    if (subcategories.Any())
                    {
                        foreach (var subCategory in subcategories)
                        {
                            var claims = Permissions.Where(p => p.CategoryId == permission.CategoryId && p.SubCategoryName == subCategory.SubCategoryName 
                                && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_",".") && string.IsNullOrEmpty(p.GroupName))
                                .GroupBy(p => p.ClaimType).Select(s => new PermissionTreeModel.PermissionClaim
                            {
                                Name = s.FirstOrDefault().Name,
                                ClaimType = s.Key,
                                IsSubUser = s.FirstOrDefault().IsSubUser,
                                HasReadOnly = s.FirstOrDefault().HasReadOnly,
                                SortOrder = s.FirstOrDefault().SortOrder,
                                ClaimValue = s.FirstOrDefault().ClaimValue
                            }).Distinct().ToList();

                            var groups = Permissions.Where(p => p.CategoryId == permission.CategoryId && p.SubCategoryName == subCategory.SubCategoryName && !string.IsNullOrEmpty(p.GroupName)).Select(s => s.GroupName).Distinct().ToList();
                            var groupList = new List<PermissionTreeModel.Group>();

                            foreach (var group in groups)
                            {
                                var groupClaims = Permissions.Where(p => p.CategoryId == permission.CategoryId && p.SubCategoryName == subCategory.SubCategoryName
                                    && p.GroupName == group && !string.IsNullOrEmpty(p.GroupName)).Select(s => new PermissionTreeModel.PermissionClaim
                                    {
                                        Name = s.Name,
                                        ClaimType = s.ClaimType,
                                        IsSubUser = s.IsSubUser,
                                        HasReadOnly = s.HasReadOnly,
                                        SortOrder = s.SortOrder,
                                        ClaimValue = s.ClaimValue
                                    }).Distinct().ToList();

                                var groupModel = new PermissionTreeModel.Group
                                {
                                    GroupName = group,
                                    Claims = groupClaims
                                };
                                groupList.Add(groupModel);
                            }

                            permission.SubCategories.Add(new PermissionTreeModel.SubCategory
                            {
                                SubCategoryName = subCategory.SubCategoryName,
                                Claims = claims,
                                Groups = groupList
                            });
                        }
                    }

                    permissionTree.Permissions.Add(permission);

                }
            }
            return permissionTree;
        }
        #endregion
    }
}
