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
}

function mostrarLogin() {
    document.getElementById('register-section').classList.add('d-none');
    document.getElementById('recuperar-section').classList.add('d-none');
    document.getElementById('user-profile').classList.add('d-none');
    document.getElementById('login-section').classList.remove('d-none');
}

// 2. MÉTODOS DE LA API (Basados en tu Swagger)

// Endpoint: /usuarios/login
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

// Endpoint: /usuarios/registrar
async function registrarUsuario(datosUsuario) {
    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/usuarios/registrar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(datosUsuario)
        });
        const data = await response.json();
        EnviarMensaje(data.codigo, data.mensaje);
        if (data.codigo === 1) mostrarLogin();
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
function decodeJwtResponse(token) {
    let base64Url = token.split('.')[1];
    let base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    let jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
    return JSON.parse(jsonPayload);
}