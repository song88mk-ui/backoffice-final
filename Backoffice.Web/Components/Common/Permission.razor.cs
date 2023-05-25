using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.EntityFrameworkCore.Internal;
using System.Collections.Generic;
using System.Security.Claims;
using static Backoffice.Shared.Models.PermissionTreeModel;

namespace Backoffice.Web.Components.Common
{
    public partial class Permission : IDisposable
    {
        #region Properties
        [Inject] public IAuthenticationService AuthenticationService { get; set; }

        [Parameter] public List<PermissionModel> Permissions { get; set; }
        [Parameter] public ClientModel ClientModel { get; set; }
        [Parameter] public bool IsReadOnly { get; set; }
        [Parameter] public bool IsSubUser { get; set; }
        [Parameter] public EventCallback<bool> IsClientEmptyCallback { get; set; }

        public PermissionTreeModel PermissionTree { get; set; }
        public List<string> SelectedClients { get; set; }
        public int SelectedCategoryId { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> SelectItems { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            try
            {
                Init();
            }
            catch (Exception)
            {

            }
        }
        public async Task OnDropCategoryClickEvent(PermissionTreeModel.PermissionCategoryModel category)
        {
            SelectedCategoryId = category.CategoryId;
        }

        public async Task OnEnableCategoryClickEvent(PermissionTreeModel.PermissionCategoryModel category)
        {
            category.IsEnabled = !category.IsEnabled;

            if (SelectItems.Where(x => !x.IsChecked).Count() == SelectItems.Count())
            {
                await IsClientEmptyCallback.InvokeAsync(true);
            }
            else
            {
                await IsClientEmptyCallback.InvokeAsync(false);
            }

            if (category.IsEnabled == false)
            {
                await OnDisableClickEvent(category);
            }
        }

        public async Task OnDisableClickEvent(PermissionTreeModel.PermissionCategoryModel category)
        {
            if (category.SubCategories.Any())
            {
                foreach (var subCategory in category.SubCategories)
                {
                    foreach (var claim in subCategory.Claims)
                    {
                        if (claim.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."))
                            claim.ClaimValue = "0";
                    }
                }
            }

            var permissions = Permissions.Where(p => p.CategoryId == category.CategoryId && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."));
            foreach (var permission in permissions)
            {
                permission.ClaimValue = "0";
            }

        }

        public async Task OnEnableClickEvent(PermissionTreeModel.PermissionCategoryModel category)
        {
            if (category.SubCategories.Any())
            {
                foreach (var subCategory in category.SubCategories)
                {
                    foreach (var claim in subCategory.Claims)
                    {
                        if ((IsSubUser && !claim.IsEnabled) == false && claim.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."))
                            claim.ClaimValue = "1";
                    }

                    if (subCategory.Groups != null && subCategory.Groups.Any())
                    {
                        foreach (var group in subCategory.Groups)
                        {
                            if (group.Claims != null && group.Claims.Any())
                            {
                                foreach (var claim in group.Claims)
                                {
                                    if ((IsSubUser && !claim.IsEnabled) == false && claim.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."))
                                        claim.ClaimValue = "1";
                                }
                            }
                        }
                    }
                }
            }

            var permissions = Permissions.Where(p => p.CategoryId == category.CategoryId && (IsSubUser && !p.IsEnabled) == false
                && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."));
            foreach (var permission in permissions)
            {
                permission.ClaimValue = "1";
            }

        }

        public async Task OnGroupSelectChangedEvent()
        {
            Init();
        }

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedClients.Clear();
            SelectedClients = items.Any() ? items.Select(s => s.Key).ToList() : new List<string>();

            var permission = Permissions.FirstOrDefault(permission => permission.ClaimType == SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."));
            if (permission != null)
            {
                permission.ClaimValue = SelectedClients.Any() ? string.Join(",", SelectedClients) : string.Empty;

                if (string.IsNullOrEmpty(permission.ClaimValue))
                {
                    await IsClientEmptyCallback.InvokeAsync(true);
                }
                else
                {
                    await IsClientEmptyCallback.InvokeAsync(false);
                }
            }
        }

        public void Dispose()
        {
        }
        #endregion

        #region Utilities
        private async void Init()
        {
            SelectedClients = new List<string>();
            PermissionTree = await GetPermissionTree();

            var clientAccess = Permissions.FirstOrDefault(p => p.ClaimType == SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."));
            if (clientAccess != null && !string.IsNullOrEmpty(clientAccess.ClaimValue))
                SelectedClients = clientAccess.ClaimValue.Split(",").ToList();

            SelectedCategoryId = PermissionTree.Permissions.FirstOrDefault().CategoryId;

            SelectItems = new List<SelectListItemWithCheckboxModel<string>>();

            SelectItems = ClientModel.Clients.Select( x => new SelectListItemWithCheckboxModel<string>()
                                                        {
                                                            Name = x.ClientName,
                                                            Value = x.ClientCode,
                                                            IsChecked = SelectedClients.Contains(x.ClientCode)
                                                        }).ToList();

            SelectItems.Insert(0, new SelectListItemWithCheckboxModel<string>()
            {
                Name = "Select All Clients",
                Value = "all",
                IsChecked = ClientModel.Clients.Count == SelectedClients.Count ? true : false
            });

            foreach (var category in PermissionTree.Permissions)
            {
                if (category.SubCategories.Any(s => s.Claims.Any(c => c.ClaimValue != "0")))
                    category.IsEnabled = true;
            }

            var permission = Permissions.FirstOrDefault(permission => permission.ClaimType == SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", "."));
            if (permission != null)
            {
                if (SelectItems.Where(x => !x.IsChecked).Count() == SelectItems.Count())
                {
                    await IsClientEmptyCallback.InvokeAsync(true);
                }
                else
                {
                    await IsClientEmptyCallback.InvokeAsync(false);
                }
            }
        }
        private async Task<PermissionTreeModel> GetPermissionTree()
        {
            var permissionTree = new PermissionTreeModel();

            if (Permissions != null && Permissions.Any())
            {
                var permissionCategories = Permissions.GroupBy(p => p.CategoryId).Select(s => new PermissionTreeModel.PermissionCategoryModel
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
                                && p.ClaimType != SportsbookClientPermission.SBC_Clients_Access.ToString().Replace("_", ".") && string.IsNullOrEmpty(p.GroupName)).OrderBy(o => o.SortOrder)
                                .GroupBy(p => p.ClaimType).Select(s => new PermissionTreeModel.PermissionClaim
                                {
                                    Name = s.FirstOrDefault().Name,
                                    ClaimType = s.Key,
                                    IsSubUser = s.FirstOrDefault().IsSubUser,
                                    HasReadOnly = s.FirstOrDefault().HasReadOnly,
                                    SortOrder = s.FirstOrDefault().SortOrder,
                                    ClaimValue = s.FirstOrDefault().ClaimValue,
                                    IsEnabled = s.FirstOrDefault().IsEnabled
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
                                        ClaimValue = s.ClaimValue,
                                        IsEnabled = s.IsEnabled
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
                                Groups = groupList,
                                ColPosition = permission.CategoryName != "SportsbookClient" ? string.Empty : subCategory.SubCategoryName == "Odds Engine" ? "RIGHT" : "LEFT"
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
