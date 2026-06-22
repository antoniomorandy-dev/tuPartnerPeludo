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

document.getElementById("formLogin").addEventListener("submit", function(event) {
    event.preventDefault();
    iniciarSesion();
});

async function iniciarSesion(event) {
    if (event) event.preventDefault();
    const btnIniciar = document.querySelector("#formLogin button[type='submit']");
    btnIniciar.disabled = true;
    btnIniciar.innerText = "Iniciando Sesión...";

    const Email = document.getElementById("loginEmail").value;
    const Password = document.getElementById("loginPass").value;

    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/usuarios/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: Email, password: Password })
        });

        const data = await response.json();

        ProcesarRespuesta(data);

        if (data.codigo === 1) {
            
            localStorage.setItem('session_token', data.token); 
            localStorage.setItem('user_session', JSON.stringify(data.usuario));

            setTimeout(() => {
                window.location.href = "main.html";
            }, 1000);
        }
            
    } catch (error) {
        console.error("Error Login:", error);
        EnviarMensaje(-1, "Error de conexión con el servidor.");
    } finally {
        btnIniciar.disabled = false;
        btnIniciar.innerText = "ENTRAR";
    }
}
async function registrarUsuario(datosUsuario) {
    const btnRegistrar = document.querySelector("#formRegistro button[type='submit']");
    btnRegistrar.disabled = true;
    btnRegistrar.innerText = "Procesando...";

    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/usuarios/registrar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(datosUsuario)
        });
        const data = await response.json();
        const codigoRespuesta = data.regCodigo;
        const mensajeRespuesta = data.regMensaje;

        if (codigoRespuesta === 1) {
            EnviarMensaje(codigoRespuesta, mensajeRespuesta);
            mostrarVerificacion();
        } else {
            EnviarMensaje(codigoRespuesta || 0, mensajeRespuesta || "Error al registrar.");
        }
    } catch (error) {
        console.error("Error completo:", error);
        EnviarMensaje(-1, "Error: Error al Procesar el registro"); 
    } finally {
        btnRegistrar.disabled = false;
        btnRegistrar.innerText = "REGISTRARME";
    }
}

// Función para mostrar/ocultar campos según la selección
function alternarCamposRecuperacion() {
    const metodo = document.getElementById("metodo-recuperacion").value;
    const campoWs = document.getElementById("campo-whatsapp");
    const campoEmail = document.getElementById("campo-email");

    if (metodo === "WHATSAPP") {
        campoWs.classList.remove("d-none");
        campoEmail.classList.add("d-none");
    } else {
        campoWs.classList.add("d-none");
        campoEmail.classList.remove("d-none");
    }
}

// Nueva versión de la función de solicitud
async function solicitarRecuperacionAdaptada() {
    const metodo = document.getElementById("metodo-recuperacion").value;
    const btn = document.getElementById("btnEnviarRecuperar");
    
    // Obtener valores según el método
    const payload = {
        Metodo: metodo,
        Telefono: metodo === "WHATSAPP" ? document.getElementById("rec-telefono").value.replace(/\D/g, "") : null,
        Email: metodo === "EMAIL" ? document.getElementById("rec-email").value : null
    };

    btn.disabled = true;
    btn.innerText = "Enviando...";

    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/usuarios/solicitar-recuperacion`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const data = await response.json();
        EnviarMensaje(data.codigo, data.mensaje);
        
        if (data.codigo === 1) {
            setTimeout(() => mostrarLogin(), 3000);
        }
    } catch (error) {
        EnviarMensaje(-1, "Error al conectar con el servidor.");
    } finally {
        btn.disabled = false;
        btn.innerText = "ENVIAR ENLACE";
    }
}

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
    event.preventDefault();

    const datos = {
        Nombre: document.getElementById('reg-nombre').value,
        Apellido: document.getElementById('reg-apellido').value || "Sin apellido",
        Email: document.getElementById('reg-email').value,
        Telefono: document.getElementById('reg-telefono').value,
        Password: document.getElementById('reg-password').value
    };

    registrarUsuario(datos);
}

function irAlMain() { 
    window.location.href = "main.html"; 
}

function cerrarSesion() {
    localStorage.clear();
    window.location.href = "index.html";
}

window.handleCredentialResponse = function(response) {
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
    var base64Url = token.split('.')[1];
    var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    var jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));

    return JSON.parse(jsonPayload);
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
    const btnConfirmar = document.getElementById("btn-verificar");

    if (!codigo) {
        EnviarMensaje(-1, "Por favor, ingresa el código de 6 dígitos.");
        return;
    }

    btnConfirmar.disabled = true;
    btnConfirmar.innerText = "Verificando Codigo Whatsapp...";

    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/Usuarios/verificar-codigo`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: email, codigo: codigo })
        });

        const data = await response.json();
        
        EnviarMensaje(data.codigo, data.mensaje);
        
        if (data.codigo === 1) {
            mostrarLogin();
        }
    } catch (error) {
        console.error("Error al verificar:", error);
        EnviarMensaje(-1, "Error al conectar con el servidor.");
    }
    finally
    {
        btnConfirmar.disabled = false;
        btnConfirmar.innerText = "Verificar Cuenta";
    }
}

window.onload = function() {
    if (localStorage.getItem('user_session')) {
        mostrarSeccionPerfil();
    }
};

window.onload = function() {
    // Si estamos en la página de inicio (index.html) y ya hay sesión, podemos ir al main directamente
    // Pero si estamos en main.html, no fuerces una validación de token si ya quitaste la autorización
    const session = localStorage.getItem('user_session');
    
    // Solo mostramos perfil si existe sesión real
    if (session && window.location.pathname.includes("index.html")) {
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

window.onload = function () {
  google.accounts.id.initialize({
    client_id: "85108018661-r3dis4gm7h25kg9or2fnnpckhme87raj.apps.googleusercontent.com",
    callback: window.handleCredentialResponse // Pasamos la función explícitamente
  });
  // No necesitas los atributos data- en el HTML si haces esto
};
