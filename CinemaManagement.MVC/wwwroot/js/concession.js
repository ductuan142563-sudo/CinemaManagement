(function () {
    var form = document.getElementById('orderForm');
    if (!form) return;

    var MAX_PER_ITEM = 10;
    var MAX_TOTAL = 30;

    var inputs = Array.prototype.slice.call(form.querySelectorAll('.qty-input'));
    var linesEl = document.getElementById('orderLines');
    var totalEl = document.getElementById('orderTotal');
    var submitBtn = document.getElementById('btnOrder');

    function fmt(value) {
        return Number(value).toLocaleString('vi-VN') + 'đ';
    }

    function qty(input) {
        var n = parseInt(input.value, 10);
        return isNaN(n) || n < 0 ? 0 : n;
    }

    function totalQty() {
        return inputs.reduce(function (sum, i) { return sum + qty(i); }, 0);
    }

    // Đặt số lượng cho một món, không vượt giới hạn mỗi món và giới hạn cả đơn
    function setQty(input, value) {
        var others = totalQty() - qty(input);
        var max = Math.min(MAX_PER_ITEM, MAX_TOTAL - others);
        input.value = Math.max(0, Math.min(value, max));
    }

    function refresh() {
        linesEl.replaceChildren();
        var total = 0;
        var count = 0;

        inputs.forEach(function (input) {
            var q = qty(input);
            if (q === 0) return;

            var price = parseFloat(input.dataset.price) || 0;
            total += price * q;
            count += 1;

            var li = document.createElement('li');
            var name = document.createElement('span');
            var amount = document.createElement('span');
            name.textContent = input.dataset.name + ' \u00d7 ' + q;
            amount.textContent = fmt(price * q);
            li.appendChild(name);
            li.appendChild(amount);
            linesEl.appendChild(li);
        });

        if (count === 0) {
            var empty = document.createElement('li');
            empty.className = 'text-muted';
            empty.textContent = 'Chưa chọn món nào.';
            linesEl.appendChild(empty);
        }

        totalEl.textContent = fmt(total);
        submitBtn.disabled = count === 0;
    }

    // Bấm nút + / -
    form.addEventListener('click', function (e) {
        var btn = e.target.closest('.qty-btn');
        if (!btn) return;

        var input = btn.parentElement.querySelector('.qty-input');
        var delta = btn.dataset.action === 'inc' ? 1 : -1;
        setQty(input, qty(input) + delta);
        refresh();
    });

    // Gõ số trực tiếp
    inputs.forEach(function (input) {
        input.addEventListener('input', function () {
            setQty(input, qty(input));
            refresh();
        });
        input.addEventListener('change', function () {
            setQty(input, qty(input));
            refresh();
        });
    });

    refresh();
})();