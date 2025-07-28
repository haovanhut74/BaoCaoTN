let selectedId = "";
let selectedName = "";

const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

document.querySelectorAll('.btn-delete').forEach(btn => {
    btn.addEventListener('click', function () {
        selectedId = this.dataset.id;
        selectedName = this.dataset.name;
        document.getElementById('role-to-delete-name').innerText = selectedName;
        const modal = new bootstrap.Modal(document.getElementById('confirmDeleteModal'));
        modal.show();
    });
});

document.getElementById('confirm-delete-btn').addEventListener('click', function () {
    fetch('/Admin/Role/DeleteAjax', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({id: selectedId})
    })
        .then(res => res.json())
        .then(data => {
            const modal = bootstrap.Modal.getInstance(document.getElementById('confirmDeleteModal'));
            modal.hide();

            const alertBox = document.getElementById('alert-area');
            if (data.success) {
                document.getElementById(`role-${selectedId}`).remove();
                showAlert(data.message, 'success');
            } else {
                showAlert(data.message, 'danger');
            }
        })
        .catch(err => {
            console.error(err);
            showAlert("Có lỗi xảy ra khi xóa.", 'danger');
        });
});

function showAlert(message, type) {
    const alertBox = document.getElementById('alert-area');
    alertBox.innerHTML = `
                <div class="alert alert-${type} alert-dismissible fade show shadow-sm" role="alert">
                    ${message}
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Đóng"></button>
                </div>`;
    setTimeout(() => {
        alertBox.innerHTML = '';
    }, 4000);
}
