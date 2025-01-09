using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace Backoffice.Web.Components.Common
{
    public partial class ChangePasswordModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IAuthenticationService AuthenticationService { get; set; }

        public List<AlertModel> Alerts { get; set; }

        public string NewPassword = string.Empty;
        public string ConfirmNewPassword = string.Empty;
        public Guid AlertGuid = Guid.Empty;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Alerts = new List<AlertModel>();
        }

        public async Task OnCancelModalClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnChangePasswordClickEvent()
        {
            bool isValidInput = await ValidateInput();

            if (isValidInput)
            {
                var response = await AuthenticationService.UpdateUserPassword(_userState.Id, new Backoffice.Shared.Models.Requests.UserRegisterRequest() 
                {
                    Password = NewPassword
                });

                if (response.StatusCode == StatusCode.Success)
                {
                    Alerts.Clear();

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Success,
                        Message = "Password successfully updated",
                        Opacity = 1.0m
                    });

                    NewPassword = string.Empty;
                    ConfirmNewPassword = string.Empty;
                }
                else
                {
                    Alerts.Clear();

                    Alerts.Add(new AlertModel() 
                    {
                        Status = AlertStatus.Error,
                        Message = $"An error has occured: {response.Message}",
                        Opacity = 1.0m
                    });
                }
            }
        }

        public async Task OnCancelClickEvent()
        {
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
            Alerts.Clear();
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private async Task<bool> ValidateInput()
        {
            var passwordErrorCount = 0;
            bool IsValidPassword = true;

            if (!string.IsNullOrWhiteSpace(NewPassword) || !string.IsNullOrWhiteSpace(ConfirmNewPassword))
            {
                if (NewPassword != ConfirmNewPassword)
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password must match",
                        Opacity = 1.0m
                    });
                }

                if (!(NewPassword.Count() >= 8 && NewPassword.Count() <= 15))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password must be between 8 to 15 character long.",
                        Opacity = 1.0m
                    });
                }

                if (!NewPassword.Any(char.IsLower))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password should contain lowercase characters.",
                        Opacity = 1.0m
                    });
                }
                if (!NewPassword.Any(char.IsUpper))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password should contain uppercase characters.",
                        Opacity = 1.0m
                    });
                }
                if (!NewPassword.Any(char.IsDigit))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password should contain any digit from 0-9.",
                        Opacity = 1.0m
                    });
                }

                var regex = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");
                if (!regex.IsMatch(NewPassword))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password must include at least one special character.",
                        Opacity = 1.0m
                    });
                }

                if (NewPassword.Any(char.IsWhiteSpace))
                {
                    IsValidPassword = false;
                    passwordErrorCount++;

                    Alerts.Add(new AlertModel
                    {
                        Status = AlertStatus.Error,
                        Message = "Password should not contain spaces.",
                        Opacity = 1.0m
                    });
                }
            }
            else
            {
                IsValidPassword = false;
                passwordErrorCount++;

                Alerts.Add(new AlertModel
                {
                    Status = AlertStatus.Error,
                    Message = "Password must not be empty.",
                    Opacity = 1.0m
                });
            }

            if (passwordErrorCount > 1)
            {
                Alerts.RemoveAll(x => x.Message.Contains("Password"));

                AlertGuid = Guid.NewGuid();

                Alerts.Add(new AlertModel
                {
                    Status = AlertStatus.Error,
                    //Message = "Password must be 8-15 characters long, which should contain uppercase and lowercase characters, with any digit from 0-9, and must include at least one special character.",
                    Message = "password_criteria",
                    Opacity = 1.0m
                });
            }

            if (IsValidPassword)
            {
                return true;
            }

            return false;
        }
        #endregion
    }
}
