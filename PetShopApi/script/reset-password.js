// Estas constantes son globales pero seguras
const urlParams = new URLSearchParams(window.location.search);
const tokenActual = urlParams.get('token');

// Solo ejecutamos lógica si estamos en la página correcta
if (window.location.pathname.includes('reset-password.html')) {
    
    // Capturamos los elementos una vez que sabemos que estamos en la página correcta
    const pass1 = document.getElementById('pass1');
    const pass2 = document.getElementById('pass2');
    const btn = document.getElementById('btnGuardar');
    const formRestablecer = document.getElementById('formRestablecer');

    const validarPassword = () => {
        const val = pass1.value;
        const reglas = {
            mayus: /[A-Z]/.test(val),
            minus: /[a-z]/.test(val),
            numero: /\d/.test(val),
            largo: val.length >= 8
        };

        actualizarCheck("check-mayus", reglas.mayus);
        actualizarCheck("check-minus", reglas.minus);
        actualizarCheck("check-numero", reglas.numero);
        actualizarCheck("check-largo", reglas.largo);

        const coinciden = val === pass2.value && val !== "";
        const mensajeCoincide = document.getElementById('check-coincide');
        if(mensajeCoincide) {
            mensajeCoincide.classList.toggle('d-none', coinciden || pass2.value === "");
        }
        
        btn.disabled = !(reglas.mayus && reglas.minus && reglas.numero && reglas.largo && coinciden);
    };

    // Agregamos los listeners solo aquí
    if (pass1 && pass2) {
        pass1.addEventListener('input', validarPassword);
        pass2.addEventListener('input', validarPassword);
    }

    if (formRestablecer) {
        formRestablecer.addEventListener('submit', async (e) => {
            e.preventDefault();

            if (!tokenActual) {
                toastr.error("Token no encontrado. Solicita un nuevo enlace.");
                return;
            }

            btn.disabled = true;
            btn.innerText = "Guardando...";

            try {
                const urlFinal = `${CONFIG.API_BASE_URL}/usuarios/restablecer-final`;
                const response = await fetch(urlFinal, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        Token: tokenActual, 
                        NuevaPassword: pass1.value 
                    })
                });

                const data = await response.json();
                
                if (data.codigo === 1) {
                    toastr.success("¡Contraseña actualizada correctamente!");
                    setTimeout(() => { window.location.href = "index.html"; }, 2000);
                } else {
                    toastr.warning(data.mensaje);
                    btn.disabled = false;
                    btn.innerText = "Guardar Cambios";
                }
            } catch (error) {
                console.error("Error:", error);
                toastr.error("No se pudo conectar con el servidor.");
                btn.disabled = false;
                btn.innerText = "Guardar Cambios";
            }
        });
    }
}

// La función auxiliar puede estar fuera, no hace daño
function actualizarCheck(id, cumple) {
    const el = document.getElementById(id);
    if (!el) return;
    
    if (cumple) {
        el.classList.replace('text-danger', 'text-success');
        el.innerHTML = "✓ " + el.innerText.substring(2);
    } else {
        el.classList.replace('text-success', 'text-danger');
        el.innerHTML = "✕ " + el.innerText.substring(2);
    }
}