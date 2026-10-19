using Backoffice.Web.Components.Administration.MarketSettlement;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.Administration
{
    // BO-102: cancelling under Market Settlement closed the modal with no confirmation.
    //
    // AddMarketSettlementModal.OnCancelModal already guarded the footer Cancel button behind an
    // unsaved-changes prompt (BO-18), but Filter.OnAddMarketSettlementClickEvent opened the modal
    // with the header close (X) button visible; CustomModal wires that button straight to
    // Close(), bypassing OnCancelModal entirely, so the X let a user discard entered
    // status/reason data with no confirmation at all.
    //
    // These tests exercise the code-behind directly (no bUnit render) since none of the methods
    // under test touch anything the Blazor render tree provides beyond the injected services set
    // here, matching how Filter.OnAddMarketSettlementClickEvent and AddMarketSettlementModal's
    // cancel flow are self-contained.
    public class MarketSettlementCancelConfirmationTests
    {
        [Fact]
        public async Task OnAddMarketSettlementClickEvent_OpensModal_WithCloseButtonHidden()
        {
            var modalService = new Mock<IModalService>();
            ModalOptions capturedOptions = null;

            modalService
                .Setup(m => m.Open<AddMarketSettlementModal>(It.IsAny<string>(), It.IsAny<ModalParameters>(), It.IsAny<ModalOptions>()))
                .Callback<string, ModalParameters, ModalOptions>((_, _, options) => capturedOptions = options);

            var filter = new Filter { ModalService = modalService.Object };

            await filter.OnAddMarketSettlementClickEvent();

            modalService.Verify(m => m.Open<AddMarketSettlementModal>(It.IsAny<string>(), It.IsAny<ModalParameters>(), It.IsAny<ModalOptions>()), Times.Once);
            Assert.NotNull(capturedOptions);
            Assert.True(capturedOptions.HideCloseButton == true, "The header close (X) button must be hidden so Cancel always goes through the unsaved-changes confirmation.");
        }

        [Fact]
        public async Task OnCancelModal_WithUnsavedChanges_ShowsConfirmation_AndDoesNotCancelYet()
        {
            var modalService = new Mock<IModalService>();
            var modal = new AddMarketSettlementModal
            {
                ModalService = modalService.Object,
                AddMarketSettlement = new AddMarketSettlementModel()
            };
            modal.AddMarketSettlement.AddMarketSettlementSelections.Add(new AddMarketSettlementSelectionModel
            {
                RowCount = 1,
                ToStatus = 0,
                Reason = "Incorrect settlement"
            });

            await modal.OnCancelModal();

            Assert.True(modal.ShowDiscardConfirm);
            modalService.Verify(m => m.Cancel(), Times.Never);
        }

        [Fact]
        public async Task OnConfirmDiscardChanges_AfterConfirmationShown_StillCancelsTheModal()
        {
            var modalService = new Mock<IModalService>();
            var modal = new AddMarketSettlementModal
            {
                ModalService = modalService.Object,
                AddMarketSettlement = new AddMarketSettlementModel()
            };
            modal.AddMarketSettlement.AddMarketSettlementSelections.Add(new AddMarketSettlementSelectionModel
            {
                RowCount = 1,
                ToStatus = (int)Backoffice.Shared.Enums.BetStatus.Void,
                Reason = string.Empty
            });

            await modal.OnCancelModal();
            Assert.True(modal.ShowDiscardConfirm);

            await modal.OnConfirmDiscardChanges();

            Assert.False(modal.ShowDiscardConfirm);
            modalService.Verify(m => m.Cancel(), Times.Once);
        }

        [Fact]
        public async Task OnCancelModal_WithNoUnsavedChanges_CancelsImmediately_WithoutPrompting()
        {
            var modalService = new Mock<IModalService>();
            var modal = new AddMarketSettlementModal
            {
                ModalService = modalService.Object,
                AddMarketSettlement = new AddMarketSettlementModel()
            };
            modal.AddMarketSettlement.AddMarketSettlementSelections.Add(new AddMarketSettlementSelectionModel
            {
                RowCount = 1,
                ToStatus = 0,
                Reason = string.Empty
            });

            await modal.OnCancelModal();

            Assert.False(modal.ShowDiscardConfirm);
            modalService.Verify(m => m.Cancel(), Times.Once);
        }

        [Fact]
        public async Task OnCancelDiscardChanges_KeepsEnteredData_AndLeavesModalOpen()
        {
            var modalService = new Mock<IModalService>();
            var modal = new AddMarketSettlementModal
            {
                ModalService = modalService.Object,
                AddMarketSettlement = new AddMarketSettlementModel()
            };
            modal.AddMarketSettlement.AddMarketSettlementSelections.Add(new AddMarketSettlementSelectionModel
            {
                RowCount = 1,
                ToStatus = (int)Backoffice.Shared.Enums.BetStatus.Won,
                Reason = "Incorrect settlement"
            });

            await modal.OnCancelModal();
            await modal.OnCancelDiscardChanges();

            Assert.False(modal.ShowDiscardConfirm);
            var row = Assert.Single(modal.AddMarketSettlement.AddMarketSettlementSelections);
            Assert.Equal((int)Backoffice.Shared.Enums.BetStatus.Won, row.ToStatus);
            Assert.Equal("Incorrect settlement", row.Reason);
            modalService.Verify(m => m.Cancel(), Times.Never);
        }
    }
}
