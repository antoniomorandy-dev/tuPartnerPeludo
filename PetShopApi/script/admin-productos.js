document.getElementById('form-producto').addEventListener('submit', async (e) => {
    e.preventDefault();
    const producto = {
        nombre: document.getElementById('nombre').value,
        descripcion: document.getElementById('descripcion').value,
        precio: parseFloat(document.getElementById('precio').value),
        urlImagen: document.getElementById('urlImagen').value
    };

    const response = await fetch('https://tupartnerpeludo.onrender.com/api/Productos', {
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
    
    // Ahora 'session.rol' vendrá directamente de tu base de datos
    if (!session || session.rol !== 'admin') {
        toastr.error("Acceso denegado: Área exclusiva para administradores");
        window.location.href = "main.html"; // Redirigir a lugar seguro
    }
}