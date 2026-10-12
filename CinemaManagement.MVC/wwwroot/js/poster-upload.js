(function () {
    var input = document.getElementById('posterFile');
    if (!input) return;

    var preview = document.getElementById('posterPreview');
    var errorBox = document.getElementById('posterError');
    var original = preview.innerHTML;       // ảnh hiện tại hoặc chữ "Chưa có ảnh"
    var MAX_BYTES = 3 * 1024 * 1024;
    var ALLOWED = ['image/jpeg', 'image/png', 'image/webp'];

    function reject(message) {
        errorBox.textContent = message;
        input.value = '';
        preview.innerHTML = original;
    }

    input.addEventListener('change', function () {
        errorBox.textContent = '';

        var file = input.files && input.files[0];
        if (!file) {
            preview.innerHTML = original;
            return;
        }

        if (ALLOWED.indexOf(file.type) === -1) {
            reject('Chỉ chấp nhận ảnh JPG, PNG hoặc WebP.');
            return;
        }

        if (file.size > MAX_BYTES) {
            reject('Ảnh poster tối đa 3 MB.');
            return;
        }

        var img = document.createElement('img');
        img.alt = 'Xem trước poster';
        img.src = URL.createObjectURL(file);
        preview.replaceChildren(img);
    });
})();