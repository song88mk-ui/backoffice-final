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

// initialize select2
window.initSelect2 = (className, event) => {
    $(className).select2();

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


window.showToast = () => {
    $('.toast').toast('show');
}

window.enforceNumberValidation = (ele) => {
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
    }
}