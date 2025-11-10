// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {

    // ===== THÊM CODE XỬ LÝ NÚT TÌM KIẾM =====
    $('#searchToggle').on('click', function (e) {
        e.preventDefault(); // Ngăn link '#' nhảy trang
        e.stopPropagation(); // Ngăn sự kiện click lan ra document
        $('#searchBoxContainer').toggleClass('active'); // Hiện/ẩn search box

        // Tự động focus vào input khi search box hiện ra
        if ($('#searchBoxContainer').hasClass('active')) {
            // Dùng setTimeout nhỏ để đảm bảo input đã visible trước khi focus
            setTimeout(function () {
                $('.search-input').focus();
            }, 100);
        }
    });

    // Ẩn search box khi click ra ngoài
    $(document).on('click', function (e) {
        // Nếu click không phải vào nút toggle VÀ không phải vào bên trong search box
        if (!$(e.target).closest('#searchToggle').length && !$(e.target).closest('.search-box-container').length) {
            $('#searchBoxContainer').removeClass('active'); // Ẩn search box
        }
    });

    // Ngăn chặn việc click vào bên trong search box làm ẩn nó đi
    $('.search-box-container').on('click', function (e) {
        e.stopPropagation(); // Ngăn sự kiện click lan ra document
    });

    // Xử lý tìm kiếm khi nhấn Enter hoặc click nút (Giữ nguyên)
    function performSearch() {
        var searchTerm = $('.search-input').val();
        if (searchTerm.trim() !== '') {
            // Thay đổi URL này thành action tìm kiếm của bạn
            window.location.href = '/Home/Index?searchTerm=' + encodeURIComponent(searchTerm);
        }
    }

    $('.search-input').on('keypress', function (e) {
        if (e.which === 13) { // Phím Enter
            e.preventDefault();
            performSearch();
        }
    });

    $('.search-button').on('click', function () {
        performSearch();
    });
    // ===== KẾT THÚC CODE TÌM KIẾM =====

}); // Kết thúc document ready