document.addEventListener("DOMContentLoaded", async () => {
    const formRestablecer = document.getElementById('formRestablecer');
    const container = document.querySelector('.card');
    const urlParams = new URLSearchParams(window.location.search);
    const tokenActual = urlParams.get('token');
    const tituloReset = document.getElementById('tituloReset');

    if (!window.location.pathname.includes('reset-password.html')) return;

    // Función auxiliar para manejar el error de token
    const manejarTokenInvalido = (mensaje) => {
        // 1. Cambiamos el título en pantalla
        if (tituloReset) tituloReset.innerText = "Enlace no válido";
        
        // 2. Usamos tu función centralizada
        EnviarMensaje(-1, mensaje);
        
        // 3. Ocultamos el formulario
        if (formRestablecer) formRestablecer.style.display = 'none';
    };

    // 1. Validación Inicial del Token
    if (!tokenActual) {
        EnviarMensaje(-1, "Token no encontrado. Por favor, solicita uno nuevo.");
        return;
    }

    try {
        const res = await fetch(`${CONFIG.API_BASE_URL}/usuarios/validar-token?token=${encodeURIComponent(tokenActual)}`);
        const data = await res.json();
        
        if (data.codigo !== 1) {
            manejarTokenInvalido(data.mensaje); // "Este enlace ha caducado..."
            return;
        }
        
        // Si es válido, podemos poner un mensaje informativo
        EnviarMensaje(1, "Token validado correctamente.");
    } catch (e) {
        manejarTokenInvalido("Error de conexión al validar el token.");
    }

    // --- LÓGICA DEL FORMULARIO (Si el token es válido) ---
    const pass1 = document.getElementById('pass1');
    const pass2 = document.getElementById('pass2');
    const btn = document.getElementById('btnGuardar');

    // ... (aquí mantienes tu lógica de validarPassword y los event listeners) ...

    formRestablecer.addEventListener('submit', async (e) => {
        e.preventDefault();
        btn.disabled = true;
        btn.innerText = "Guardando...";

        try {
            const response = await fetch(`${CONFIG.API_BASE_URL}/usuarios/restablecer-final`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ Token: tokenActual, NuevaPassword: pass1.value })
            });

            const data = await response.json();
            
            if (data.codigo === 1) {
                EnviarMensaje(data.codigo, data.mensaje);
                setTimeout(() => window.location.href = "index.html", 2000);
            } else {
                EnviarMensaje(data.codigo, data.mensaje); // Azul informativo
                btn.disabled = false;
                btn.innerText = "Guardar Cambios";
            }
        } catch (error) {
            EnviarMensaje(-1, "Error al conectar con el servidor.");
            btn.disabled = false;
            btn.innerText = "Guardar Cambios";
        }
    });
});

function actualizarCheck(id, cumple) {
    const el = document.getElementById(id);
    if (!el) return;
    el.classList.replace(cumple ? 'text-danger' : 'text-success', cumple ? 'text-success' : 'text-danger');
    el.innerHTML = (cumple ? "✓ " : "✕ ") + el.innerText.substring(2);
}