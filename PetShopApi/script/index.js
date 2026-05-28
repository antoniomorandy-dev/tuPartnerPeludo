// PetShopApi/script/index.js

// 1. FUNCIONES DE NAVEGACIÓN (Control de vistas)
function mostrarRecuperar() {
    document.getElementById('login-section').classList.add('d-none');
    document.getElementById('register-section').classList.add('d-none');
    document.getElementById('recuperar-section').classList.remove('d-none');
}

function mostrarRegistro() {
    document.getElementById('login-section').classList.add('d-none');
    document.getElementById('recuperar-section').classList.add('d-none');
    document.getElementById('register-section').classList.remove('d-none');
    document.getElementById('seccion-verificacion').style.display = 'none';
}

function mostrarLogin() {
    document.getElementById('register-section').classList.add('d-none');
    document.getElementById('recuperar-section').classList.add('d-none');
    document.getElementById('user-profile').classList.add('d-none');
    document.getElementById('login-section').classList.remove('d-none');
    document.getElementById('seccion-verificacion').style.display = 'none';
}

// 2. MÉTODOS DE LA API (Basados en tu Swagger)

// Endpoint: /usuarios/login
/*
async function loginUsuario(email, password) {
    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/usuarios/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Email: email, Password: password })
        });
        const data = await response.json();
        if (response.ok && data.codigo === 1) {
            localStorage.setItem('user_session', JSON.stringify(data.objeto));
            irAlMain();
        } else {
            EnviarMensaje(data.codigo, data.mensaje);
        }
    } catch (error) {
        console.error("Error Login:", error);
        EnviarMensaje(-1, "No se pudo conectar con el servidor.");
    }
}
*/

document.getElementById("formLogin").addEventListener("submit", function(event) {
    event.preventDefault(); // Evita que la página se recargue al enviar
    iniciarSesion();      // Aquí es donde se gatilla tu función
});

async function iniciarSesion() {
    const payload = {
        Email: document.getElementById("loginEmail").value,    // Cambiado a mayúscula
        Password: document.getElementById("loginPass").value // Cambiado a mayúscula
    };

    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/usuarios/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const data = await response.json();

        if (response.ok && data.codigo === 1) {
            localStorage.setItem('user_session', JSON.stringify(data.usuario));
            mostrarSeccionPerfil();
            //EnviarMensaje(data.codigo, data.mensaje); // Opcional: mostrar éxito
        } else {
            EnviarMensaje(data.codigo || 0, data.mensaje || "Error desconocido");
        }
    } catch (error) {
        console.error("Error:", error);
        EnviarMensaje(-1, "No se pudo conectar con el servidor.");
    }
}
// Endpoint: /usuarios/registrar
async function registrarUsuario(datosUsuario) {
    console.log("Datos enviados a la API:", JSON.stringify(datosUsuario));
    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/usuarios/registrar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(datosUsuario)
        });
        const data = await response.json();
        EnviarMensaje(data.codigo, data.mensaje);
        if (data.codigo === 1) mostrarVerificacion();
    } catch (error) {
        console.error("Error Registro:", error);
        EnviarMensaje(-1, "Error al procesar el registro.");
    }
}

// Endpoint: /usuarios/solicitar-recuperacion (WhatsApp)
async function solicitarRecuperacion() {
    const telefonoInput = document.getElementById("recuperar-telefono");
    if (!telefonoInput) return;

    const telefonoLimpio = telefonoInput.value.replace(/\D/g, "");

    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/usuarios/solicitar-recuperacion`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Telefono: telefonoLimpio })
        });

        const data = await response.json();
        EnviarMensaje(data.codigo, data.mensaje);
        if (data.codigo === 1) setTimeout(() => mostrarLogin(), 3000);
    } catch (error) {
        console.error("Error Recuperación:", error);
        EnviarMensaje(-1, "Error de conexión.");
    }
}

function procesarRegistro(event) {
    event.preventDefault(); // Evita que la página se recargue

    // Recogemos los datos del formulario
    const datos = {
        Nombre: document.getElementById('reg-nombre').value,
        Apellido: document.getElementById('reg-apellido').value || "Sin apellido",
        Email: document.getElementById('reg-email').value,
        Telefono: document.getElementById('reg-telefono').value,
        Password: document.getElementById('reg-password').value
    };

    // Llamamos a la función que ya tenías
    registrarUsuario(datos);
}

// 3. UTILIDADES Y SESIÓN
function irAlMain() { 
    window.location.href = "main.html"; 
}

function cerrarSesion() {
    localStorage.clear();
    window.location.href = "index.html";
}

function EnviarMensaje(codigo, mensaje) {
    toastr.options = { "closeButton": true, "progressBar": true, "positionClass": 'toast-bottom-right' };
    if (codigo <= -1) toastr.error(mensaje);
    else if (codigo === 0) toastr.info(mensaje);
    else toastr.success(mensaje);
}

function handleCredentialResponse(response) {
    const userData = decodeJwtResponse(response.credential);

    const sesionGoogle = {
        nombre: userData.name,
        foto: userData.picture,
        tipo: "google"
    };
    localStorage.setItem('user_session', JSON.stringify(sesionGoogle));
    localStorage.setItem('session_token', response.credential);

    mostrarSeccionPerfil();
}

function mostrarSeccionPerfil() {
    const user = JSON.parse(localStorage.getItem('user_session'));
    if (user) {
        document.getElementById('login-section').classList.add('d-none');
        document.getElementById('user-profile').classList.remove('d-none');
        document.getElementById('user-name').textContent = user.nombre;
        if (user.foto) {
            document.getElementById('user-photo').src = user.foto;
        }
    }
}

function mostrarVerificacion() {
    document.getElementById('register-section').classList.add('d-none');
    document.getElementById('seccion-verificacion').style.display = 'block';
}

async function confirmarCodigo() {
    const codigo = document.getElementById('codigo-verificacion').value;
    const email = document.getElementById('reg-email').value;
    
    // Validamos que no esté vacío
    if (!codigo) {
        EnviarMensaje(-1, "Por favor, ingresa el código de 6 dígitos.");
        return;
    }

    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/Usuarios/verificar-codigo`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            // Según tu Swagger, el endpoint espera un string simple
            body: JSON.stringify({ email: email, codigo: codigo })
        });

        const data = await response.json();
        
        // Manejo de la respuesta
        EnviarMensaje(data.codigo, data.mensaje);
        
        if (data.codigo === 1) {
            // Si es correcto, llevamos al usuario al login
            mostrarLogin();
        }
    } catch (error) {
        console.error("Error al verificar:", error);
        EnviarMensaje(-1, "Error al conectar con el servidor.");
    }
}

// También es recomendable agregar esta validación al cargar la página
window.onload = function() {
    if (localStorage.getItem('user_session')) {
        mostrarSeccionPerfil();
    }
};

function decodeJwtResponse(token) {
    let base64Url = token.split('.')[1];
    let base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    let jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
    return JSON.parse(jsonPayload);
}
