// initialize table sorting
window.initSort = (className, handler) => {
    var tableClass = className + ' > tbody';

    $(tableClass).sortable({
        handle: handler,
        placeholder: "placeholder",
        start: function (event, ui) {

            var item = $(ui.item);
            var headers = $(className + '> thead > tr').children('th');

            for (var i = 0; i < headers.length; i++) {
                var rect = $(headers)[i].getBoundingClientRect();

                $(item).children('td')[i].style.width = `${rect.width}px`;
            }
        },
        stop: function (event, ui) {
            var item = $(ui.item);
            var columns = $(item).children('td');

            for (var i = 0; i < columns.length; i++) {
                columns[i].style.width = '';
            }
        }
    });
}

// disable table sorting
window.disableSort = (className) => {
    var tableClass = className + ' > tbody';

    $(tableClass).sortable('disable');
}

// enable table sorting
window.enableSort = (className) => {
    var tableClass = className + ' > tbody';

    $(tableClass).sortable('option', 'disabled', false);
}

// destroy the sortable funtionality completely
window.destroySort = (className) => {
    var tableClass = className + ' > tbody';

    $(tableClass).sortable('destroy');
}

// initialize select2
window.initSelect2 = (className, event, closeOnSelect) => {
    if (closeOnSelect) {
        $(className).select2({ closeOnSelect: false });
    }
    else {
        $(className).select2();
    }

    $(className).on('select2:select', function (e) {

        var data = {
            parentId: e.currentTarget.id,
            selectedOptionId: e.params.data.id,
            isSelect: true
        }

        DotNet.invokeMethodAsync('Backoffice.Web', event, data);
    });

    $(className).on('select2:unselecting', function (e) {
        var data = {
            parentId: e.currentTarget.id,
            selectedOptionId: e.params.args.data.id,
            isSelect: false
        }

        DotNet.invokeMethodAsync('Backoffice.Web', event, data);
    })
}

window.destroySelect2 = (className) => {
    $(className).select2('destroy');
}

window.disableSelect2 = (className) => {
    $(className).prop("disabled", true);
}

window.enableSelect2 = (className) => {
    $(className).prop("disabled", false);
}

window.getSortOrder = (className) => {
    var tableClass = className + ' > tbody';

    let data = [];

    $(tableClass).children('tr').each(function (index) {
        var market = {
            RowId: $(this).attr('id'),
            SortOrder: index + 1
        };

        data.push(JSON.stringify(market));
    });

    var concat = data.join("\r\n");
    return new Blob([concat], { type: "application/txt" });
}


window.showToast = (className) => {
    $('.' + className).toast({
        autohide: false
    });

    $('.toast-container').prepend($('.' + className));

    $('.' + className).on('hidden.bs.toast', (evt) => {
        $('.toast-container').remove($('.' + className));
    });

    $('.' + className).toast('show');
}

window.enforceNumberValidation = (ele, limit) => {
    if ($(ele).data('decimal') != null) {
        // found valid rule for decimal
        var decimal = parseInt($(ele).data('decimal')) || 0;
        var val = $(ele).val();
        if (decimal > 0) {
            var splitVal = val.split('.');
            if (splitVal.length == 2 && splitVal[1].length > decimal) {
                // user entered invalid input
                $(ele).val(splitVal[0] + '.' + splitVal[1].substr(0, decimal));
            }
        } else if (decimal == 0) {
            // do not allow decimal place
            var splitVal = val.split('.');
            if (splitVal.length > 1) {
                // user entered invalid input
                $(ele).val(splitVal[0]); // always trim everything after '.'
            }
        }

        if (limit !== undefined) {
            if ($(ele).val() > limit || $(ele).val() <= 0) {
                if (!$(ele).hasClass('border-danger')) {
                    $(ele).toggleClass('border-danger');
                }
            }
            else {
                if ($(ele).hasClass('border-danger')) {
                    $(ele).toggleClass('border-danger');
                }
            }
        }

        //for parlay bonus only        
        if ($('#filterAddBtn').length == 1) {
            if ($(ele).val() > 0 && $('#totalCount').val() < 50) {
                $('#filterAddBtn').prop('disabled', false);
            }
            else {
                $('#filterAddBtn').prop('disabled', true);
            }

            DotNet.invokeMethodAsync('Backoffice.Web', 'DisableSaveButtonOnInput', val, $(ele).attr('id'));

            //if ($('.parlay-bonus-input.border-danger').length > 0 || $('#invalidCount').val() > 0) {
            //    //$('#saveChangesBtn').prop('disabled', true);                  
            //    //DotNet.invokeMethodAsync('Backoffice.Web', 'DisableSaveButtonOnInput', true);
            //}
            //else {
            //    //$('#saveChangesBtn').prop('disabled', false);                            
            //    //DotNet.invokeMethodAsync('Backoffice.Web', 'DisableSaveButtonOnInput', false);
            //}
        }
    }
}

window.disableSaveButton = (value) => {
    $('#saveChangesBtn').prop('disabled', value);
}

window.password_show_hide = () => {
    var x = document.getElementById("password");
    var show_eye = document.getElementById("show_eye");
    var hide_eye = document.getElementById("hide_eye");
    hide_eye.classList.remove("d-none");
    if (x.type === "password") {
        x.type = "text";
        show_eye.style.display = "none";
        hide_eye.style.display = "block";
    } else {
        x.type = "password";
        show_eye.style.display = "block";
        hide_eye.style.display = "none";
    }
}

//function password_show_hide() {
//    var x = document.getElementById("password");
//    var show_eye = document.getElementById("show_eye");
//    var hide_eye = document.getElementById("hide_eye");
//    hide_eye.classList.remove("d-none");
//    if (x.type === "password") {
//        x.type = "text";
//        show_eye.style.display = "none";
//        hide_eye.style.display = "block";
//    } else {
//        x.type = "password";
//        show_eye.style.display = "block";
//        hide_eye.style.display = "none";
//    }
//}

window.ValidateParlayBonusMinOdds = (refEl) => {
    var element = $(refEl);
    var isDisabled = false;

    if (element.val() < 1.20) {
        isDisabled = true;
    }

    if (element.val().indexOf('.') >= 1) {
        //input has .
        //split the element first to check if there are chars after .
        if (element.val().split('.').length > 1) {
            //now check the length after .
            if (element.val().split('.')[1] > 2) {
                var splitVal = element.val().split('.');
                element.val(splitVal[0] + '.' + splitVal[1].substr(0, 2));
            }
        }
    }

    DotNet.invokeMethodAsync('Backoffice.Web', 'DisableSaveButton', isDisabled);
}

// initialize table sorting with only specified sortable items
// as of right now codes below are only appliable for Sport Settings
window.initSortItems = (className, items, handler, categorySelector) => {
    var tableClass = className + ' > tbody';

    $(tableClass).sortable({
        handle: handler,
        placeholder: "placeholder",
        items: items,
        start: function (event, ui) {
            var item = $(ui.item);
            var headers = $(className + '> thead > tr').children('th');

            for (var i = 0; i < headers.length; i++) {
                var rect = $(headers)[i].getBoundingClientRect();

                $(item).children('td')[i].style.width = `${rect.width}px`;
            }
        },
        stop: function (event, ui) {
            var item = $(ui.item);
            var columns = $(item).children('td');

            for (var i = 0; i < columns.length; i++) {
                columns[i].style.width = '';
            }

            var id = item.attr('id');
            var index = item.index(categorySelector);

            DotNet.invokeMethodAsync('Backoffice.Web', 'OnStopSortOrder', id, index);
        }
    });
}

window.getSortOrderBySelector = (className, selector) => {
    var tableClass = className + ' > tbody';

    let data = [];

    $(tableClass).children(selector).each(function (index) {
        var market = {
            RowId: $(this).attr('id'),
            SortOrder: index + 1
        };

        data.push(JSON.stringify(market));
    });

    var concat = data.join("\r\n");
    return new Blob([concat], { type: "application/txt" });
}

window.loadSegmentItems = (id, collection) => {
    var mainRow = $(id);
    var rowChild;

    $.each(collection, function (index, value) {
        var colNo = $("<td class='text-center'></td>").append(`<span class="ms-2">${value.sortOrder}</span>`);
        var colCheckbox = $("<td></td>");
        var colName = $("<td></td>").append(`<div class="d-flex justify-content-between"><span class="ms-2">${value.segmentName}</span><span class="dropdown-cat dropdown-league"><i class="fa fa-chevron-down"></i></span></div>`);
        var colAction = $("<td class='text-center'></td>").append(`<a class="link-bars me-2 drag-handle" href="javascript:(0)"><i class="fa fa-bars"></i></a>`)

        rowChild = $(`<tr id="${value.id}" class="${value.rowClass}"></tr>`)
            .append(colNo).append(colCheckbox).append(colName).append(colAction);

        rowChild.insertAfter(mainRow);

        // user the currentChild as the main row for the next insertion
        mainRow = rowChild;
    });

    expandButton('.sport-row', id);
    $('tr.segment-row .dropdown-league .fa').bind('click', leagueEventHandler);
}

window.loadLeagueItems = (id, collection) => {
    var mainRow = $(id);
    var rowChild;

    $.each(collection, function (index, value) {
        var colNo = $("<td class='text-center fst-italic'></td>").append(`<span class="ms-4">${value.sortOrder}</span>`);
        var colCheckbox = $("<td></td>");
        var colName = $("<td></td>").append(`<div class="d-flex justify-content-between"><span class="ms-2">${value.leagueName}</span></div>`);
        var colAction = $("<td class='text-center'></td>").append(`<a class="link-bars me-2 drag-handle" href="javascript:(0)"><i class="fa fa-bars"></i></a>`)

        rowChild = $(`<tr id="${value.id}" class="${value.rowClass}"></tr>`)
            .append(colNo).append(colCheckbox).append(colName).append(colAction);

        rowChild.insertAfter(mainRow);

        // user the currentChild as the main row for the next insertion
        mainRow = rowChild;
    });

    expandButton('.segment-row', id);
}

window.unloadItems = (className, rowClass) => {
    var tableClass = className + ' > tbody';

    if (rowClass == '.segment-row') {
        $('tr.segment-row .dropdown-cat .fa').unbind('click', leagueEventHandler);
    }

    /*collapseButton(rowClass);*/
    $(tableClass).children(rowClass).remove();
}

var leagueEventHandler = function () {
    var row = $(this).closest('tr');

    DotNet.invokeMethodAsync('Backoffice.Web', 'OnInvokeSegmentClick', row.attr('id'));
}

window.expandButton = (selector, id) => {
    collapseButton(selector);

    var classSelector = `${id} .dropdown-cat .fa`;

    // reset all dropdowns
    $(classSelector).removeClass('fa-chevron-up');
    $(classSelector).removeClass('fa-chevron-down');

    $(classSelector).addClass('fa-chevron-up');
}

window.collapseButton = (selector) => {
    var classSelector = `${selector} .dropdown-cat .fa`;

    // reset all dropdowns
    $(classSelector).removeClass('fa-chevron-up');
    $(classSelector).removeClass('fa-chevron-down');

    $(classSelector).addClass('fa-chevron-down');
}

window.validateManualBetSettlementData = () => {
    var invalidSelectCount = 0;
    var invalidReasonCount = 0;

    $('td .form-select').each(function (index) {
        if ($(this).val() == '0') {
            $('.modal-settle-bets').prop("disabled", true);            
            invalidSelectCount += 1;
        }
    });

    $('td .reason').each(function (index) {
        if ($(this).val() == '') {
            $('.modal-settle-bets').prop("disabled", true);      
            invalidReasonCount += 1;
        }
    });

    if ((invalidReasonCount == 0 && invalidSelectCount == 0)) {
        $('.modal-settle-bets').prop("disabled", false);
    }    
}

window.resetManualBetSettlementInputs = (index) => {
    $($('td .form-select')[index]).val('0');    

    $($('td .reason')[index]).val('');

    $('.modal-settle-bets').prop("disabled", true);
}

//tableau filtering
window.filterClientName = (clients) => {
    console.log("filterClient");
    console.log(clients);

    let viz = document.getElementById('tableau-viz');
    let sheet = viz.workbook.activeSheet;
    const filter = sheet.worksheets[sheet.worksheets.length - 1];

    filter.clearFilterAsync("ClientName");
    filter.applyFilterAsync("ClientName", JSON.parse(clients), "replace");
}

window.filterClientName2 = () => {
    console.log("filterClient");

    let viz = document.getElementById('tableau-viz');
    let sheet = viz.workbook.activeSheet;
    const filter = sheet.worksheets[sheet.worksheets.length - 1];

    filter.clearFilterAsync("ClientName");
    filter.applyFilterAsync("ClientName", ["GURUPLAY", "10 BET JAPAN"], "replace");
}
