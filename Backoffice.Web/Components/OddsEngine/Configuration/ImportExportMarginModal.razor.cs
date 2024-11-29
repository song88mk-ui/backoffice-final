using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Serilog;
using System.IO.Compression;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ImportExportMarginModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private IExcelService ExcelService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public EventCallback OnExportMarginCallback { get; set; }
        public EventCallback<string> OnImportMarginCallback { get; set; }

        public List<AlertModel> Alerts { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }

        public static HashSet<string> SelectedClientsSet { get; set; }
        public static HashSet<string> SelectedSportsSet { get; set; }

        public string Region { get; set; }
        public string SelectedClient { get; set; }
        public string CurrentFileInProcess { get; set; } = string.Empty;
        public string ProcessingMessage { get; set; } = string.Empty;
        public bool IsImportTab { get; set; }
        public bool IsUploading { get; set; }

        public const string ImportSelect = "import-select";
        public const string ExportSelect = "export-select";

        public Guid Guid { get; set; }

        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsImportTab = true;
            IsUploading = false;
            Clients = new List<SelectListItemModel<string>>();
            Sports = new List<SelectListItemModel<int>>();
            Alerts = new List<AlertModel>();

            SelectedClientsSet = new HashSet<string>();
            SelectedSportsSet = new HashSet<string>();
            Region = ModalParameters.TryGet<string>("Region").ToUpper();
            OnExportMarginCallback = ModalParameters.TryGet<EventCallback>("OnExportMarginCallback");
            OnImportMarginCallback = ModalParameters.TryGet<EventCallback<string>>("OnImportMarginCallback");
            TradingService.Region = Region.ToUpper();

            Guid = new Guid();

            Clients = await LoadClients();

        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (IsImportTab)
            {
                await JSRuntime.InvokeVoidAsync("initSelect2", $".{ImportSelect}", "OnImportSelectChanged", true);
            }
            else
            {
                SelectedClient = Clients.FirstOrDefault().Value;
                await JSRuntime.InvokeVoidAsync("initSelect2", $".{ExportSelect}", "OnExportSelectChanged", true);
            }
        }
        public async Task OnImportRadioClickEvent()
        {
            IsImportTab = true;
            SelectedClientsSet = new HashSet<string>();
            await JSRuntime.InvokeVoidAsync("destroySelect2", $"{ExportSelect}");
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
            try
            {
                // Task - Migrate to API call
                return;

                IsUploading = true;
                StateHasChanged();

                if (e.File.Name.Contains(".xlsx"))
                {
                    var stream = new MemoryStream();
                    await e.File.OpenReadStream(long.MaxValue).CopyToAsync(stream);

                    // Task - Migrate to api call
                    //var payload = await ExcelService.ConvertExceltoBulkUpdateConfigMessage(stream, e.File.Name, UserModel.Id, string.Join(",", SelectedClientsSet), MessageBroker);


                    IsUploading = false;
                    //if (payload.StatusCode == StatusCode.Error)
                    //{
                    //    await OnImportMarginCallback.InvokeAsync(payload.Message);
                    //    return;
                    //}
                }
                else if (e.File.Name.Contains(".zip"))
                {
                    var inputStream = new MemoryStream();
                    await e.File.OpenReadStream(long.MaxValue).CopyToAsync(inputStream);

                    var files = GetFilesFromZipped(inputStream.ToArray());

                    if (!files.Any())
                    {
                        IsUploading = false;

                        return;
                    }

                    if (files.Any(f => !f.Key.Contains(".xlsx")))
                    {
                        IsUploading = false;
                        Alerts.Clear();

                        Alerts.Add(new AlertModel()
                        {
                            Status = AlertStatus.Error,
                            Message = "Error! Please zip an excel file only",
                            Opacity = 1.0m
                        });

                        Guid = Guid.NewGuid();

                        return;
                    }

                    var processCount = 0;
                    foreach (var fileStream in files)
                    {
                        processCount++;
                        CurrentFileInProcess = $"Processing {fileStream.Key}";
                        ProcessingMessage = $"Processing {processCount} of {files.Count} file/s..";

                        StateHasChanged();

                        //using (MemoryStream ms = new MemoryStream(fileStream.Value))
                        //{
                        //    // Task - Migrate to api call
                        //    var payload = await ExcelService.ConvertExceltoBulkUpdateConfigMessage(ms, fileStream.Key, UserModel.Id, string.Join(",", SelectedClientsSet), MessageBroker);

                        //    if (payload.StatusCode == StatusCode.Error)
                        //    {
                        //        IsUploading = false;


                        //        return;
                        //    }
                        //}
                    }

                    IsUploading = false;
                }
                else
                {
                    Alerts.Clear();

                    Alerts.Add(new AlertModel()
                    {
                        Status = AlertStatus.Error,
                        Message = $"Invalid file type",
                        Opacity = 1.0m
                    });

                    Guid = Guid.NewGuid();
                    return;
                }

                IsUploading = false;
                ModalService.Cancel();
            }
            catch (Exception ex)
            {
                Alerts.Clear();

                Alerts.Add(new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = $"{ex.Message}",
                    Opacity = 1.0m
                });

                Guid = Guid.NewGuid();

                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }

        public async Task OnClientSelectChanged(ChangeEventArgs e)
        {
            SelectedClient = e.Value.ToString();
        }

        public async Task OnExportClickEvent()
        {
            IsUploading = true;
            await InvokeAsync(StateHasChanged);

            var payload = new OnExportConfigMarginMessage()
            {
                UserId = UserModel.Id,
                ClientCode = SelectedClient,
                SportIds = string.Join(",", SelectedSportsSet)
            };

            // Task - Migrate to api call
            //MessageBroker.Publish(JsonSerializer.Serialize(payload), "OnGetMarginSpreadsheet2", "r.oe.web.config.ongetmarginspreadsheet2", Guid.NewGuid().ToString());

            IsUploading = false;

            ModalService.Cancel();

            await OnExportMarginCallback.InvokeAsync();
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
            var clients = new List<ClientInformationModel>();
            clients = (await TradingService.GetClientList()).Result;

            if (clients != null)
            {
                clients = UserModel.ClientCodes.Any() ? clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList() : new List<ClientInformationModel>();
            }

            return clients?.Select(x => new SelectListItemModel<string>()
            {
                Name = x.ClientName,
                Value = x.ClientCode
            }).ToList() ?? new List<SelectListItemModel<string>>();
        }

        private async Task<List<SelectListItemModel<int>>> LoadSports()
        {
            var result = (await TradingService.GetSportLookup(_translationState.ActiveLanguageCode.ToString())).Result;

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
        #endregion
    }
}
