
(() => {
    const token = document.querySelector('meta[name="request-verification-token"]')?.content;
    const cartCountEl = document.getElementById('cart-count');

    async function addToCart(productId, qty = 1) {
        const res = await fetch('/Cart/Add', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ id: productId, qty })
        });

        if (!res.ok) return;
        const data = await res.json();
        if (data?.success && cartCountEl) {
            cartCountEl.textContent = data.cartCount ?? 0;
        }
    }

    document.querySelectorAll('.btn-add-to-cart').forEach(btn => {
        btn.addEventListener('click', () => {
            const id = parseInt(btn.dataset.productId, 10);
            const qty = parseInt(btn.dataset.qty || '1', 10);
            addToCart(id, qty);
        });
    });
})();