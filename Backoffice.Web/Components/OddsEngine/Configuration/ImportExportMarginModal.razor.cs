using Backoffice.Shared.Enums;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.IO.Compression;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ImportExportMarginModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private IExcelService ExcelService { get; set; }
        [Inject] private IMessageBroker MessageBroker { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public static HashSet<string> SelectedClientsSet { get; set; }
        public static HashSet<string> SelectedSportsSet { get; set; }
        public bool IsImportTab { get; set; }
        public bool IsUploading { get; set; }        

        public string SelectedClient { get; set; }
        public string ToastTitle { get; set; }
        public string ToastMessage { get; set; }

        public const string ImportSelect = "import-select";
        public const string ExportSelect = "export-select";
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsImportTab = true;
            IsUploading = false;
            Clients = new List<SelectListItemModel<string>>();
            Sports = new List<SelectListItemModel<int>>();

            SelectedClientsSet = new HashSet<string>();
            SelectedSportsSet = new HashSet<string>();

            Clients = await LoadClients();
            
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (IsImportTab)
            {
                await JSRuntime.InvokeVoidAsync("initSelect2", $".{ImportSelect}", "OnImportSelectChanged", true);
                ToastTitle = "Import Margins";
            }
            else
            {
                SelectedClient = Clients.FirstOrDefault().Value;
                await JSRuntime.InvokeVoidAsync("initSelect2", $".{ExportSelect}", "OnExportSelectChanged", true);
                ToastTitle = "Export Margins";
            }
        }
        public async Task OnImportRadioClickEvent()
        {
            IsImportTab = true;
            SelectedClientsSet = new HashSet<string>();            
            await JSRuntime.InvokeVoidAsync("destroySelect2",$"{ExportSelect}");
        }
        public async Task OnExportRadioClickEvent()
        {
            IsImportTab = false;
            SelectedSportsSet = new HashSet<string>();
            await JSRuntime.InvokeVoidAsync("destroySelect2", $".{ImportSelect}");

            Sports = new List<SelectListItemModel<int>>();
            Sports = await LoadSports();
        }

        public async Task OnImportInputFileChanged(InputFileChangeEventArgs e)
        {
            IsUploading = true;

            await InvokeAsync(StateHasChanged);

            if (e.File.Name.Contains(".xlsx"))
            {
                var stream = new MemoryStream();
                await e.File.OpenReadStream(long.MaxValue).CopyToAsync(stream);

                var payload = await ExcelService.ConvertExceltoBulkUpdateConfigMessage(stream, e.File.Name, UserModel.Id, string.Join(",", SelectedClientsSet), MessageBroker);

                IsUploading = false;

                if (payload.StatusCode == StatusCode.Error)
                {
                    ToastMessage = payload.Message;
                    await ShowToast();
                    return;
                }
            }
            else if (e.File.Name.Contains(".zip"))
            {
                var inputStream = new MemoryStream();
                await e.File.OpenReadStream(long.MaxValue).CopyToAsync(inputStream);

                var files = GetFilesFromZipped(inputStream.ToArray());

                if (!files.Any())
                {
                    IsUploading = false;
                    ToastMessage = "Error! No Files found from zip";
                    await ShowToast();
                    return;
                }

                if (files.Any(f => !f.Key.Contains(".xlsx")))
                {
                    IsUploading = false;
                    ToastMessage = "Error! Please zip an excel file only";
                    return;
                }

                var processCount = 0;
                foreach (var fileStream in files)
                {
                    processCount++;
                    //AlertStatus = $"Processing {fileStream.Key}";
                    //AlertMessage = $"Processing {processCount} of {files.Count} file/s..";

                    await InvokeAsync(StateHasChanged);

                    using (MemoryStream ms = new MemoryStream(fileStream.Value))
                    {
                        var payload = await ExcelService.ConvertExceltoBulkUpdateConfigMessage(ms, fileStream.Key, UserModel.Id, string.Join(",", SelectedClientsSet), MessageBroker);

                        if (payload.StatusCode == StatusCode.Error)
                        {
                            IsUploading = false;
                            ToastMessage = payload.Message;
                            await ShowToast();
                            return;
                        }
                    }
                }

                IsUploading = false;
                ToastMessage = "Success! Excel file is being imported. System will notify once done";
                await ShowToast();
            }
            else
            {
                ToastMessage = "Error! Please select an excel file";
                await ShowToast();
            }

            IsUploading = false;
        }

        public async Task OnClientSelectChanged(ChangeEventArgs e)
        {
            SelectedClient = e.Value.ToString();
        }

        public async Task OnExportClickEvent()
        {
            IsUploading = true;
            await InvokeAsync(StateHasChanged);

            var payload = new OnGetMarginSpreadsheetMessage()
            {
                UserId = UserModel.Id,
                ClientCode = SelectedClient,
                SportIds = string.Join(",", SelectedSportsSet)
            };

            MessageBroker.Publish(JsonSerializer.Serialize(payload), "OnGetMarginSpreadsheet2", "r.oe.web.config.ongetmarginspreadsheet2", Guid.NewGuid().ToString());

            IsUploading = false;
            ToastMessage = "Success! Margin configurations are being generated, system will notify once done";
            await ShowToast();
            ModalService.Cancel();
        }

        [JSInvokable("OnImportSelectChanged")]
        public static void OnImportSelectChanged(SelectedModel model)
        {
            if (model.IsSelect)
            {
                if (model.SelectedOptionId == "0")
                {
                    SelectedClientsSet.Clear();
                    SelectedClientsSet.Add(model.SelectedOptionId);
                }
                else
                {
                    SelectedClientsSet.Add(model.SelectedOptionId);
                }
            }
            else
            {
                if (model.SelectedOptionId == "0") 
                {
                    return;
                }

                if (SelectedClientsSet.Contains(model.SelectedOptionId))
                {
                    SelectedClientsSet.Remove(model.SelectedOptionId);
                }
            }
        }

        [JSInvokable("OnExportSelectChanged")]
        public static void OnExportSelectChanged(SelectedModel model)
        {
            if (model.IsSelect)
            {
                if (model.SelectedOptionId == "0")
                {
                    SelectedSportsSet.Clear();
                    SelectedSportsSet.Add(model.SelectedOptionId);
                }
                else
                {
                    SelectedSportsSet.Add(model.SelectedOptionId);
                }
            }
            else
            {
                if (model.SelectedOptionId == "0")
                {
                    return;
                }

                if (SelectedSportsSet.Contains(model.SelectedOptionId))
                {
                    SelectedSportsSet.Remove(model.SelectedOptionId);
                }
            }
        }

        public async Task OnModalCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {
            
        }
        #endregion

        #region Utilities
        private async Task<List<SelectListItemModel<string>>> LoadClients()
        {
            ClientModel clients = new ClientModel();
            clients = (await BackOfficeService.GetClients()).Result;
            if (clients != null)
            {

                if (UserModel.ClientCodes.Any())
                {
                    clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                }
            }

            return clients.Clients.Select(x => new SelectListItemModel<string>()
            {
                Name = x.ClientName,
                Value = x.ClientCode
            }).ToList();
        }

        private async Task<List<SelectListItemModel<int>>> LoadSports()
        {
            var result = (await TradingService.GetSportLookup()).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var sports = result.SportLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            return sports;
        }

        private static Dictionary<string, byte[]> GetFilesFromZipped(byte[] zippedFile)
        {
            using (MemoryStream ms = new MemoryStream(zippedFile))
            using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                return archive.Entries.ToDictionary(x => x.Name, x => ReadStream(x.Open()));
            }
        }

        private static byte[] ReadStream(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private async Task ShowToast()
        {
            await InvokeAsync(StateHasChanged);
            await JSRuntime.InvokeVoidAsync("showToast");
            await Task.Delay(1000);
        }
        #endregion
    }
}
