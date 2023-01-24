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

// initialize table sorting with only specified sortable items
window.initSortItems = (className, items, handler) => {
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
        $(className).select2({closeOnSelect: false});
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

window.getSortOrder = (className, event) => {
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


window.showToast = (isAutoHide) => {
    if (isAutoHide !== undefined) {
        $('.toast').toast({
            autohide: isAutoHide
        });
    }
    
    $('.toast').toast('show');
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
            if ($(ele).val() > 0 && $('#totalCount').val() != 50) {
                $('#filterAddBtn').prop('disabled', false);
            }            
            else {
                $('#filterAddBtn').prop('disabled', true);
            }

            if ($(ele).hasClass('border-danger')) {
                $('#saveChangesBtn').prop('disabled', true);
            }
        }
    }
}

function password_show_hide() {
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