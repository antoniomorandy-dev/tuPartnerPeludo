document.getElementById('form-producto').addEventListener('submit', async (e) => {
    e.preventDefault();
    const producto = {
        nombre: document.getElementById('nombre').value,
        descripcion: document.getElementById('descripcion').value,
        precio: parseFloat(document.getElementById('precio').value),
        urlImagen: document.getElementById('urlImagen').value
    };

    const response = await fetch(`${CONFIG.API_BASE_URL}/Productos`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(producto)
    });

    if(response.ok) {
        toastr.success("Producto guardado correctamente");
        document.getElementById('form-producto').reset();
    }
});

function verificarAdmin() {
    const session = JSON.parse(localStorage.getItem('user_session'));
    if (!session || session.rol !== 'admin') {
        toastr.error("Acceso denegado: Área exclusiva para administradores");
        window.location.href = "main.html";
    }
}
verificarAdmin();