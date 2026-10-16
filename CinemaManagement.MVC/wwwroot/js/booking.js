(function () {
    const root = document.getElementById('bookingRoot');
    const showtimeId = parseInt(root.dataset.showtimeId, 10);
    const discountPercent = parseFloat(root.dataset.discountPercent) || 0;
    const vndPerPoint = parseInt(root.dataset.vndPerPoint, 10) || 10000;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const $ = id => document.getElementById(id);
    const fmt = v => Number(v).toLocaleString('vi-VN') + 'đ';
    const POLL_MS = 10000;   // chu kỳ hỏi lại máy chủ khi không có kết nối trực tiếp

    let seatMap = null;
    const seatsById = new Map();     // seatId -> seat
    const seatButtons = new Map();   // seatId -> <button>
    const selected = new Map();      // seatId -> seat đang chọn
    let myPending = new Set();       // ghế mình đang gửi thanh toán (để không báo "người khác đặt")
    let pollTimer = null;

    // Tên hiển thị của ghế (ưu tiên displayName của API, ví dụ "A1")
    const seatLabel = seat => seat.displayName || (seat.rowLabel + seat.seatNumber);

    // API không trả giá riêng từng ghế => dùng giá cơ bản của suất chiếu
    function seatPrice() { return seatMap.basePrice; }

    // ---------- Load sơ đồ ghế ----------
    async function loadSeatMap() {
        selected.clear();
        try {
            const res = await fetch('/Booking/SeatMapData?showtimeId=' + showtimeId, { cache: 'no-store' });
            const json = await res.json();
            if (!json.success) { showMessage(json.message, 'danger'); $('movieTitle').textContent = 'Lỗi'; return; }
            seatMap = json.data;

            seatsById.clear();
            seatMap.seats.forEach(s => seatsById.set(s.seatId, s));

            $('movieTitle').textContent = seatMap.movieTitle;
            $('showtimeInfo').textContent =
                new Date(seatMap.startTime).toLocaleString('vi-VN', {
                    hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric'
                }) + ' — ' + seatMap.hallName;
            renderSeats();
            updateSummary();
        } catch {
            showMessage('Không tải được sơ đồ ghế.', 'danger');
        }
    }

    function renderSeats() {
        const grid = $('seatGrid');
        grid.replaceChildren();
        seatButtons.clear();

        // Phòng chiếu chưa có ghế
        if (!seatMap.seats || seatMap.seats.length === 0) {
            showMessage('Phòng chiếu này chưa có ghế. Vui lòng liên hệ quản trị viên.', 'warning');
            return;
        }

        // Gom ghế theo hàng
        const rows = {};
        seatMap.seats.forEach(s => (rows[s.rowLabel] = rows[s.rowLabel] || []).push(s));

        Object.keys(rows).sort().forEach(rowName => {
            const rowEl = document.createElement('div');
            rowEl.className = 'seat-row';

            const label = document.createElement('span');
            label.className = 'row-label';
            label.textContent = rowName;
            rowEl.appendChild(label);

            rows[rowName].sort((a, b) => a.seatNumber - b.seatNumber).forEach(seat => {
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.textContent = seat.seatNumber;
                btn.title = seatLabel(seat) + ' — ' + fmt(seatPrice()) +
                            (seat.seatType ? ' (' + seat.seatType + ')' : '');
                btn.className = 'seat ' + (seat.isBooked ? 'booked' : (selected.has(seat.seatId) ? 'selected' : 'available'));
                btn.disabled = seat.isBooked;
                if (!seat.isBooked) btn.addEventListener('click', () => toggleSeat(seat, btn));
                seatButtons.set(seat.seatId, btn);
                rowEl.appendChild(btn);
            });
            grid.appendChild(rowEl);
        });
    }

    // ---------- Chọn / bỏ chọn ghế ----------
    function toggleSeat(seat, btn) {
        if (seat.isBooked) return;

        if (selected.has(seat.seatId)) {
            selected.delete(seat.seatId);
            btn.className = 'seat available';
        } else {
            selected.set(seat.seatId, seat);
            btn.className = 'seat selected';
        }
        updateSummary();
    }

    // ---------- Tổng tiền real-time (ước tính; số chính thức do API trả về) ----------
    function updateSummary() {
        const seats = [...selected.values()];
        const total = seats.length * (seatMap ? seatPrice() : 0);
        const discount = Math.round(total * discountPercent / 100);
        const final = total - discount;
        const points = Math.floor(final / vndPerPoint);

        $('selectedSeats').textContent = seats.length
            ? seats.map(seatLabel).join(', ')
            : 'Chưa chọn ghế';
        $('totalAmount').textContent = fmt(total);
        $('discountAmount').textContent = '-' + fmt(discount);
        $('pointsEarned').textContent = points.toLocaleString('vi-VN');
        $('finalAmount').textContent = fmt(final);
        $('estimateNote').textContent = seats.length ? 'Số liệu ước tính, hệ thống sẽ tính chính thức khi thanh toán.' : '';
        $('btnBook').disabled = seats.length === 0;
    }

    // ---------- Đánh dấu ghế vừa có người đặt (gọi khi nhận thông báo trực tiếp) ----------
    function applyBooked(ids) {
        if (!seatMap || !ids || ids.length === 0) return;

        const lost = [];
        let selectionChanged = false;

        ids.forEach(id => {
            const seat = seatsById.get(id);
            if (!seat || seat.isBooked) return;

            seat.isBooked = true;

            const btn = seatButtons.get(id);
            if (btn) {
                btn.className = 'seat booked just-booked';
                btn.disabled = true;
            }

            if (selected.has(id)) {
                selected.delete(id);
                selectionChanged = true;
                if (!myPending.has(id)) lost.push(seatLabel(seat));
            }
        });

        if (selectionChanged) updateSummary();

        if (lost.length > 0) {
            showMessage('Ghế ' + lost.join(', ') + ' vừa được người khác đặt, đã bỏ khỏi danh sách bạn chọn.', 'warning');
        }
    }

    // Hỏi lại máy chủ để bù những thông báo có thể đã bỏ lỡ
    async function syncFromServer() {
        try {
            const res = await fetch('/Booking/SeatMapData?showtimeId=' + showtimeId, { cache: 'no-store' });
            const json = await res.json();
            if (!json.success) return;

            const bookedIds = json.data.seats.filter(s => s.isBooked).map(s => s.seatId);
            applyBooked(bookedIds);
        } catch {
            // Bỏ qua, lần sau thử lại
        }
    }

    // ---------- Cập nhật trực tiếp (SignalR) ----------
    function setLive(state) {
        const el = $('liveStatus');
        if (!el) return;

        const text = {
            live: 'Đang cập nhật trực tiếp',
            connecting: 'Đang kết nối lại...',
            offline: 'Không có kết nối trực tiếp, đang cập nhật mỗi 10 giây'
        };

        el.dataset.state = state;
        el.querySelector('.live-status__text').textContent = text[state];
    }

    function startPolling() {
        if (pollTimer) return;
        pollTimer = setInterval(syncFromServer, POLL_MS);
    }

    function stopPolling() {
        if (!pollTimer) return;
        clearInterval(pollTimer);
        pollTimer = null;
    }

    async function startRealtime() {
        // Không tải được thư viện (mất mạng, bị chặn): vẫn cập nhật bằng cách hỏi định kỳ
        if (typeof signalR === 'undefined') {
            setLive('offline');
            startPolling();
            return;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/seats')
            .withAutomaticReconnect()
            .build();

        connection.on('SeatsBooked', payload => {
            if (!payload || payload.showtimeId !== showtimeId) return;
            applyBooked(payload.seatIds || []);
        });

        connection.onreconnecting(() => {
            setLive('connecting');
            startPolling();
        });

        connection.onreconnected(async () => {
            await connection.invoke('JoinShowtime', showtimeId);
            await syncFromServer();
            stopPolling();
            setLive('live');
        });

        connection.onclose(() => {
            setLive('offline');
            startPolling();
        });

        try {
            await connection.start();
            await connection.invoke('JoinShowtime', showtimeId);
            await syncFromServer();   // bù khoảng trống giữa lúc tải sơ đồ và lúc vào phòng
            stopPolling();
            setLive('live');
        } catch {
            setLive('offline');
            startPolling();
        }
    }

    // ---------- Submit booking ----------
    async function submitBooking() {
        const btn = $('btnBook');
        btn.disabled = true;
        showMessage('Đang xử lý...', 'info');
        myPending = new Set(selected.keys());

        try {
            const res = await fetch('/Booking/Create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify({ showtimeId, seatIds: [...selected.keys()] })
            });
            const json = await res.json();

            if (json.success) {
                const r = json.data;
                // Dữ liệu chính thức từ API (BookingResponse)
                const msg =
                    `Đặt vé thành công! Mã đặt vé: ${r.bookingCode} — ${r.totalSeats} ghế, ` +
                    `thành tiền ${fmt(r.finalAmount)}, +${r.pointsEarned} điểm.`;
                await loadSeatMap();          // ghế vừa đặt chuyển sang xám
                showMessage(msg, 'success');  // hiện thông báo sau khi reload
            } else {
                if (json.conflict) {
                    await loadSeatMap();
                    showMessage(json.message, 'danger');
                } else {
                    showMessage(json.message || 'Đặt vé thất bại.', 'danger');
                    btn.disabled = selected.size === 0;
                }
            }
        } catch {
            showMessage('Không kết nối được máy chủ.', 'danger');
            btn.disabled = selected.size === 0;
        } finally {
            myPending = new Set();
        }
    }

    function showMessage(text, type) {
        const el = $('bookingMessage');
        el.className = 'alert alert-' + type + ' mt-3';
        el.textContent = text;
    }

    $('btnBook').addEventListener('click', submitBooking);

    loadSeatMap().then(startRealtime);
})();