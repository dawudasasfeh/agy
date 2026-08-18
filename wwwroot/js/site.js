/**
 * Antigravity Gear E-Commerce Client Interactions
 * Team 5: Bashar, Shorouq, Dawood
 */

(function () {
    'use strict';

    // --- TOAST NOTIFICATIONS ---
    function showToast(message, type = 'success') {
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.style.position = 'fixed';
            container.style.bottom = '20px';
            container.style.right = '20px';
            container.style.zIndex = '9999';
            container.style.display = 'flex';
            container.style.flexDirection = 'column';
            container.style.gap = '10px';
            document.body.appendChild(container);
        }

        const toast = document.createElement('div');
        toast.className = 'animate-fade-in-up';
        toast.style.background = 'rgba(17, 24, 39, 0.95)';
        toast.style.backdropFilter = 'blur(10px)';
        toast.style.border = '1px solid rgba(255, 255, 255, 0.1)';
        
        if (type === 'success') {
            toast.style.borderLeft = '4px solid #10b981'; // Wilderness Green
        } else if (type === 'error') {
            toast.style.borderLeft = '4px solid #ef4444'; // Cancelled/Alert Red
        } else {
            toast.style.borderLeft = '4px solid #ff6b35'; // Campfire Orange
        }

        toast.style.color = '#f8fafc';
        toast.style.padding = '12px 24px';
        toast.style.borderRadius = '8px';
        toast.style.boxShadow = '0 10px 15px -3px rgba(0, 0, 0, 0.3)';
        toast.style.display = 'flex';
        toast.style.alignItems = 'center';
        toast.style.justifyContent = 'space-between';
        toast.style.gap = '15px';
        toast.style.minWidth = '280px';
        toast.style.transition = 'all 0.5s ease';

        const icon = type === 'success' ? 'bi-check-circle-fill' : type === 'error' ? 'bi-exclamation-triangle-fill' : 'bi-info-circle-fill';
        const iconColor = type === 'success' ? '#10b981' : type === 'error' ? '#ef4444' : '#ff6b35';

        toast.innerHTML = `
            <div style="display: flex; align-items: center; gap: 10px;">
                <i class="bi ${icon}" style="color: ${iconColor}; font-size: 1.2rem;"></i>
                <span>${message}</span>
            </div>
            <button style="background:none; border:none; color:#94a3b8; cursor:pointer;" onclick="this.parentElement.remove()">&times;</button>
        `;

        container.appendChild(toast);

        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(10px)';
            setTimeout(() => {
                toast.remove();
            }, 500);
        }, 4000);
    }

    // Export showToast to window so inline triggers in other views (if any) or AJAX calls can fire it
    window.showToast = showToast;

    // --- PRODUCT GALLERY INTERACTIVE THUMBNAILS ---
    function initProductGallery() {
        const thumbs = document.querySelectorAll('.product-detail-thumb');
        const mainImg = document.querySelector('#product-main-image');
        
        if (thumbs.length > 0 && mainImg) {
            thumbs.forEach(thumb => {
                thumb.addEventListener('click', function() {
                    thumbs.forEach(t => t.classList.remove('active'));
                    this.classList.add('active');
                    const newSrc = this.getAttribute('data-large');
                    if (newSrc) {
                        mainImg.src = newSrc;
                    }
                });
            });
        }
    }

    // --- INTERACTIVE STAR RATING SELECTION (REVIEW FORM) ---
    function initStarRating() {
        const stars = document.querySelectorAll('.star-rating-interactive i');
        const ratingInput = document.getElementById('selected-rating');
        
        if (stars.length > 0 && ratingInput) {
            stars.forEach(star => {
                star.addEventListener('click', function() {
                    const val = parseInt(this.getAttribute('data-value'));
                    ratingInput.value = val;
                    
                    stars.forEach(s => {
                        const sVal = parseInt(s.getAttribute('data-value'));
                        if (sVal <= val) {
                            s.classList.remove('bi-star');
                            s.classList.add('bi-star-fill', 'active');
                        } else {
                            s.classList.remove('bi-star-fill', 'active');
                            s.classList.add('bi-star');
                        }
                    });
                });

                star.addEventListener('mouseenter', function() {
                    const val = parseInt(this.getAttribute('data-value'));
                    stars.forEach(s => {
                        const sVal = parseInt(s.getAttribute('data-value'));
                        if (sVal <= val) {
                            s.style.color = '#ffc107';
                        }
                    });
                });

                star.addEventListener('mouseleave', function() {
                    stars.forEach(s => {
                        if (!s.classList.contains('active')) {
                            s.style.color = '';
                        }
                    });
                });
            });
        }
    }

    // --- AJAX SHOPPING CART ADDITIONS ---
    function addToCart(productId, qty = 1) {
        fetch(`/Cart/AddToCart?productId=${productId}&quantity=${qty}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getVerificationToken()
            }
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showToast(data.message, 'success');
                updateCartBadge(data.cartCount);
            } else {
                showToast(data.message, 'error');
            }
        })
        .catch(error => {
            console.error('Error adding to cart:', error);
            showToast('Failed to add product to cart', 'error');
        });
    }

    // --- AJAX WISHLIST TOGGLE ---
    function toggleWishlist(productId, btnElement) {
        fetch(`/Wishlist/Toggle?productId=${productId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getVerificationToken()
            }
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showToast(data.message, 'success');
                if (btnElement) {
                    if (data.isAdded) {
                        btnElement.classList.add('active');
                        btnElement.innerHTML = '<i class="bi bi-heart-fill"></i>';
                    } else {
                        btnElement.classList.remove('active');
                        btnElement.innerHTML = '<i class="bi bi-heart"></i>';
                    }
                }
                updateWishlistBadge(data.wishlistCount);
            } else {
                showToast(data.message, 'error');
                if (data.redirect) {
                    window.location.href = data.redirect;
                }
            }
        })
        .catch(error => {
            console.error('Error toggling wishlist:', error);
            showToast('Failed to update wishlist', 'error');
        });
    }

    // --- DYNAMIC CART QUANTITY CONTROLLER (CART PAGE) ---
    function ajaxUpdateQty(productId, newQty) {
        fetch(`/Cart/UpdateQuantity?productId=${productId}&quantity=${newQty}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getVerificationToken()
            }
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Update input box value
                const qtyInput = document.querySelector(`#qty-${productId}`);
                if (qtyInput) qtyInput.value = newQty;
                
                // Update product subtotal
                const subtotalSpan = document.querySelector(`#subtotal-${productId}`);
                if (subtotalSpan) subtotalSpan.textContent = data.itemTotal;
                
                // Update summary card totals
                const summarySubtotal = document.querySelector('#cart-summary-subtotal');
                const summaryTotal = document.querySelector('#cart-summary-total');
                if (summarySubtotal) summarySubtotal.textContent = data.cartTotal;
                if (summaryTotal) summaryTotal.textContent = data.cartTotal;
                
                // Update navbar cart counts
                updateCartBadge(data.cartCount);
                showToast(data.message, 'success');
            } else {
                showToast(data.message, 'error');
            }
        })
        .catch(error => {
            console.error('Error updating quantity:', error);
            showToast('Failed to update quantity', 'error');
        });
    }

    // --- BADGE STATUS MODIFIERS ---
    function updateCartBadge(count) {
        const badge = document.querySelector('#cart-badge');
        if (badge) {
            badge.textContent = count;
            badge.style.display = count > 0 ? 'inline-block' : 'none';
        }
    }

    function updateWishlistBadge(count) {
        const badge = document.querySelector('#wishlist-badge');
        if (badge) {
            badge.textContent = count;
            badge.style.display = count > 0 ? 'inline-block' : 'none';
        }
    }

    function getVerificationToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    // --- INITIALIZE SCRIPTS ON DOM CONTENT LOADED ---
    document.addEventListener('DOMContentLoaded', () => {
        initProductGallery();
        initStarRating();

        // 1. Bind TempData feedback messages from Body attributes
        const successMsg = document.body.getAttribute('data-success-message');
        const errorMsg = document.body.getAttribute('data-error-message');
        
        if (successMsg) {
            showToast(successMsg, 'success');
        }
        if (errorMsg) {
            showToast(errorMsg, 'error');
        }

        // 2. Bind dynamic "Add to Cart" buttons
        document.querySelectorAll('.btn-add-to-cart-ajax').forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.preventDefault();
                const pId = this.getAttribute('data-product-id');
                const qtyInput = document.querySelector('#quantity-input');
                const qty = qtyInput ? parseInt(qtyInput.value) : 1;
                addToCart(pId, qty);
            });
        });

        // 3. Bind "Wishlist Toggle" buttons
        document.querySelectorAll('.btn-wishlist-ajax').forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.preventDefault();
                const pId = this.getAttribute('data-product-id');
                toggleWishlist(pId, this);
            });
        });

        // 4. Bind Cart Page Quantity selectors (if present)
        document.querySelectorAll('.btn-qty-dec').forEach(btn => {
            btn.addEventListener('click', function() {
                const productId = this.getAttribute('data-product-id');
                const input = document.querySelector(`#qty-${productId}`);
                if (input) {
                    let currentVal = parseInt(input.value);
                    if (currentVal > 1) {
                        ajaxUpdateQty(productId, currentVal - 1);
                    }
                }
            });
        });

        document.querySelectorAll('.btn-qty-inc').forEach(btn => {
            btn.addEventListener('click', function() {
                const productId = this.getAttribute('data-product-id');
                const input = document.querySelector(`#qty-${productId}`);
                if (input) {
                    let currentVal = parseInt(input.value);
                    ajaxUpdateQty(productId, currentVal + 1);
                }
            });
        });
    });
})();
