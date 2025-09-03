$(document).ready(function () {
    // Bắt sự kiện checkbox lọc thay đổi
    $('#filterForm input[type="checkbox"]').on('change', function () {
        let form = $('#filterForm');
        let url = '@Url.Action("FilterPartial", "Product", new { area = "User" })';

        $.ajax({
            url: url,
            type: 'GET',
            data: form.serialize(),
            success: function (html) {
                $('#product-list-container').html(html);
            },
            error: function () {
                showToast("⚠️ Lọc sản phẩm thất bại!", "danger");
            }
        });
    });

    // Thêm sản phẩm vào giỏ (sử dụng logic gốc của bạn)
    $(document).on('click', '.btn-add-to-cart', function () {
        const productId = $(this).data('id');
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '@Url.Action("Add", "Cart", new { area = "User" })',
            type: 'POST',
            data: {
                id: productId,
                __RequestVerificationToken: token
            },
            success: function (response) {
                if (response.success) {
                    showToast(response.message, "success");

                    if (response.cartCount !== undefined) {
                        $("#cartCount").text(response.cartCount);
                    }
                } else {
                    showToast(response.message, "danger");
                }
            },

            error: function () {
                showToast("❌ Có lỗi xảy ra!", "danger");
            }
        });
    });

    // Initialize card animations on page load
    initializeCardAnimations();
});

// Modern Toast Function
function showToast(message, type = 'success') {
    const toastContainer = document.getElementById('toast-container');
    const toastId = 'toast-' + Date.now();

    const iconMap = {
        success: 'fas fa-check-circle',
        danger: 'fas fa-exclamation-triangle',
        warning: 'fas fa-exclamation-circle',
        info: 'fas fa-info-circle'
    };

    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = `toast toast-modern align-items-center text-white bg-${type} border-0 show`;
    toast.setAttribute('role', 'alert');
    toast.innerHTML = `
                <div class="d-flex">
                    <div class="toast-body d-flex align-items-center">
                        <i class="${iconMap[type]} me-2"></i>
                        ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" 
                            onclick="closeToast('${toastId}')" aria-label="Close"></button>
                </div>
            `;

    toastContainer.appendChild(toast);
/*
    // Auto remove after 4 seconds
    setTimeout(() => closeToast(toastId), 4000);
}

function closeToast(toastId) {
    const toast = document.getElementById(toastId);
    if (toast) {
        toast.style.transform = 'translateX(100%)';
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }
}

// Initialize card animations
function initializeCardAnimations() {
    const cards = document.querySelectorAll('.fade-in');
    cards.forEach((card, index) => {
        card.style.animationDelay = `${index * 0.1}s`;
    });*/
}