// Hiển thị ảnh được chọn từ input file lên thẻ img
// (Thẻ input có thuộc tính data-img-preview trỏ đến id của thẻ img dung để hiển thị ảnh)
function previewImage(input) {
    if (!input.files || !input.files[0]) return;

    const previewId = input.dataset.imgPreview; // lấy data-img-preview
    if (!previewId) return;

    const img = document.getElementById(previewId);
    if (!img) return;

    const reader = new FileReader();
    reader.onload = function (e) {
        img.src = e.target.result;
    };
    reader.readAsDataURL(input.files[0]);
}

// Tìm kiếm phân trang bằng AJAX
function paginationSearch(event, form, page) {
    if (event) {
        event.preventDefault(); // Chặn sự kiện mặc định
        event.stopPropagation(); // Chặn nổi bọt sự kiện
    }
    if (!form) return false;

    const url = form.action;
    const targetId = form.dataset.target;
    const formData = new FormData(form);

    formData.set("Page", page); // Đồng bộ tham số Page 

    // Làm sạch dấu chấm phân cách hàng nghìn trước khi gửi 
    if (formData.has("MinPrice")) {
        formData.set("MinPrice", formData.get("MinPrice").replace(/[^0-9]/g, ""));
    }
    if (formData.has("MaxPrice")) {
        formData.set("MaxPrice", formData.get("MaxPrice").replace(/[^0-9]/g, ""));
    }

    const params = new URLSearchParams(formData).toString();
    const fetchUrl = url + "?" + params;

    const targetEl = document.getElementById(targetId);
    if (targetEl) {
        targetEl.innerHTML = `<div class="text-center py-4"><span>Đang tải...</span></div>`;

        fetch(fetchUrl)
            .then(res => res.text())
            .then(html => {
                targetEl.innerHTML = html;
            })
            .catch(err => {
                console.error("Search error:", err);
                targetEl.innerHTML = `<div class="text-danger">Lỗi tải dữ liệu</div>`;
            });
    }

    return false; //Trả về false để form không bị submit thật
}

// Mở modal và load nội dung từ link vào modal
(function () {
    //dialogModal là id của modal dùng chung đuơc định nghĩa trong _Layout.cshtml
    const modalEl = document.getElementById("dialogModal");
    if (!modalEl) return;

    const modalContent = modalEl.querySelector(".modal-content");

    // Clear nội dung khi modal đóng
    modalEl.addEventListener('hidden.bs.modal', function () {
        modalContent.innerHTML = '';
    });

    window.openModal = function (event, link) {
        if (!link) return;
        if (event) event.preventDefault();

        const url = link.getAttribute("href");

        // Hiển thị loading
        modalContent.innerHTML = `
            <div class="modal-body text-center py-5">
                <span>Đang tải dữ liệu...</span>
            </div>`;

        // Khởi tạo modal (chỉ tạo 1 lần)
        let modal = bootstrap.Modal.getInstance(modalEl);
        if (!modal) {
            modal = new bootstrap.Modal(modalEl, {
                backdrop: 'static',
                keyboard: false
            });
        }

        modal.show();

        // Load nội dung
        fetch(url)
            .then(res => res.text())
            .then(html => {
                modalContent.innerHTML = html;
            })
            .catch(() => {
                modalContent.innerHTML = `
                    <div class="modal-body text-danger">
                        Không tải được dữ liệu
                    </div>`;
            });
    };
})();


//Xử lý các hành động đơn hàng(Duyệt, Hủy, Từ chối, Xóa) qua Ajax

function handleOrderAction(event, form) {
    event.preventDefault(); // Chặn việc load lại trang của form mặc định

    const url = form.action;
    const method = form.method;
    const formData = $(form).serialize(); // Lấy toàn bộ dữ liệu trong form

    $.ajax({
        url: url,
        type: method,
        data: formData,
        success: function (res) {
            if (res.code === 1) {
                // Nếu là hành động XÓA đơn hàng
                if (url.toLowerCase().includes("delete")) {
                    alert("Xóa đơn hàng thành công!");
                    window.location.href = '/Order'; // Quay lại trang danh sách
                } else {
                    // Các hành động khác (Duyệt, Hủy...) thì chỉ cần load lại trang hiện tại
                    window.location.reload();
                }
            } else {
                alert(res.message || "Thao tác thất bại.");
            }
        },
        error: function () {
            alert("Đã có lỗi xảy ra trong quá trình xử lý.");
        }
    });
}
