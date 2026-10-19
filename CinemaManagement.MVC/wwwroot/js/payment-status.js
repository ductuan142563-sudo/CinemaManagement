(function () {
    var panel = document.getElementById('payPanel');
    if (!panel) return;

    var code = panel.dataset.code;
    if (!code) return;

    async function check() {
        if (document.hidden) return;   // tab đang ẩn thì chờ lần sau

        try {
            var res = await fetch('/Concession/Status?code=' + encodeURIComponent(code), {
                cache: 'no-store',
                headers: { 'Accept': 'application/json' }
            });
            if (!res.ok) return;

            var data = await res.json();
            if (data && data.found && (data.paymentStatus === 'Paid' || data.status === 'Cancelled')) {
                window.location.reload();
            }
        } catch (e) {
            // Bỏ qua (mất mạng, hết phiên đăng nhập...), lần sau thử lại
        }
    }

    setInterval(check, 5000);
})();