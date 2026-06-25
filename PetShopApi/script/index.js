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

function mostrarVerificacion() {
    document.getElementById('register-section').classList.add('d-none');
    document.getElementById('seccion-verificacion').style.display = 'block';
    document.getElementById('recuperar-section').classList.remove('d-none');
}

document.getElementById("formLogin").addEventListener("submit", function(event) {
    event.preventDefault();
    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPass').value;
    
    // Llamamos a la función centralizada
    realizarLogin(email, password);
});

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
        const codigo = data.codigo;
        const mensaje = data.mensaje;

        if (codigo === 1) {
            EnviarMensaje(codigo, mensaje);
            mostrarLogin();
        } else {
            // Si codigo es undefined, usamos 0 como respaldo
            EnviarMensaje(codigo || 0, mensaje || "Error al registrar.");
        }
    } catch (error) {
        console.error("Error completo:", error);
        EnviarMensaje(-1, "Error: Error al Procesar el registro"); 
    } finally {
        btnRegistrar.disabled = false;
        btnRegistrar.innerText = "REGISTRARME";
    }
}

async function cargarMetodosRecuperacion() {
    try {
        const res = await fetch(`${CONFIG.API_BASE_URL}/usuarios/metodos-recuperacion`);
        const json = await res.json();
        console.log(json);
        const metodos = json.salida.metodos; 
        const select = document.getElementById('metodo-recuperacion');
        
        select.innerHTML = "";

        metodos.forEach(metodo => {
            const option = document.createElement('option');
            option.value = metodo.metodo;
            option.textContent = metodo.etiqueta;
            option.dataset.placeholder = metodo.placeholder; 
            select.appendChild(option);
        });

        alternarCamposRecuperacion();
    } catch (e) {
        console.error("Error al cargar métodos:", e);
    }
}

function alternarCamposRecuperacion() {
    const select = document.getElementById("metodo-recuperacion");
    const metodo = select.value;
    
    const todosLosCampos = document.querySelectorAll(".campo-recuperacion");
    
    todosLosCampos.forEach(c => c.classList.add("d-none"));
    
    const campoSeleccionado = document.getElementById("campo-" + metodo.toLowerCase());
    if (campoSeleccionado) {
        campoSeleccionado.classList.remove("d-none");
    }

    const placeholder = select.options[select.selectedIndex].dataset.placeholder;
    const inputActivo = campoSeleccionado.querySelector("input");
    if(inputActivo) inputActivo.placeholder = placeholder;
}

async function solicitarRecuperacionAdaptada() {
    const metodo = document.getElementById("metodo-recuperacion").value;
    const btn = document.getElementById("btnEnviarRecuperar");
    const campoTelefono = document.getElementById('rec-telefono');
    const campoEmail = document.getElementById('rec-email');

    let esValido = false;
    let valorAEnviar = "";
    if (metodo === "WHATSAPP") {
        const telefonoRegex = /^\d{8,12}$/;
        esValido = telefonoRegex.test(campoTelefono.value);
        valorAEnviar = campoTelefono.value;
        if (!esValido) EnviarMensaje(-1, "Por favor, ingresa un número de teléfono válido.");
    } else {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        esValido = emailRegex.test(campoEmail.value);
        valorAEnviar = campoEmail.value;
        if (!esValido) EnviarMensaje(-1, "Por favor, ingresa un correo electrónico válido.");
    }
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
        else
        {
            EnviarMensaje(0, data.mensaje);
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
    const sessionData = localStorage.getItem('user_session');

    if (sessionData && window.location.pathname.includes("index.html")) {
        const session = JSON.parse(sessionData);
        window.location.replace(session.rol === 'admin' ? "admin-productos.html" : "main.html");
        alert('bucle');
        return;
    }

    // ... inicialización de Google ...
};

function decodeJwtResponse(token) {
    let base64Url = token.split('.')[1];
    let base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    let jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
    return JSON.parse(jsonPayload);
}

// Asegúrate de ejecutar esto cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', () => {
    // Si estás en la página de recuperación, carga los métodos:
    if (document.getElementById('metodo-recuperacion')) {
        cargarMetodosRecuperacion();
    }
});