//const Sortable = require("./sortablejs/modular/sortable.core.esm.js");


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

window.getClientTimeZoneOffset = () => {
    return (new Date().getTimezoneOffset()) * -1;
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

window.password_show_hide2 = (ele) => {
    var inputEle = $(ele).prev();

    //for eye
    var showEyeEl = $(ele).find('#show_eye');
    var hideEyeEl = $(ele).find('#hide_eye');

    hideEyeEl.removeClass("d-none");

    if ($(inputEle).attr('type') == 'password') {
        $(inputEle).attr('type', 'text');        

        showEyeEl.css('display', 'none');
        hideEyeEl.css('display', 'block');
    }
    else {
        $(inputEle).attr('type', 'password');

        showEyeEl.css('display', 'block');
        hideEyeEl.css('display', 'none');
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

window.downloadFileFromText = (fileName, text, mimeType) => {
    var blob = new Blob([text], { type: mimeType + ';charset=utf-8;' });

    if (window.navigator && window.navigator.msSaveOrOpenBlob) {
        window.navigator.msSaveOrOpenBlob(blob, fileName);
        return;
    }

    var link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setTimeout(function () {
        URL.revokeObjectURL(link.href);
    }, 100);
}

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
window.initFilterClientName = () => {
    DotNet.invokeMethodAsync('Backoffice.Web', 'OnInvokeFilterEvent');
}

window.filterClientName = (clients) => {
    console.log("filterClient");
    console.log(clients);

    let viz = document.getElementById('tableau-viz');
    let sheet = viz.workbook.activeSheet;
    const filter = sheet.worksheets[sheet.worksheets.length - 1];

    filter.clearFilterAsync("ClientName");
    filter.applyFilterAsync("ClientName", clients, "replace");
}

window.scrollToTopOfElement = (className, speed) => {
    $(className).animate({scrollTop: 0}, speed);
}

window.scrollIntoElement = (elementId) => {
    $(elementId)[0].scrollIntoView();
}

window.showSelectedLeagueCount = () => {
    $('#selectedLeagueWrapper').slideToggle("fast");
}

var netReference = null;

window.loadEventListener = (reference) => {
    netReference = reference;
}
window.enableSportSortOrder = () => {
    const $sportsTree = $('#sportstreeEditable');
    const $allSports = $('#allSportsEditable');

    $('.connect-list').multisortable({
        stop: function () {
            const updateSortOrder = (parentSelector) => {
                const result = [];
                let index = 1;

                $(parentSelector + ' .sport-container').each(function () {
                    result.push({
                        rowId: this.dataset.id, // Use native `dataset` for performance
                        sortOrder: index++
                    });
                });

                return result;
            };
            
            const treeData = updateSortOrder('#sportstreeEditable');
            const allSportsData = updateSortOrder('#allSportsEditable');
            
            netReference.invokeMethodAsync('UpdateSportSetting', treeData, allSportsData);
        },
    });
    
    $sportsTree.add($allSports).sortable({
        handle: '.drag-container',
        connectWith: '.connect-list',
        placeholder: 'py-3 mb-2 border border-2 border-info rounded',
        scroll: true,
        tolerance: 'pointer'
    });
};


window.enableSortOrder2 = (id, childId, sportId, segmentId) => {
    $(id).sortable({
        handle: '.drag-container',
        placeholder: 'py-3 mb-2 border border-2 border-info rounded',
        scroll: true,
        tolerance: 'pointer',

        stop: function (event, ui) {
            var data = [];
            var index = 1;

            $(childId).each(function () {
                data.push({
                    rowId: $(this).attr('data-id'),
                    sortOrder: index
                });

                index++;
            });

            netReference.invokeMethodAsync('UpdateSportSetting', sportId, segmentId, data);
        },
    });
};

window.disableSortOrder2 = (id) => {
    $(id).sortable('disable');
}

window.getSelectedSports = () => {
    var data = [];

    $('#allSportsEditable .sport-container.selected').each(function () {
        data.push($(this).attr('data-id'));
    });

    return data;
}

window.getCurrentSortOrder = (id) => {
    var data = [];
    var index = 1;

    $(id).each(function () {

        data.push({
            rowId: $(this).attr('data-id'),
            sortOrder: index
        });

        index++;
    });

    return data;
}

// Selector Market Templates and League Sorting: drag rows via the .drag-handle column. On drop the
// DOM move is reverted (sortable('cancel')) and the new order is pushed to Blazor, which re-renders
// the keyed rows and renumbers the sort orders - keeps the DOM and the render tree in sync.
// methodName is the [JSInvokable] to call on dotnetRef, Selector Market Templates by default.
window.enableSelectorTemplateSort = (tableSelector, dotnetRef, methodName = 'UpdateSelectorTemplateOrder') => {
    var tbody = tableSelector + ' > tbody';

    $(tbody).sortable({
        handle: '.drag-handle',
        placeholder: 'placeholder',
        stop: function () {
            var rowKeys = [];

            $(tbody + ' > tr[data-id]').each(function () {
                rowKeys.push($(this).attr('data-id'));
            });

            $(this).sortable('cancel');

            dotnetRef.invokeMethodAsync(methodName, rowKeys);
        },
    });
}


window.setCookie = (name, value, days) => {
    let expires = "";
    if (days) {
        const date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/";
}

window.getCookie = (name) => {
    const nameEQ = name + "=";
    const ca = document.cookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) === ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
}

window.blockPageClose = function (message) {
    window.onbeforeunload = function () {
        return message;
    };
};

window.allowPageClose = function () {
    window.onbeforeunload = null;
};

window.languageChanged = function (name, value, days) {
    setCookie(name, value, days);
    window.location.reload();
}