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
window.initSelect2 = (className) => {
    $(className).select2();
}

window.disableSelect2 = (className) => {
    $(className).prop("disabled", true);
}

window.enableSelect2 = (className) => {
    $(className).prop("disabled", false);
}