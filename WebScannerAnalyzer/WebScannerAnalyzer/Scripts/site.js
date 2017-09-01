var siteRoot = '';

String.prototype.endsWith = function (suffix) {
    return this.indexOf(suffix, this.length - suffix.length) !== -1;
};

function showDialog(header, message) {

    if (header !== undefined)
        $("#modal .modal-title").html(header);

    if (message !== undefined)
        $("#modal .modal-body").html(message);

    $("#modal").modal("show");
}

$(document).ready(function() {
    $('a[href="' + this.location.pathname + '"]').parents('li,ul').addClass('active');
});