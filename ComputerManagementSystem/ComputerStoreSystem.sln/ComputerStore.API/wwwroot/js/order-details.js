(() => {
    'use strict';
    const dialog = document.getElementById('orderDetailsDialog');
    const body = document.getElementById('orderDetailsBody');
    const title = document.getElementById('orderDetailsTitle');
    if (!dialog || !body || !title) return;
    let trigger = null;
    let previousOverflow = '';
    document.addEventListener('click', event => {
        const button = event.target.closest('[data-order-details]');
        if (!button || dialog.open) return;
        const id = button.dataset.orderDetails;
        if (!/^\d+$/.test(id)) return;
        const template = document.getElementById(`order-detail-${id}`);
        if (!(template instanceof HTMLTemplateElement)) return;
        trigger = button;
        title.textContent = `Chi tiết đơn hàng #${id}`;
        // Clone các node đã được Razor mã hóa; không chèn dữ liệu người dùng vào innerHTML.
        body.replaceChildren(template.content.cloneNode(true));
        previousOverflow = document.body.style.overflow;
        dialog.showModal();
        document.body.style.overflow = 'hidden';
        body.scrollTop = 0;
    });
    dialog.querySelectorAll('[data-close-order-dialog]').forEach(button => {
        button.addEventListener('click', () => dialog.close());
    });
    // Esc và Tab do dialog gốc của trình duyệt quản lý.
    dialog.addEventListener('click', event => {
        if (event.target !== dialog) return;
        const bounds = dialog.getBoundingClientRect();
        if (event.clientX < bounds.left || event.clientX > bounds.right ||
            event.clientY < bounds.top || event.clientY > bounds.bottom) dialog.close();
    });
    dialog.addEventListener('close', () => {
        document.body.style.overflow = previousOverflow;
        body.replaceChildren();
        if (trigger?.isConnected) trigger.focus({ preventScroll: true });
        trigger = null;
    });
})();
