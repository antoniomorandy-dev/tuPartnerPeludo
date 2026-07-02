document.getElementById('form-producto').addEventListener('submit', async (e) => {
    e.preventDefault();

    const formData = new FormData();
    formData.append("Nombre", document.getElementById('nombre').value);
    formData.append("Descripcion", document.getElementById('descripcion').value);
    formData.append("Precio", document.getElementById('precio').value);
    
    const fileInput = document.getElementById('imagenProducto');
    formData.append("file", fileInput.files[0]); 

    const response = await fetch(`${CONFIG.API_BASE_URL}/Productos`, {
        method: 'POST',
        body: formData 
    });

    const data = await response.json();

    if(data.codigo === 1) {
        toastr.success("Producto guardado correctamente");
        document.getElementById('form-producto').reset();
        renderizarTablaProductos(); 
    } else {
        toastr.error("Error al guardar: " + (data.mensaje || "Error desconocido"));
    }
});

window.onload = function() {
    initAuth((data) => {
        const nombre = data.nombre || data.name;
        const foto = data.foto || data.picture;

        const nombreElement = document.getElementById('user-name');
        if (nombreElement) {
            nombreElement.innerText = nombre.toUpperCase();
        }

        const imgElement = document.getElementById('user-img');
        if (imgElement) {
            imgElement.src = foto || "images/default-user.png";
        }
    });
}

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
    formData.append("Precio", document.getElementById('precio').value);
    formData.append("Descripcion", document.getElementById('descripcion').value);
    const fileInput = document.getElementById('imagenProducto');
    formData.append("file", fileInput.files[0]);

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

async function renderizarTablaProductos() {
    const contenedor = document.getElementById('lista-productos');
    
    try {
        // La URL debe coincidir con el endpoint que devuelve el JSON de la captura
        const response = await fetch(`${CONFIG.API_BASE_URL}/Productos`);
        const data = await response.json();
        
        // ¡Aquí está la clave! Accedemos a data.productos
        if (data.productos && Array.isArray(data.productos)) {
            contenedor.innerHTML = ''; 
            
            data.productos.forEach(p => {
                // p.urlImagen ahora contiene la URL que Cloudinary te devuelve
                contenedor.innerHTML += `
                    <tr>
                        <td><img src="${p.urlImagen}" class="img-thumbnail" style="width: 60px; height: 60px; object-fit: cover;"></td>
                        <td>${p.nombre}</td>
                        <td>$${p.precio.toLocaleString()}</td>
                        <td>
                            <button class="btn btn-sm btn-outline-danger">Eliminar</button>
                        </td>
                    </tr>
                `;
            });
        } else {
            console.warn("La API no devolvió una lista en 'productos'", data);
        }
    } catch (error) {
        console.error("Error al cargar la tabla mensaje:", error);
    }
}

document.getElementById('form-producto').addEventListener('submit', async (e) => {
    e.preventDefault();

    const formData = new FormData(e.target); 

    const response = await fetch(`${CONFIG.API_BASE_URL}/Productos/guardar`, {
        method: 'POST',
        body: formData
    });

    const data = await response.json();

    if (data.codigo === 1) {
        e.target.reset();
        renderizarTablaProductos(); 
    } else {
    }
    ProcesarRespuesta(data);
});

document.addEventListener('DOMContentLoaded', () => {
    renderizarTablaProductos();
});