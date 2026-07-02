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
        EnviarMensaje(-1, "Acceso denegado: Área exclusiva para administradores");
        window.location.href = "main.html";
    }
}
verificarAdmin();

document.getElementById('form-producto').addEventListener('submit', async (e) => {
    e.preventDefault();

    const formData = new FormData();
    formData.append("Nombre", document.getElementById('nombre').value);
    formData.append("Descripcion", document.getElementById('descripcion').value);
    formData.append("Precio", document.getElementById('precio').value);
    formData.append("UrlImagen", document.getElementById('imagenProducto').files[0]);

    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/Productos/guardar`, {
            method: 'POST',
            // IMPORTANTE: No definas 'Content-Type' aquí. 
            // El navegador lo asigna automáticamente al usar FormData.
            body: formData
        });

        const data = await response.json();
        
        if (data.codigo === 1) {
            toastr.success("Producto guardado correctamente");
            document.getElementById('form-producto').reset();
        } else {
            toastr.error(data.mensaje || "Error al guardar");
        }
    } catch (error) {
        toastr.error("Error de conexión con el servidor");
    }
});

async function cargarProductos() {
    const response = await fetch(`${CONFIG.API_BASE_URL}/Productos/listar`);
    const data = await response.json();

    if (data.codigo === 1) {
        const contenedor = document.getElementById('contenedorProductos');
        contenedor.innerHTML = ''; // Limpiar antes de llenar

        data.productos.forEach(p => {
            // CONSTRUCCIÓN DE LA URL: 
            // Como guardamos la ruta relativa (ej: /images/productos/...),
            // concatenamos con la URL base de tu servidor.
            const urlCompleta = `https://tupartnerpeludo.onrender.com${p.urlImagen}`;

            contenedor.innerHTML += `
                <div class="col-md-4">
                    <div class="card">
                        <img src="${urlCompleta}" class="card-img-top" alt="${p.nombre}" style="height: 200px; object-fit: cover;">
                        <div class="card-body">
                            <h5>${p.nombre}</h5>
                            <p>Precio: $${p.precio}</p>
                        </div>
                    </div>
                </div>
            `;
        });
    }
}